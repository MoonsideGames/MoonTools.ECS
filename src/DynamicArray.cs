using System;

namespace MoonTools.ECS
{
	public class DynamicArray<T> where T : unmanaged
	{
		private T[] Array;
		public int Count { get; private set; }

		public Span<T> ToSpan() => new Span<T>(Array, 0, Count);
		public ReverseSpanEnumerator<T> GetEnumerator() => new ReverseSpanEnumerator<T>(new Span<T>(Array, 0, Count));

		public DynamicArray(int capacity = 16)
		{
			Array = new T[capacity];
			Count = 0;
		}

		public ref T this[int i]
		{
			get { return ref Array[i]; }
		}

		public void Add(T item)
		{
			if (Count >= Array.Length)
			{
				global::System.Array.Resize(ref Array, Array.Length * 2);
			}

			Array[Count] = item;
			Count += 1;
		}

		public void Clear()
		{
			Count = 0;
		}
	}
}
