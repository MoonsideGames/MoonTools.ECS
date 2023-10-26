using System.Collections.Generic;

namespace MoonTools.ECS.Rev2;

internal class Archetype
{
	public World World;
	public ArchetypeSignature Signature;
	public List<Column> ComponentColumns = new List<Column>();
	public List<Id> RowToEntity = new List<Id>();

	public Dictionary<Id, int> ComponentToColumnIndex =
		new Dictionary<Id, int>();
	public SortedDictionary<Id, ArchetypeEdge> Edges = new SortedDictionary<Id, ArchetypeEdge>();

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
