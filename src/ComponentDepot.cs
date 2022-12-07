﻿using System;
using System.Runtime.CompilerServices;

namespace MoonTools.ECS
{
	internal class ComponentDepot
	{
		private TypeIndices ComponentTypeIndices;

		private ComponentStorage[] storages = new ComponentStorage[256];

		public ComponentDepot(TypeIndices componentTypeIndices)
		{
			ComponentTypeIndices = componentTypeIndices;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void Register<TComponent>(int index) where TComponent : unmanaged
		{
			if (index >= storages.Length)
			{
				Array.Resize(ref storages, storages.Length * 2);
			}

			storages[index] = new ComponentStorage<TComponent>();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private ComponentStorage<TComponent> Lookup<TComponent>() where TComponent : unmanaged
		{
			var storageIndex = ComponentTypeIndices.GetIndex<TComponent>();
			// TODO: is there some way to avoid this null check?
			if (storages[storageIndex] == null)
			{
				Register<TComponent>(storageIndex);
			}
			return (ComponentStorage<TComponent>) storages[storageIndex];
		}

		public bool Some<TComponent>() where TComponent : unmanaged
		{
			return Lookup<TComponent>().Any();
		}

		public ref readonly TComponent Get<TComponent>(int entityID) where TComponent : unmanaged
		{
			return ref Lookup<TComponent>().Get(entityID);
		}

		public ref readonly TComponent GetFirst<TComponent>() where TComponent : unmanaged
		{
			return ref Lookup<TComponent>().GetFirst();
		}

		public void Set<TComponent>(int entityID, in TComponent component) where TComponent : unmanaged
		{
			Lookup<TComponent>().Set(entityID, component);
		}



		public Entity GetSingletonEntity<TComponent>() where TComponent : unmanaged
		{
			return Lookup<TComponent>().FirstEntity();
		}

		public ReadOnlySpan<TComponent> ReadComponents<TComponent>() where TComponent : unmanaged
		{
			return Lookup<TComponent>().AllComponents();
		}

		public void Remove(int entityID, int storageIndex)
		{
			storages[storageIndex].Remove(entityID);
		}

		public void Remove<TComponent>(int entityID) where TComponent : unmanaged
		{
			Lookup<TComponent>().Remove(entityID);
		}

		public void Clear()
		{
			for (var i = 0; i < ComponentTypeIndices.Count; i += 1)
			{
				if (storages[i] != null)
				{
					storages[i].Clear();
				}
			}
		}

		// these methods used to implement snapshots, templates, and debugging

		// FIXME: use unsafe pointers instead of object
		internal object UntypedGet(int entityID, int componentTypeIndex)
		{
			return storages[componentTypeIndex].UntypedGet(entityID);
		}

		// FIXME: use unsafe pointers instead of object
		internal void Set(int entityID, int componentTypeIndex, object component)
		{
			storages[componentTypeIndex].Set(entityID, component);
		}

		public void CreateMissingStorages(ComponentDepot other)
		{
			for (var i = 0; i < ComponentTypeIndices.Count; i += 1)
			{
				if (storages[i] == null && other.storages[i] != null)
				{
					storages[i] = other.storages[i].CreateStorage();
				}
			}
		}
	}
}
