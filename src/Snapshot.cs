namespace MoonTools.ECS
{
	public class Snapshot
	{
		private World World;
		private Filter? Filter;

		private EntityStorage SnapshotEntityStorage;
		private ComponentDepot SnapshotComponentDepot;

		internal Snapshot(World world)
		{
			World = world;
			SnapshotEntityStorage = new EntityStorage();
			SnapshotComponentDepot = new ComponentDepot(World.ComponentTypeIndices);
		}

		public void Take(Filter filter)
		{
			Clear();
			Filter = filter;
			SnapshotComponentDepot.FillMissingStorages(World.ComponentDepot);

			foreach (var worldEntity in filter.Entities)
			{
				var snapshotEntity = SnapshotEntityStorage.Create();
				foreach (var componentTypeIndex in World.EntityStorage.ComponentTypeIndices(worldEntity.ID))
				{
					SnapshotEntityStorage.SetComponent(snapshotEntity.ID, componentTypeIndex);
					SnapshotComponentDepot.Set(snapshotEntity.ID, componentTypeIndex, World.ComponentDepot.UntypedGet(worldEntity.ID, componentTypeIndex));
				}
			}
		}

		public void Restore()
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

				foreach (var componentTypeIndex in SnapshotEntityStorage.ComponentTypeIndices(i))
				{
					World.EntityStorage.SetComponent(entity.ID, componentTypeIndex);
					World.FilterStorage.Check(entity.ID, componentTypeIndex);
					World.ComponentDepot.Set(entity.ID, componentTypeIndex, SnapshotComponentDepot.UntypedGet(i, componentTypeIndex));
				}
			}
		}

		private void Clear()
		{
			SnapshotEntityStorage.Clear();
			SnapshotComponentDepot.Clear();
		}
	}
}
