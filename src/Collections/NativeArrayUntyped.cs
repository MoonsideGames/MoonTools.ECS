using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MoonTools.ECS.Collections;

internal unsafe class NativeArray : IDisposable
{
	private nint Elements;
	public int Count { get; private set;}
	private int Capacity;
	public readonly int ElementSize;

	private bool IsDisposed;

	public NativeArray(int elementSize)
	{
		Capacity = 16;
		Count = 0;
		ElementSize = elementSize;

		Elements = (nint) NativeMemory.Alloc((nuint) (ElementSize * Capacity));
	}

	public Span<T> ToSpan<T>()
	{
		return new Span<T>((void*) Elements, Count);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ref T Get<T>(int i) where T : unmanaged
	{
		return ref ((T*) Elements)[i];
	}

	public void Set<T>(int i, in T element) where T : unmanaged
	{
		Unsafe.Write((void*) (Elements + ElementSize * i), element);
	}

	private void Resize()
	{
		Capacity *= 2;
		Elements = (nint) NativeMemory.Realloc((void*) Elements, (nuint) (ElementSize * Capacity));
	}

	private void ResizeTo(int capacity)
	{
		Capacity = capacity;
		Elements = (nint) NativeMemory.Realloc((void*) Elements, (nuint) (ElementSize * Capacity));
	}

	// Fills gap by copying final element to the deleted index
	public void Delete(int index)
	{
		if (index != Count - 1)
		{
			NativeMemory.Copy(
				(void*) (Elements + ((Count - 1) * ElementSize)),
				(void*) (Elements + (index * ElementSize)),
				(nuint) ElementSize
			);
		}

		Count -= 1;
	}

	public void Append<T>(T component) where T : unmanaged
	{
		if (Count >= Capacity)
		{
			Resize();
		}

		((T*) Elements)[Count] = component;
		Count += 1;
	}

	public void CopyAllTo(NativeArray other)
	{
		if (Count >= other.Capacity)
		{
			other.ResizeTo(Count);
		}

		NativeMemory.Copy(
			(void*) Elements,
			(void*) other.Elements,
			(nuint) (ElementSize * Count)
		);

		other.Count = Count;
	}

	public void Clear()
	{
		Count = 0;
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!IsDisposed)
		{
			NativeMemory.Free((void*) Elements);
			IsDisposed = true;
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
