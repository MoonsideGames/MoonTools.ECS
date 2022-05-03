using System;
using System.Collections.Generic;

namespace MoonTools.ECS
{
	public struct FilterBuilder
	{
		private ComponentDepot ComponentDepot;
		private HashSet<Type> Included;
		private HashSet<Type> Excluded;

		internal FilterBuilder(ComponentDepot componentDepot)
		{
			ComponentDepot = componentDepot;
			Included = new HashSet<Type>();
			Excluded = new HashSet<Type>();
		}

		private FilterBuilder(ComponentDepot componentDepot, HashSet<Type> included, HashSet<Type> excluded)
		{
			ComponentDepot = componentDepot;
			Included = included;
			Excluded = excluded;
		}

		public FilterBuilder Include<TComponent>() where TComponent : unmanaged
		{
			ComponentDepot.Register<TComponent>();
			Included.Add(typeof(TComponent));
			return new FilterBuilder(ComponentDepot, Included, Excluded);
		}

		public FilterBuilder Exclude<TComponent>() where TComponent : unmanaged
		{
			ComponentDepot.Register<TComponent>();
			Excluded.Add(typeof(TComponent));
			return new FilterBuilder(ComponentDepot, Included, Excluded);
		}

		public Filter Build()
		{
			return ComponentDepot.CreateFilter(Included, Excluded);
		}
	}
}
