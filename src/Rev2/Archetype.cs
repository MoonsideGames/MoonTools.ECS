using System.Collections.Generic;

namespace MoonTools.ECS.Rev2;

internal class Archetype
{
	public World World;
	public ArchetypeSignature Signature;
	public List<Column> ComponentColumns = new List<Column>();
	public List<EntityId> RowToEntity = new List<EntityId>();

	public Dictionary<ComponentId, int> ComponentToColumnIndex =
		new Dictionary<ComponentId, int>();
	public SortedDictionary<ComponentId, ArchetypeEdge> Edges = new SortedDictionary<ComponentId, ArchetypeEdge>();

	public int Count => RowToEntity.Count;

	public Archetype(World world, ArchetypeSignature signature)
	{
		World = world;
		Signature = signature;
	}

	public void ClearAll()
	{
		for (int i = 0; i < ComponentColumns.Count; i += 1)
		{
			ComponentColumns[i].Count = 0;
		}

		foreach (var entityId in RowToEntity)
		{
			World.FreeEntity(entityId);
		}

		RowToEntity.Clear();
	}
}
