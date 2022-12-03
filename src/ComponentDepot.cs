using System;
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
		private void Register<TComponent>(int index) where TComponent : unmanaged
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

#if DEBUG
		public object Debug_Get(int entityID, int componentTypeIndex)
		{
			return storages[componentTypeIndex].Debug_Get(entityID);
		}
#endif

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
	}
}
