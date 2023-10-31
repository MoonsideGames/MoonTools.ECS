using System.Collections.Generic;
using MoonTools.ECS.Collections;

namespace MoonTools.ECS.Rev2;

internal class Archetype
{
	public World World;
	public ArchetypeSignature Signature;
	public NativeArray[] ComponentColumns;
	public NativeArray<Id> RowToEntity = new NativeArray<Id>();

	public Dictionary<Id, int> ComponentToColumnIndex =
		new Dictionary<Id, int>();
	public SortedDictionary<Id, ArchetypeEdge> Edges = new SortedDictionary<Id, ArchetypeEdge>();

	public int Count => RowToEntity.Count;

	public Archetype(World world, ArchetypeSignature signature)
	{
		World = world;
		Signature = signature;
		ComponentColumns = new NativeArray[signature.Count];
	}

	public void ClearAll()
	{
		for (int i = 0; i < ComponentColumns.Length; i += 1)
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
