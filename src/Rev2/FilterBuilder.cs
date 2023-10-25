using System.Collections.Generic;

namespace MoonTools.ECS.Rev2
{
	public ref struct FilterBuilder
	{
		World World;
		HashSet<ComponentId> Included;
		HashSet<ComponentId> Excluded;

		internal FilterBuilder(World world)
		{
			World = world;
			Included = new HashSet<ComponentId>();
			Excluded = new HashSet<ComponentId>();
		}

		private FilterBuilder(World world, HashSet<ComponentId> included, HashSet<ComponentId> excluded)
		{
			World = world;
			Included = included;
			Excluded = excluded;
		}

		public FilterBuilder Include<T>() where T : unmanaged
		{
			Included.Add(World.TypeToComponentId[typeof(T)]);
			return new FilterBuilder(World, Included, Excluded);
		}

		public FilterBuilder Exclude<T>() where T : unmanaged
		{
			Excluded.Add(World.TypeToComponentId[typeof(T)]);
			return new FilterBuilder(World, Included, Excluded);
		}

		public Filter Build()
		{
			return new Filter(World.EmptyArchetype, Included, Excluded);
		}
	}
}
