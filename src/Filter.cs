using System;
using System.Collections.Generic;

namespace MoonTools.ECS
{
	public class Filter
	{
		internal FilterSignature Signature;
		private FilterStorage FilterStorage;

		internal Filter(FilterStorage filterStorage, HashSet<int> included, HashSet<int> excluded)
		{
			FilterStorage = filterStorage;
			Signature = new FilterSignature(included, excluded);
		}

		public ReverseSpanEnumerator<Entity> Entities => FilterStorage.FilterEntities(Signature);
		public LinearCongruentialEnumerator EntitiesInRandomOrder => FilterStorage.FilterEntitiesRandom(Signature);
		public Entity RandomEntity => FilterStorage.FilterRandomEntity(Signature);

		public int Count => FilterStorage.FilterCount(Signature);
		public bool Empty => Count == 0;

		// WARNING: this WILL crash if the index is out of range!
		public Entity NthEntity(int index) => FilterStorage.FilterNthEntity(Signature, index);
	}
}
