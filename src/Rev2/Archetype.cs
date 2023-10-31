using System.Collections.Generic;
using MoonTools.ECS.Collections;

namespace MoonTools.ECS.Rev2;

internal class Archetype
{
	public World World;
	public ArchetypeSignature Signature;
	public NativeArray[] ComponentColumns;
	public NativeArray<EntityId> RowToEntity = new NativeArray<EntityId>();

	public Dictionary<TypeId, int> ComponentToColumnIndex =
		new Dictionary<TypeId, int>();
	public SortedDictionary<TypeId, ArchetypeEdge> Edges = new SortedDictionary<TypeId, ArchetypeEdge>();

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
