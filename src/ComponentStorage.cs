using System;
using System.Collections.Generic;
using MoonTools.ECS.Collections;

namespace MoonTools.ECS;

internal class ComponentStorage : IDisposable
{
	internal readonly Dictionary<Entity, int> EntityIDToStorageIndex = new Dictionary<Entity, int>(16);
	internal readonly NativeArray Components;
	internal readonly NativeArray<Entity> EntityIDs;
	internal readonly TypeId TypeId;
	internal readonly int ElementSize;

	private bool IsDisposed;

	public ComponentStorage(TypeId typeId, int elementSize)
	{
		ElementSize = elementSize;
		Components = new NativeArray(elementSize);
		EntityIDs = new NativeArray<Entity>();
		TypeId = typeId;
	}

	public bool Any()
	{
		return Components.Count > 0;
	}

	public bool Has(Entity entity)
	{
		return EntityIDToStorageIndex.ContainsKey(entity);
	}

	public ref T Get<T>(in Entity entity) where T : unmanaged
	{
		return ref Components.Get<T>(EntityIDToStorageIndex[entity]);
	}

	public ref T GetFirst<T>() where T : unmanaged
	{
#if DEBUG
		if (Components.Count == 0)
		{
			throw new IndexOutOfRangeException("Component storage is empty!");
		}
#endif
		return ref Components.Get<T>(0);
	}

	// Returns true if the entity had this component.
	public bool Set<T>(in Entity entity, in T component) where T : unmanaged
	{
		if (EntityIDToStorageIndex.TryGetValue(entity, out var index))
		{
			Components.Set(index, component);
			return true;
		}
		else
		{
			EntityIDToStorageIndex[entity] = Components.Count;
			EntityIDs.Append(entity);
			Components.Append(component);
			return false;
		}
	}

	// Returns true if the entity had this component.
	public bool Remove(in Entity entity)
	{
		if (EntityIDToStorageIndex.TryGetValue(entity, out int index))
		{
			var lastElementIndex = Components.Count - 1;

			var lastEntity = EntityIDs[lastElementIndex];

			// move a component into the hole to maintain contiguous memory
			Components.Delete(index);
			EntityIDs.Delete(index);
			EntityIDToStorageIndex.Remove(entity);

			// update the index if it changed
			if (lastElementIndex != index)
			{
				EntityIDToStorageIndex[lastEntity] = index;
			}

			return true;
		}

		return false;
	}

	public void Clear()
	{
		Components.Clear();
		EntityIDs.Clear();
		EntityIDToStorageIndex.Clear();
	}

	public Entity FirstEntity()
	{
#if DEBUG
		if (EntityIDs.Count == 0)
		{
			throw new IndexOutOfRangeException("Component storage is empty!");
		}
#endif
		return EntityIDs[0];
	}

#if DEBUG
	internal IEnumerable<Entity> Debug_GetEntities()
	{
		return EntityIDToStorageIndex.Keys;
	}
#endif

	protected virtual void Dispose(bool disposing)
	{
		if (!IsDisposed)
		{
			Components.Dispose();
			EntityIDs.Dispose();

			IsDisposed = true;
		}
	}

	// ~ComponentStorage()
	// {
	// 	// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
	// 	Dispose(disposing: false);
	// }

	public void Dispose()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}
