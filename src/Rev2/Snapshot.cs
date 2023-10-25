using System.Collections.Generic;

namespace MoonTools.ECS.Rev2;

public class Snapshot
{
	private Dictionary<ArchetypeSignature, ArchetypeSnapshot> ArchetypeSnapshots =
		new Dictionary<ArchetypeSignature, ArchetypeSnapshot>();

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
		foreach (var (archetypeSignature, archetypeSnapshot) in ArchetypeSnapshots)
		{
			var archetype = world.ArchetypeIndex[archetypeSignature];
			RestoreArchetypeSnapshot(archetype);
		}
	}

	internal void Reset()
	{
		foreach (var archetypeSnapshot in ArchetypeSnapshots.Values)
		{
			archetypeSnapshot.Count = 0;
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
		public readonly List<Column> ComponentColumns;

		public int Count;

		public ArchetypeSnapshot(ArchetypeSignature signature)
		{
			Signature = signature;
			ComponentColumns = new List<Column>(signature.Count);

			for (int i = 0; i < signature.Count; i += 1)
			{
				var componentId = signature[i];
				ComponentColumns.Add(new Column(World.ElementSizes[componentId]));
			}
		}

		public void Take(Archetype archetype)
		{
			for (int i = 0; i < ComponentColumns.Count; i += 1)
			{
				archetype.ComponentColumns[i].CopyAllTo(ComponentColumns[i]);
			}

			Count = archetype.Count;
		}

		public void Restore(Archetype archetype)
		{
			// Clear out existing entities
			archetype.ClearAll();

			// Copy all component data
			for (int i = 0; i < ComponentColumns.Count; i += 1)
			{
				ComponentColumns[i].CopyAllTo(archetype.ComponentColumns[i]);
			}

			// Clear the row to entity list
			archetype.RowToEntity.Clear();

			// Create new entities and repopulate the row to entity list
			for (int i = 0; i < Count; i += 1)
			{
				var entityId = archetype.World.CreateEntityOnArchetype(archetype);
				archetype.RowToEntity.Add(entityId);
			}
		}
	}
}
