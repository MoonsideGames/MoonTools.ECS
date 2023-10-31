using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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

	internal unsafe class ComponentStorage<TComponent> : ComponentStorage, IDisposable where TComponent : unmanaged
	{
		private readonly Dictionary<int, int> entityIDToStorageIndex = new Dictionary<int, int>(16);
		private TComponent* components;
		private int* entityIDs;
		private int count = 0;
		private int capacity = 16;
		private bool disposed;

		public ComponentStorage()
		{
			components = (TComponent*) NativeMemory.Alloc((nuint) (capacity * Unsafe.SizeOf<TComponent>()));
			entityIDs = (int*) NativeMemory.Alloc((nuint) (capacity * Unsafe.SizeOf<int>()));
		}

		public bool Any()
		{
			return count > 0;
		}

		public ref TComponent Get(int entityID)
		{
			return ref components[entityIDToStorageIndex[entityID]];
		}

		internal override unsafe void* UntypedGet(int entityID)
		{
			return &components[entityIDToStorageIndex[entityID]];
		}

		public ref readonly TComponent GetFirst()
		{
#if DEBUG
			if (count == 0)
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
				var index = count;
				count += 1;

				if (index >= capacity)
				{
					capacity *= 2;
					components = (TComponent*) NativeMemory.Realloc(components, (nuint) (capacity * Unsafe.SizeOf<TComponent>()));
					entityIDs = (int*) NativeMemory.Realloc(entityIDs, (nuint) (capacity * Unsafe.SizeOf<int>()));
				}

				entityIDToStorageIndex[entityID] = index;
				entityIDs[index] = entityID;
			}

			components[entityIDToStorageIndex[entityID]] = component;
		}

		internal override unsafe void Set(int entityID, void* component)
		{
			Set(entityID, *(TComponent*) component);
		}

		// Returns true if the entity had this component.
		public override bool Remove(int entityID)
		{
			if (entityIDToStorageIndex.TryGetValue(entityID, out int storageIndex))
			{
				entityIDToStorageIndex.Remove(entityID);

				var lastElementIndex = count - 1;

				// move a component into the hole to maintain contiguous memory
				if (lastElementIndex != storageIndex)
				{
					var lastEntityID = entityIDs[lastElementIndex];
					entityIDToStorageIndex[lastEntityID] = storageIndex;
					components[storageIndex] = components[lastElementIndex];
					entityIDs[storageIndex] = lastEntityID;
				}

				count -= 1;

				return true;
			}

			return false;
		}

		public override void Clear()
		{
			count = 0;
			entityIDToStorageIndex.Clear();
		}

		public ReadOnlySpan<TComponent> AllComponents()
		{
			return new ReadOnlySpan<TComponent>(components, count);
		}

		public Entity FirstEntity()
		{
#if DEBUG
			if (count == 0)
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

		protected virtual void Dispose(bool disposing)
		{
			if (!disposed)
			{
				NativeMemory.Free(components);
				NativeMemory.Free(entityIDs);
				components = null;
				entityIDs = null;

				disposed = true;
			}
		}

		~ComponentStorage()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: false);
		}

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}
