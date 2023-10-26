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
			// Copy all component data
			for (int i = 0; i < ComponentColumns.Count; i += 1)
			{
				ComponentColumns[i].CopyAllTo(archetype.ComponentColumns[i]);
			}

			var archetypeCount = archetype.Count;

			if (Count < archetypeCount)
			{
				// if snapshot has fewer entities than archetype, remove extra entities
				for (int i = archetypeCount - 1; i >= Count; i -= 1)
				{
					archetype.World.FreeEntity(archetype.RowToEntity[i]);
					archetype.RowToEntity.RemoveAt(i);
				}
			}
			else if (Count > archetypeCount)
			{
				// if snapshot has more entities than archetype, add entities
				for (int i = archetypeCount; i < Count; i += 1)
				{
					archetype.World.CreateEntityOnArchetype(archetype);
				}
			}
		}
	}
}
