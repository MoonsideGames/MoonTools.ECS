using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MoonTools.ECS
{
	internal class IndexableSet<T> : IEnumerable<T> where T : unmanaged
	{
		private Dictionary<T, int> indices;
		private T[] array;
		public int Count { get; private set; }

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

		public IEnumerator<T> GetEnumerator()
		{
			for (var i = Count - 1; i >= 0; i -= 1)
			{
				yield return array[i];
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			for (var i = Count - 1; i >= 0; i -= 1)
			{
				yield return array[i];
			}
		}

		public void Save(IndexableSetState<T> state)
		{
			ReadOnlySpan<byte> arrayBytes = MemoryMarshal.Cast<T, byte>(array);

			if (arrayBytes.Length > state.Array.Length)
			{
				Array.Resize(ref state.Array, arrayBytes.Length);
			}

			arrayBytes.CopyTo(state.Array);

			state.Count = Count;
		}

		public void Load(IndexableSetState<T> state)
		{
			state.Array.CopyTo(MemoryMarshal.Cast<T, byte>(array));

			indices.Clear();
			for (var i = 0; i < state.Count; i += 1)
			{
				indices[array[i]] = i;
			}

			Count = state.Count;
		}
	}
}
