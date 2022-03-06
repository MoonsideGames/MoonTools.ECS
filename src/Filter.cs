namespace MoonTools.ECS;

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
}
