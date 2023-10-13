using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MoonTools.ECS.Collections
{
	public unsafe class IndexableSet<T> : IDisposable where T : unmanaged
	{
		private Dictionary<T, int> indices;
		private T* array;
		private int count;
		private int capacity;
		private bool disposed;

		public int Count => count;
		public ReverseSpanEnumerator<T> GetEnumerator() => new ReverseSpanEnumerator<T>(new Span<T>(array, count));

		public IndexableSet(int capacity = 32)
		{
			this.capacity = capacity;
			count = 0;

			indices = new Dictionary<T, int>(capacity);
			array = (T*) NativeMemory.Alloc((nuint) (capacity * Unsafe.SizeOf<T>()));
		}

		public T this[int i] => array[i];

		public bool Contains(T element)
		{
			return indices.ContainsKey(element);
		}

		public bool Add(T element)
		{
			if (!Contains(element))
			{
				indices.Add(element, count);

				if (count >= capacity)
				{
					capacity *= 2;
					array = (T*) NativeMemory.Realloc(array, (nuint) (capacity * Unsafe.SizeOf<T>()));
				}

				array[count] = element;
				count += 1;

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

			var lastElement = array[Count - 1];
			var index = indices[element];
			array[index] = lastElement;
			indices[lastElement] = index;
			count -= 1;
			indices.Remove(element);

			return true;
		}

		public void Clear()
		{
			indices.Clear();
			count = 0;
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposed)
			{
				NativeMemory.Free(array);
				array = null;

				disposed = true;
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
}
