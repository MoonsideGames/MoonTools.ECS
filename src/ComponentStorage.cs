using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MoonTools.ECS.Collections;

namespace MoonTools.ECS;

public class ComponentStorage : IDisposable
{
	internal readonly NativeArray<Entity> DenseArray = new NativeArray<Entity>();
	internal readonly NativeArray<int> SparseArray = new NativeArray<int>();
	internal nint ElementArray;
	internal int ElementArrayCapacity;
	private bool IsDisposed;

	public int ElementSize { get; private set; }
	public int Count => DenseArray.Count;

	public unsafe ComponentStorage(int elementSize)
	{
		for (var i = 0; i < 16; i += 1)
		{
			SparseArray.Append(Entity.Null.ID); // sentinel value
		}

		ElementArrayCapacity = 16;
		ElementArray = (nint) NativeMemory.Alloc((nuint) (elementSize * ElementArrayCapacity));
		ElementSize = elementSize;
	}

	public bool Any() => DenseArray.Count > 0;

	public unsafe ref T Get<T>(Entity entity) where T : unmanaged
	{
		return ref ((T*) ElementArray)[SparseArray[entity.ID]];
	}

	public unsafe ref T GetFirst<T>() where T : unmanaged
	{
#if DEBUG
		if (DenseArray.Count == 0)
		{
			throw new IndexOutOfRangeException("Component storage is empty!");
		}
#endif
		return ref ((T*) ElementArray)[0];
	}

	public unsafe bool Set<T>(Entity entity, T component) where T : unmanaged
	{
		var newEntity = entity.ID >= SparseArray.Count || SparseArray[entity.ID] == Entity.Null.ID;
		if (newEntity)
		{
			// the entity is being added! let's do some record keeping
			var index = DenseArray.Count;
			DenseArray.Append(entity);
			if (entity.ID >= SparseArray.Count)
			{
				var oldCount = SparseArray.Count;
				SparseArray.ResizeTo(entity.ID + 1);
				for (var i = oldCount; i < SparseArray.Capacity; i += 1)
				{
					SparseArray.Append(Entity.Null.ID); // sentinel value
				}
			}
			SparseArray[entity.ID] = index;

			if (entity.ID >= ElementArrayCapacity)
			{
				ElementArrayCapacity = entity.ID + 1;
				ElementArray = (nint) NativeMemory.Realloc((void*) ElementArray, (nuint) (ElementArrayCapacity * ElementSize));
			}
		}

		Unsafe.Write((void*) (ElementArray + ElementSize * SparseArray[entity.ID]), component);
		return !newEntity;
	}

	public bool Has(Entity entity)
	{
		return entity.ID < SparseArray.Count && SparseArray[entity.ID] != Entity.Null.ID;
	}

	public unsafe bool Remove(Entity entity)
	{
		if (Has(entity))
		{
			var denseIndex = SparseArray[entity.ID];
			var lastItem = DenseArray[DenseArray.Count - 1];
		 	DenseArray[denseIndex] = lastItem;
			SparseArray[lastItem.ID] = denseIndex;
			SparseArray[entity.ID] = Entity.Null.ID; // sentinel value

			if (denseIndex != DenseArray.Count - 1)
			{
				NativeMemory.Copy((void*) (ElementArray + ElementSize * (DenseArray.Count - 1)), (void*) (ElementArray + ElementSize * denseIndex), (nuint) ElementSize);
			}

			DenseArray.RemoveLastElement();



			return true;
		}

		return false;
	}

	public void Clear()
	{
		DenseArray.Clear();
		for (var i = 0; i < SparseArray.Capacity; i += 1)
		{
			SparseArray[i] = Entity.Null.ID;
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

	protected unsafe virtual void Dispose(bool disposing)
	{
		if (!IsDisposed)
		{
			if (disposing)
			{
				DenseArray.Dispose();
				SparseArray.Dispose();
			}

			NativeMemory.Free((void*) ElementArray);

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
