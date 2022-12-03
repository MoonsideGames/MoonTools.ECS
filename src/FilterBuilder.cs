using System;
using System.Collections.Generic;

namespace MoonTools.ECS
{
	public struct FilterBuilder
	{
		private TypeIndices ComponentTypeIndices;
		private FilterStorage FilterStorage;
		private HashSet<int> Included;
		private HashSet<int> Excluded;

		internal FilterBuilder(FilterStorage filterStorage, TypeIndices componentTypeIndices)
		{
			FilterStorage = filterStorage;
			ComponentTypeIndices = componentTypeIndices;
			Included = new HashSet<int>();
			Excluded = new HashSet<int>();
		}

		private FilterBuilder(FilterStorage filterStorage, TypeIndices componentTypeIndices, HashSet<int> included, HashSet<int> excluded)
		{
			FilterStorage = filterStorage;
			ComponentTypeIndices = componentTypeIndices;
			Included = included;
			Excluded = excluded;
		}

		public FilterBuilder Include<TComponent>() where TComponent : unmanaged
		{
			Included.Add(ComponentTypeIndices.GetIndex<TComponent>());
			return new FilterBuilder(FilterStorage, ComponentTypeIndices, Included, Excluded);
		}

		public FilterBuilder Exclude<TComponent>() where TComponent : unmanaged
		{
			Excluded.Add(ComponentTypeIndices.GetIndex<TComponent>());
			return new FilterBuilder(FilterStorage, ComponentTypeIndices, Included, Excluded);
		}

		public Filter Build()
		{
			return FilterStorage.CreateFilter(Included, Excluded);
		}
	}
}
