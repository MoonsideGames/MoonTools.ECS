using MoonTools.ECS.Collections;

namespace MoonTools.ECS
{
	public struct FilterBuilder
	{
		private TypeIndices ComponentTypeIndices;
		private FilterStorage FilterStorage;
		private IndexableSet<int> Included;
		private IndexableSet<int> Excluded;

		internal FilterBuilder(FilterStorage filterStorage, TypeIndices componentTypeIndices)
		{
			FilterStorage = filterStorage;
			ComponentTypeIndices = componentTypeIndices;
			Included = new IndexableSet<int>();
			Excluded = new IndexableSet<int>();
		}

		private FilterBuilder(FilterStorage filterStorage, TypeIndices componentTypeIndices, IndexableSet<int> included, IndexableSet<int> excluded)
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
