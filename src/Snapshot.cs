using System;
using System.Collections.Generic;

namespace MoonTools.ECS
{
	public class Snapshot
	{
		private World World;
		private Filter? Filter;

		private EntityStorage SnapshotEntityStorage;
		private ComponentDepot SnapshotComponentDepot;
		private RelationDepot SnapshotRelationDepot;

		private List<int> SnapshotToWorldID = new List<int>();
		private Dictionary<int, int> WorldToSnapshotID = new Dictionary<int, int>();

		internal Snapshot(World world)
		{
			World = world;
			SnapshotEntityStorage = new EntityStorage();
			SnapshotComponentDepot = new ComponentDepot(World.ComponentTypeIndices);
			SnapshotRelationDepot = new RelationDepot(World.RelationTypeIndices);
		}

		public unsafe void Take(Filter filter)
		{
			Clear();
			Filter = filter;
			SnapshotComponentDepot.CreateMissingStorages(World.ComponentDepot);
			SnapshotRelationDepot.CreateMissingStorages(World.RelationDepot);

			foreach (var worldEntity in filter.Entities)
			{
				var snapshotEntity = SnapshotEntityStorage.Create();
				WorldToSnapshotID.Add(worldEntity.ID, snapshotEntity.ID);

				foreach (var componentTypeIndex in World.EntityStorage.ComponentTypeIndices(worldEntity.ID))
				{
					SnapshotEntityStorage.SetComponent(snapshotEntity.ID, componentTypeIndex);
					SnapshotComponentDepot.Set(snapshotEntity.ID, componentTypeIndex, World.ComponentDepot.UntypedGet(worldEntity.ID, componentTypeIndex));
				}
			}

			foreach (var worldEntity in filter.Entities)
			{
				var snapshotEntityID = WorldToSnapshotID[worldEntity.ID];

				foreach (var relationTypeIndex in World.EntityStorage.RelationTypeIndices(worldEntity.ID))
				{
					SnapshotEntityStorage.AddRelationKind(snapshotEntityID, relationTypeIndex);

					foreach (var (otherEntityID, relationStorageIndex) in World.RelationDepot.OutRelationIndices(worldEntity.ID, relationTypeIndex))
					{
#if DEBUG
						if (!World.FilterStorage.CheckSatisfied(otherEntityID, Filter.Signature))
						{
							throw new InvalidOperationException($"Snapshot entity {worldEntity.ID} is related to non-snapshot entity {otherEntityID}!");
						}
#endif
						var otherSnapshotID = WorldToSnapshotID[otherEntityID];
						SnapshotEntityStorage.AddRelationKind(otherSnapshotID, relationTypeIndex);
						SnapshotRelationDepot.Set(snapshotEntityID, otherSnapshotID, relationTypeIndex, World.RelationDepot.Get(relationTypeIndex, relationStorageIndex));
					}
				}
			}
		}

		public unsafe void Restore()
		{
			if (Filter == null)
			{
				return;
			}

			foreach (var entity in Filter.Entities)
			{
				World.Destroy(entity);
			}

			for (var i = 0; i < SnapshotEntityStorage.Count; i += 1)
			{
				var entity = World.CreateEntity();
				SnapshotToWorldID.Add(entity.ID);

				foreach (var componentTypeIndex in SnapshotEntityStorage.ComponentTypeIndices(i))
				{
					World.EntityStorage.SetComponent(entity.ID, componentTypeIndex);
					World.FilterStorage.Check(entity.ID, componentTypeIndex);
					World.ComponentDepot.Set(entity.ID, componentTypeIndex, SnapshotComponentDepot.UntypedGet(i, componentTypeIndex));
				}
			}

			for (var i = 0; i < SnapshotEntityStorage.Count; i += 1)
			{
				var worldID = SnapshotToWorldID[i];

				foreach (var relationTypeIndex in SnapshotEntityStorage.RelationTypeIndices(i))
				{
					World.EntityStorage.AddRelationKind(worldID, relationTypeIndex);

					foreach (var (otherEntityID, relationStorageIndex) in SnapshotRelationDepot.OutRelationIndices(i, relationTypeIndex))
					{
						var otherEntityWorldID = SnapshotToWorldID[otherEntityID];
						World.RelationDepot.Set(worldID, otherEntityWorldID, relationTypeIndex, SnapshotRelationDepot.Get(relationTypeIndex, relationStorageIndex));
					}
				}
			}
		}

		private void Clear()
		{
			SnapshotEntityStorage.Clear();
			SnapshotComponentDepot.Clear();
			SnapshotRelationDepot.Clear();
			SnapshotToWorldID.Clear();
			WorldToSnapshotID.Clear();
		}
	}
}
