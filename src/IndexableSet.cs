using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace MoonTools.ECS
{
	internal class IndexableSet<T> where T : unmanaged
	{
		private Dictionary<T, int> indices;
		private T[] array;
		public int Count { get; private set; }
        public Enumerator GetEnumerator() => new Enumerator(this);

		public IndexableSet(int size = 32)
		{
			indices = new Dictionary<T, int>(size);
			array = new T[size];
		}

		public T this[int i]
		{
			get { return array[i]; }
		}

		public bool Contains(T element)
		{
			return indices.ContainsKey(element);
		}

		public bool Add(T element)
		{
			if (!Contains(element))
			{
				indices.Add(element, Count);

				if (Count >= array.Length)
				{
					Array.Resize(ref array, array.Length * 2);
				}

				array[Count] = element;
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

			var lastElement = array[Count - 1];
			var index = indices[element];
			array[index] = lastElement;
			indices[lastElement] = index;
			Count -= 1;
			indices.Remove(element);

			return true;
		}

		public void Clear()
		{
			Count = 0;
		}

		public struct Enumerator
        {
            /// <summary>The set being enumerated.</summary>
            private readonly IndexableSet<T> _set;
            /// <summary>The next index to yield.</summary>
            private int _index;

            /// <summary>Initialize the enumerator.</summary>
            /// <param name="set">The set to enumerate.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Enumerator(IndexableSet<T> set)
            {
				_set = set;
				_index = _set.Count;
            }

            /// <summary>Advances the enumerator to the next element of the span.</summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                int index = _index - 1;
                if (index >= 0)
                {
                    _index = index;
                    return true;
                }

                return false;
            }

            /// <summary>Gets the element at the current position of the enumerator.</summary>
            public T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _set[_index];
            }
        }
	}
}
