using System;
using System.Collections.Generic;

namespace MoonTools.ECS
{
	public class TypeIndices
	{
		Dictionary<Type, int> TypeToIndex = new Dictionary<Type, int>();
		int nextID = 0;
		public int Count => TypeToIndex.Count;

		public int GetIndex<T>() where T : unmanaged
		{
			if (!TypeToIndex.ContainsKey(typeof(T)))
			{
				TypeToIndex.Add(typeof(T), nextID);
				nextID += 1;
			}

			return TypeToIndex[typeof(T)];
		}

		public int GetIndex(Type type)
		{
			return TypeToIndex[type];
		}


#if DEBUG
		public IEnumerable<Type> Types => TypeToIndex.Keys;
#endif
	}
}
