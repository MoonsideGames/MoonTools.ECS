using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MoonTools.ECS.Collections
{
	public unsafe class NativeArray<T> : IDisposable where T : unmanaged
	{
		private T* Array;
		private int count;
		private int capacity;

		public int Count => count;

		public ReverseSpanEnumerator<T> GetEnumerator() => new ReverseSpanEnumerator<T>(new Span<T>(Array, Count));

		private bool disposed;

		public NativeArray(int capacity = 16)
		{
			this.capacity = capacity;
			Array = (T*) NativeMemory.Alloc((nuint) (capacity * Unsafe.SizeOf<T>()));
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

		public void Clear()
		{
			count = 0;
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
}
