using System;
using System.Collections.Generic;
using MoonTools.ECS.Collections;

namespace MoonTools.ECS
{
	public class Filter
	{
		internal FilterSignature Signature;
		private FilterStorage FilterStorage;

		internal Filter(FilterStorage filterStorage, IndexableSet<int> included, IndexableSet<int> excluded)
		{
			FilterStorage = filterStorage;
			Signature = new FilterSignature(included, excluded);
		}

		public ReverseSpanEnumerator<Entity> Entities => FilterStorage.FilterEntities(Signature);
		public RandomEntityEnumerator EntitiesInRandomOrder => FilterStorage.FilterEntitiesRandom(Signature);
		public Entity RandomEntity => FilterStorage.FilterRandomEntity(Signature);

		public int Count => FilterStorage.FilterCount(Signature);
		public bool Empty => Count == 0;

		// WARNING: this WILL crash if the index is out of range!
		public Entity NthEntity(int index) => FilterStorage.FilterNthEntity(Signature, index);

		public void RegisterAddCallback(Action<Entity> callback)
		{
			FilterStorage.RegisterAddCallback(Signature, callback);
		}

		public void RegisterRemoveCallback(Action<Entity> callback)
		{
			FilterStorage.RegisterRemoveCallback(Signature, callback);
		}
	}
}
