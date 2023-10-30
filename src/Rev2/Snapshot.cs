using System.Collections.Generic;
using MoonTools.ECS.Collections;

namespace MoonTools.ECS.Rev2;

public class Snapshot
{
	private Dictionary<ArchetypeSignature, ArchetypeSnapshot> ArchetypeSnapshots =
		new Dictionary<ArchetypeSignature, ArchetypeSnapshot>();

	private Dictionary<Id, Record> EntityIndex = new Dictionary<Id, Record>();
	private IdAssigner IdAssigner = new IdAssigner();

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
			RestoreArchetypeSnapshot(archetype);
		}

		// restore entity index
		world.EntityIndex.Clear();
		foreach (var (id, record) in EntityIndex)
		{
			world.EntityIndex[id] = record;
		}

		// restore id assigner state
		IdAssigner.CopyTo(world.IdAssigner);
	}

	public void Take(World world)
	{
		// copy id assigner state
		world.IdAssigner.CopyTo(IdAssigner);

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
	}

	internal void TakeArchetypeSnapshot(Archetype archetype)
	{
		if (!ArchetypeSnapshots.TryGetValue(archetype.Signature, out var archetypeSnapshot))
		{
			archetypeSnapshot = new ArchetypeSnapshot(archetype.Signature);
			ArchetypeSnapshots.Add(archetype.Signature, archetypeSnapshot);
		}

		archetypeSnapshot.Take(archetype);
	}

	private void RestoreArchetypeSnapshot(Archetype archetype)
	{
		var archetypeSnapshot = ArchetypeSnapshots[archetype.Signature];
		archetypeSnapshot.Restore(archetype);
	}

	private class ArchetypeSnapshot
	{
		public ArchetypeSignature Signature;
		private readonly Column[] ComponentColumns;
		private readonly NativeArray<Id> RowToEntity;

		public int Count => RowToEntity.Count;

		public ArchetypeSnapshot(ArchetypeSignature signature)
		{
			Signature = signature;
			ComponentColumns = new Column[signature.Count];
			RowToEntity = new NativeArray<Id>();

			for (int i = 0; i < signature.Count; i += 1)
			{
				var componentId = signature[i];
				ComponentColumns[i] = new Column(World.ElementSizes[componentId]);
			}
		}

		public void Clear()
		{
			RowToEntity.Clear();
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
}
