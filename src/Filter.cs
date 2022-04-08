using System;
using System.Collections.Generic;

namespace MoonTools.ECS
{
	public class Filter
	{
		internal FilterSignature Signature;
		private ComponentDepot ComponentDepot;

		internal Filter(ComponentDepot componentDepot, HashSet<Type> included, HashSet<Type> excluded)
		{
			ComponentDepot = componentDepot;
			Signature = new FilterSignature(included, excluded);
		}

		public IEnumerable<Entity> Entities => ComponentDepot.FilterEntities(this);
		public IEnumerable<Entity> EntitiesInRandomOrder => ComponentDepot.FilterEntitiesRandom(this);
		public Entity RandomEntity => ComponentDepot.FilterRandomEntity(this);

		public int Count => ComponentDepot.FilterCount(this);
		public bool Empty => Count == 0;
	}
}
