using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MoonTools.ECS.Collections;

namespace MoonTools.ECS.Rev2;

public class Snapshot
{
	private Dictionary<ArchetypeSignature, ArchetypeSnapshot> ArchetypeSnapshots =
		new Dictionary<ArchetypeSignature, ArchetypeSnapshot>();

	private Dictionary<TypeId, RelationSnapshot> RelationSnapshots =
		new Dictionary<TypeId, RelationSnapshot>();

	private Dictionary<EntityId, Record> EntityIndex = new Dictionary<EntityId, Record>();

	private Dictionary<EntityId, IndexableSet<TypeId>> EntityRelationIndex =
		new Dictionary<EntityId, IndexableSet<TypeId>>();

	private IdAssigner EntityIdAssigner = new IdAssigner();

	public int Count
	{
		get
		{
			var count = 0;

			foreach (var snapshot in ArchetypeSnapshots.Values)
			{
				count += snapshot.Count;
			}

			return count;
		}
	}

	public void Restore(World world)
	{
		// restore archetype storage
		foreach (var (archetypeSignature, archetypeSnapshot) in ArchetypeSnapshots)
		{
			var archetype = world.ArchetypeIndex[archetypeSignature];
			archetypeSnapshot.Restore(archetype);
		}

		// restore entity index
		world.EntityIndex.Clear();
		foreach (var (id, record) in EntityIndex)
		{
			world.EntityIndex[id] = record;
		}

		// restore id assigner state
		EntityIdAssigner.CopyTo(world.EntityIdAssigner);

		// restore relation state
		foreach (var (typeId, relationSnapshot) in RelationSnapshots)
		{
			var relationStorage = world.RelationIndex[typeId];
			relationSnapshot.Restore(relationStorage);
		}

		// restore entity relation index state
		// FIXME: arghhhh this is so slow
		foreach (var (id, relationTypeSet) in EntityRelationIndex)
		{
			world.EntityRelationIndex[id].Clear();

			foreach (var typeId in relationTypeSet)
			{
				world.EntityRelationIndex[id].Add(typeId);
			}
		}
	}

	public void Take(World world)
	{
		// copy id assigner state
		world.EntityIdAssigner.CopyTo(EntityIdAssigner);

		// copy entity index
		EntityIndex.Clear();
		foreach (var (id, record) in world.EntityIndex)
		{
			EntityIndex[id] = record;
		}

		// copy archetypes
		foreach (var archetype in world.ArchetypeIndex.Values)
		{
			TakeArchetypeSnapshot(archetype);
		}

		// copy relations
		foreach (var (typeId, relationStorage) in world.RelationIndex)
		{
			TakeRelationSnapshot(typeId, relationStorage);
		}

		// copy entity relation index
		// FIXME: arghhhh this is so slow
		foreach (var (id, relationTypeSet) in world.EntityRelationIndex)
		{
			if (!EntityRelationIndex.ContainsKey(id))
			{
				EntityRelationIndex.Add(id, new IndexableSet<TypeId>());
			}

			EntityRelationIndex[id].Clear();

			foreach (var typeId in relationTypeSet)
			{
				EntityRelationIndex[id].Add(typeId);
			}
		}
	}

	private void TakeArchetypeSnapshot(Archetype archetype)
	{
		if (!ArchetypeSnapshots.TryGetValue(archetype.Signature, out var archetypeSnapshot))
		{
			archetypeSnapshot = new ArchetypeSnapshot(archetype.Signature);
			ArchetypeSnapshots.Add(archetype.Signature, archetypeSnapshot);
		}

		archetypeSnapshot.Take(archetype);
	}

	private void TakeRelationSnapshot(TypeId typeId, RelationStorage relationStorage)
	{
		if (!RelationSnapshots.TryGetValue(typeId, out var snapshot))
		{
			snapshot = new RelationSnapshot(World.ElementSizes[typeId]);
			RelationSnapshots.Add(typeId, snapshot);
		}

		snapshot.Take(relationStorage);
	}

	private class ArchetypeSnapshot
	{
		private readonly NativeArray[] ComponentColumns;
		private readonly NativeArray<EntityId> RowToEntity;

		public int Count => RowToEntity.Count;

		public ArchetypeSnapshot(ArchetypeSignature signature)
		{
			ComponentColumns = new NativeArray[signature.Count];
			RowToEntity = new NativeArray<EntityId>();

			for (int i = 0; i < signature.Count; i += 1)
			{
				var componentId = signature[i];
				ComponentColumns[i] = new NativeArray(World.ElementSizes[componentId]);
			}
		}

		public void Take(Archetype archetype)
		{
			for (int i = 0; i < ComponentColumns.Length; i += 1)
			{
				archetype.ComponentColumns[i].CopyAllTo(ComponentColumns[i]);
			}

			archetype.RowToEntity.CopyTo(RowToEntity);
		}

		public void Restore(Archetype archetype)
		{
			// Copy all component data
			for (int i = 0; i < ComponentColumns.Length; i += 1)
			{
				ComponentColumns[i].CopyAllTo(archetype.ComponentColumns[i]);
			}

			RowToEntity.CopyTo(archetype.RowToEntity);
		}
	}

	private class RelationSnapshot
	{
		private NativeArray Relations;
		private NativeArray RelationDatas;

		public RelationSnapshot(int elementSize)
		{
			Relations = new NativeArray(Unsafe.SizeOf<(EntityId, EntityId)>());
			RelationDatas = new NativeArray(elementSize);
		}

		public void Take(RelationStorage relationStorage)
		{
			relationStorage.relations.CopyAllTo(Relations);
			relationStorage.relationDatas.CopyAllTo(RelationDatas);
		}

		public void Restore(RelationStorage relationStorage)
		{
			relationStorage.Clear();

			Relations.CopyAllTo(relationStorage.relations);
			RelationDatas.CopyAllTo(relationStorage.relationDatas);

			for (int index = 0; index < Relations.Count; index += 1)
			{
				var relation = Relations.Get<(EntityId, EntityId)>(index);
				relationStorage.indices[relation] = index;

				relationStorage.indices[relation] = index;

				if (!relationStorage.outRelations.ContainsKey(relation.Item1))
				{
					relationStorage.outRelations[relation.Item1] =
						relationStorage.AcquireHashSetFromPool();
				}

				relationStorage.outRelations[relation.Item1].Add(relation.Item2);

				if (!relationStorage.inRelations.ContainsKey(relation.Item2))
				{
					relationStorage.inRelations[relation.Item2] =
						relationStorage.AcquireHashSetFromPool();
				}

				relationStorage.inRelations[relation.Item2].Add(relation.Item1);
			}
		}
	}
}
