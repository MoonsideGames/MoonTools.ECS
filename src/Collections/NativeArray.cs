using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MoonTools.ECS.Collections;

public unsafe class NativeArray<T> : IDisposable where T : unmanaged
{
	private T* Array;
	private int count;
	private int capacity;
	private int elementSize;

	public int Count => count;

	public Span<T>.Enumerator GetEnumerator() => new Span<T>(Array, count).GetEnumerator();

	private bool disposed;

	public NativeArray(int capacity = 16)
	{
		this.capacity = capacity;
		elementSize = Unsafe.SizeOf<T>();
		Array = (T*) NativeMemory.Alloc((nuint) (capacity * elementSize));
		count = 0;
	}

	public ref T this[int i] => ref Array[i];

	public void Add(T item)
	{
		if (count >= capacity)
		{
			capacity *= 2;
			Array = (T*) NativeMemory.Realloc(Array, (nuint) (capacity * Unsafe.SizeOf<T>()));
		}

		Array[count] = item;
		count += 1;
	}

	public void RemoveLastElement()
	{
		count -= 1;
	}

	public bool TryPop(out T element)
	{
		if (count > 0)
		{
			element = Array[count - 1];
			count -= 1;
			return true;
		}

		element = default;
		return false;
	}

	public void Clear()
	{
		count = 0;
	}

	private void ResizeTo(int size)
	{
		capacity = size;
		Array = (T*) NativeMemory.Realloc((void*) Array, (nuint) (elementSize * capacity));
	}

	public void CopyTo(NativeArray<T> other)
	{
		if (count >= other.capacity)
		{
			other.ResizeTo(Count);
		}

		NativeMemory.Copy(
			(void*) Array,
			(void*) other.Array,
			(nuint) (elementSize * Count)
		);

		other.count = count;
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!disposed)
		{
			NativeMemory.Free(Array);
			Array = null;

			disposed = true;
		}
	}

	~NativeArray()
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
