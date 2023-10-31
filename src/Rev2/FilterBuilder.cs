using System.Collections.Generic;

namespace MoonTools.ECS.Rev2;

public ref struct FilterBuilder
{
	World World;
	HashSet<TypeId> Included;
	HashSet<TypeId> Excluded;

	internal FilterBuilder(World world)
	{
		World = world;
		Included = new HashSet<TypeId>();
		Excluded = new HashSet<TypeId>();
	}

	private FilterBuilder(World world, HashSet<TypeId> included, HashSet<TypeId> excluded)
	{
		World = world;
		Included = included;
		Excluded = excluded;
	}

	public FilterBuilder Include<T>() where T : unmanaged
	{
		Included.Add(World.GetTypeId<T>());
		return new FilterBuilder(World, Included, Excluded);
	}

	public FilterBuilder Exclude<T>() where T : unmanaged
	{
		Excluded.Add(World.GetTypeId<T>());
		return new FilterBuilder(World, Included, Excluded);
	}

	public Filter Build()
	{
		return new Filter(World.EmptyArchetype, Included, Excluded);
	}
}
