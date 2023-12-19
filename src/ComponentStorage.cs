using System;
using MoonTools.ECS.Collections;

namespace MoonTools.ECS;

public class ComponentStorage : IDisposable
{
	internal readonly NativeArray<Entity> DenseArray = new NativeArray<Entity>();
	internal readonly NativeArray<Entity> SparseArray = new NativeArray<Entity>();
	internal readonly NativeArray ElementArray;
	private bool IsDisposed;

	public int ElementSize { get; private set; }
	public int Count => DenseArray.Count;

	public ComponentStorage(int elementSize)
	{
		for (var i = 0; i < 16; i += 1)
		{
			SparseArray.Append(Entity.Null); // sentinel value
		}

		ElementArray = new NativeArray(elementSize);
		ElementSize = elementSize;
	}

	public bool Any() => DenseArray.Count > 0;

	public ref T Get<T>(Entity entity) where T : unmanaged
	{
		if (entity.ID >= ElementArray.Capacity)
		{
			throw new Exception("oh noes");
		}
		return ref ElementArray.Get<T>(entity.ID);
	}

	public ref T GetFirst<T>() where T : unmanaged
	{
#if DEBUG
		if (DenseArray.Count == 0)
		{
			throw new IndexOutOfRangeException("Component storage is empty!");
		}
#endif
		return ref ElementArray.Get<T>(DenseArray[0].ID);
	}

	public bool Set<T>(Entity entity, T component) where T : unmanaged
	{
		var newEntity = SparseArray[entity.ID] == Entity.Null;
		if (newEntity)
		{
			// the entity is being added! let's do some record keeping
			var index = DenseArray.Count;
			DenseArray.Append(entity);
			if (entity.ID >= SparseArray.Count)
			{
				var oldCount = SparseArray.Count;
				SparseArray.ResizeTo(entity.ID + 1);
				for (var i = oldCount; i < SparseArray.Count; i += 1)
				{
					SparseArray.Append(Entity.Null); // sentinel value
				}
			}
			SparseArray[entity.ID] = entity;

			// FIXME: something is not right here
			if (entity.ID >= ElementArray.Count)
			{
				ElementArray.ResizeTo(entity.ID + 1);
			}
		}

		ElementArray.Set(entity.ID, component);
		return !newEntity;
	}

	public bool Has(Entity entity)
	{
		return SparseArray[entity.ID] != Entity.Null;
	}

	public bool Remove(Entity entity)
	{
		if (Has(entity))
		{
			var denseIndex = SparseArray[entity.ID];
			var lastItem = DenseArray[DenseArray.Count - 1];
		 	DenseArray[denseIndex.ID] = lastItem;
			SparseArray[lastItem.ID] = denseIndex;
			SparseArray[entity.ID] = Entity.Null; // sentinel value
			DenseArray.RemoveLastElement();
			ElementArray.RemoveLastElement();

			return true;
		}

		return false;
	}

	public void Clear()
	{
		DenseArray.Clear();
		ElementArray.Clear();
		for (var i = 0; i < SparseArray.Count; i += 1)
		{
			SparseArray[i] = Entity.Null;
		}
	}

	public Entity FirstEntity()
	{
#if DEBUG
		if (DenseArray.Count == 0)
		{
			throw new IndexOutOfRangeException("Component storage is empty!");
		}
#endif
		return DenseArray[0];
	}

#if DEBUG
	internal Span<Entity> Debug_GetEntities()
	{
		return DenseArray.ToSpan();
	}
#endif

	protected virtual void Dispose(bool disposing)
	{
		if (!IsDisposed)
		{
			if (disposing)
			{
				DenseArray.Dispose();
				SparseArray.Dispose();
				ElementArray.Dispose();
			}

			IsDisposed = true;
		}
	}

	public void Dispose()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}
