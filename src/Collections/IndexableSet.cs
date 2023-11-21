using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MoonTools.ECS.Collections;

public unsafe class IndexableSet<T> : IDisposable where T : unmanaged
{
	private Dictionary<T, int> Indices;
	private T* Array;
	public int Count { get; private set; }
	private int Capacity;
	private bool IsDisposed;

	public Span<T> AsSpan() => new Span<T>(Array, Count);
	public ReverseSpanEnumerator<T> GetEnumerator() => new ReverseSpanEnumerator<T>(new Span<T>(Array, Count));

	public IndexableSet(int capacity = 32)
	{
		this.Capacity = capacity;
		Count = 0;

		Indices = new Dictionary<T, int>(capacity);
		Array = (T*) NativeMemory.Alloc((nuint) (capacity * Unsafe.SizeOf<T>()));
	}

	public T this[int i] => Array[i];

	public bool Contains(T element)
	{
		return Indices.ContainsKey(element);
	}

	public bool Add(T element)
	{
		if (!Contains(element))
		{
			Indices.Add(element, Count);

			if (Count >= Capacity)
			{
				Capacity *= 2;
				Array = (T*) NativeMemory.Realloc(Array, (nuint) (Capacity * Unsafe.SizeOf<T>()));
			}

			Array[Count] = element;
			Count += 1;

			return true;
		}

		return false;
	}

	public bool Remove(T element)
	{
		if (!Contains(element))
		{
			return false;
		}

		var index = Indices[element];

		for (var i = index; i < Count - 1; i += 1)
		{
			Array[i] = Array[i + 1];
			Indices[Array[i]] = i;
		}

		Indices.Remove(element);
		Count -= 1;

		return true;
	}

	public void Clear()
	{
		Indices.Clear();
		Count = 0;
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!IsDisposed)
		{
			NativeMemory.Free(Array);
			Array = null;

			IsDisposed = true;
		}
	}

	~IndexableSet()
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
