using MoonTools.ECS.Collections;

namespace MoonTools.ECS
{
	public struct FilterBuilder
	{
		World World;
		IndexableSet<TypeId> Included;
		IndexableSet<TypeId> Excluded;

		internal FilterBuilder(World world)
		{
			World = world;
			Included = new IndexableSet<TypeId>();
			Excluded = new IndexableSet<TypeId>();
		}

		private FilterBuilder(World world, IndexableSet<TypeId> included, IndexableSet<TypeId> excluded)
		{
			World = world;
			Included = included;
			Excluded = excluded;
		}

		public FilterBuilder Include<T>() where T : unmanaged
		{
			Included.Add(World.GetComponentTypeId<T>());
			return new FilterBuilder(World, Included, Excluded);
		}

		public FilterBuilder Exclude<T>() where T : unmanaged
		{
			Excluded.Add(World.GetComponentTypeId<T>());
			return new FilterBuilder(World, Included, Excluded);
		}

		public Filter Build()
		{
			var signature = new FilterSignature(Included, Excluded);
			return World.GetFilter(signature);
		}
	}
}
