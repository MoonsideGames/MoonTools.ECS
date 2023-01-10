using System;
using System.Collections.Generic;

namespace MoonTools.ECS
{
	internal abstract class ComponentStorage
	{
		internal abstract unsafe void Set(int entityID, void* component);
		public abstract bool Remove(int entityID);
		public abstract void Clear();

		// used for debugging and template instantiation
		internal abstract unsafe void* UntypedGet(int entityID);
		// used to create correctly typed storage on snapshot
		public abstract ComponentStorage CreateStorage();
#if DEBUG
		internal abstract object Debug_Get(int entityID);
		internal abstract IEnumerable<int> Debug_GetEntityIDs();
#endif
	}

	internal class ComponentStorage<TComponent> : ComponentStorage where TComponent : unmanaged
	{
		private int nextID;
		private readonly Dictionary<int, int> entityIDToStorageIndex = new Dictionary<int, int>(16);
		private int[] entityIDs = new int[16];
		private TComponent[] components = new TComponent[16];

		public bool Any()
		{
			return nextID > 0;
		}

		public ref readonly TComponent Get(int entityID)
		{
			return ref components[entityIDToStorageIndex[entityID]];
		}

		internal override unsafe void* UntypedGet(int entityID)
		{
			fixed (void* p = &components[entityIDToStorageIndex[entityID]])
			{
				return p;
			}
		}

		public ref readonly TComponent GetFirst()
		{
#if DEBUG
			if (nextID == 0)
			{
				throw new IndexOutOfRangeException("Component storage is empty!");
			}
#endif
			return ref components[0];
		}

		public void Set(int entityID, in TComponent component)
		{
			if (!entityIDToStorageIndex.ContainsKey(entityID))
			{
				var index = nextID;
				nextID += 1;

				if (index >= components.Length)
				{
					Array.Resize(ref components, components.Length * 2);
					Array.Resize(ref entityIDs, entityIDs.Length * 2);
				}

				entityIDToStorageIndex[entityID] = index;
				entityIDs[index] = entityID;
			}

			components[entityIDToStorageIndex[entityID]] = component;
		}

		internal override unsafe void Set(int entityID, void* component)
		{
			Set(entityID, *((TComponent*) component));
		}

		// Returns true if the entity had this component.
		public override bool Remove(int entityID)
		{
			if (entityIDToStorageIndex.TryGetValue(entityID, out int storageIndex))
			{
				entityIDToStorageIndex.Remove(entityID);

				var lastElementIndex = nextID - 1;

				// move a component into the hole to maintain contiguous memory
				if (lastElementIndex != storageIndex)
				{
					var lastEntityID = entityIDs[lastElementIndex];
					entityIDToStorageIndex[lastEntityID] = storageIndex;
					components[storageIndex] = components[lastElementIndex];
					entityIDs[storageIndex] = lastEntityID;
				}

				nextID -= 1;

				return true;
			}

			return false;
		}

		public override void Clear()
		{
			nextID = 0;
			entityIDToStorageIndex.Clear();
		}

		public ReadOnlySpan<TComponent> AllComponents()
		{
			return new ReadOnlySpan<TComponent>(components, 0, nextID);
		}

		public Entity FirstEntity()
		{
#if DEBUG
			if (nextID == 0)
			{
				throw new IndexOutOfRangeException("Component storage is empty!");
			}
#endif
			return new Entity(entityIDs[0]);
		}

		public override ComponentStorage<TComponent> CreateStorage()
		{
			return new ComponentStorage<TComponent>();
		}

#if DEBUG
		internal override object Debug_Get(int entityID)
		{
			return components[entityIDToStorageIndex[entityID]];
		}

		internal override IEnumerable<int> Debug_GetEntityIDs()
		{
			return entityIDToStorageIndex.Keys;
		}
#endif
	}
}
