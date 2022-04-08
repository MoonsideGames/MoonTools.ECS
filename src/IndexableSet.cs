﻿using System;
using System.Collections;
using System.Collections.Generic;

namespace MoonTools.ECS
{
	internal class IndexableSet<T> : IEnumerable<T> where T : notnull
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
			for (var i = 0; i < Count; i += 1)
			{
				yield return array[i];
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			for (var i = 0; i < Count; i += 1)
			{
				yield return array[i];
			}
		}
	}
}
