using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MoonTools.ECS.Collections;

namespace MoonTools.ECS;

// TODO: we should implement a NativeDictionary that can be memcopied
public class Snapshot : IDisposable
{
	private Dictionary<TypeId, ComponentSnapshot> ComponentSnapshots = new Dictionary<TypeId, ComponentSnapshot>();

	private Dictionary<FilterSignature, List<Entity>> Filters = new Dictionary<FilterSignature, List<Entity>>();

	private Dictionary<TypeId, RelationSnapshot> RelationSnapshots =
		new Dictionary<TypeId, RelationSnapshot>();

	private Dictionary<Entity, IndexableSet<TypeId>> EntityRelationIndex =
		new Dictionary<Entity, IndexableSet<TypeId>>();

	private Dictionary<Entity, IndexableSet<TypeId>> EntityComponentIndex =
		new Dictionary<Entity, IndexableSet<TypeId>>();

	private Dictionary<Entity, string> EntityTags = new Dictionary<Entity, string>();

	private IdAssigner EntityIdAssigner = new IdAssigner();

	private bool IsDisposed;

	public void Restore(World world)
	{
		// restore id assigner state
		EntityIdAssigner.CopyTo(world.EntityIdAssigner);

		// restore filter states
		// this could be sped up if we figured out a direct IndexableSet copy
		foreach (var (signature, entityList) in Filters)
		{
			var filter = world.FilterIndex[signature];

			filter.Clear();

			foreach (var entity in entityList)
			{
				filter.AddEntity(entity);
			}
		}

		// clear all component storages in case any were created after snapshot
		// FIXME: this can be eliminated via component discovery
		foreach (var (typeId, componentStorage) in world.ComponentIndex)
		{
			componentStorage.Clear();
		}

		// clear all relation storages in case any were created after snapshot
		// FIXME: this can be eliminated via component discovery
		foreach (var (typeId, relationStorage) in world.RelationIndex)
		{
			relationStorage.Clear();
		}

		// restore components
		foreach (var (typeId, componentSnapshot) in ComponentSnapshots)
		{
			var componentStorage = world.ComponentIndex[typeId];
			componentSnapshot.Restore(componentStorage);
		}

		// restore relation state
		foreach (var (typeId, relationSnapshot) in RelationSnapshots)
		{
			var relationStorage = world.RelationIndex[typeId];
			relationSnapshot.Restore(relationStorage);
		}

		// restore entity relation index state
		// FIXME: arghhhh this is so slow

		foreach (var (id, relationTypeSet) in world.EntityRelationIndex)
		{
			relationTypeSet.Clear();
		}

		foreach (var (id, relationTypeSet) in EntityRelationIndex)
		{
			foreach (var typeId in relationTypeSet)
			{
				world.EntityRelationIndex[id].Add(typeId);
			}
		}

		// restore entity component index state
		// FIXME: arrghghhh this is so slow

		foreach (var (id, componentTypeSet) in world.EntityComponentIndex)
		{
			componentTypeSet.Clear();
		}

		foreach (var (id, componentTypeSet) in EntityComponentIndex)
		{
			foreach (var typeId in componentTypeSet)
			{
				world.EntityComponentIndex[id].Add(typeId);
			}
		}

		// restore entity tags
		foreach (var (id, s) in EntityTags)
		{
			world.EntityTags[id] = s;
		}
	}

	public void Take(World world)
	{
		// copy id assigner state
		world.EntityIdAssigner.CopyTo(EntityIdAssigner);

		// copy filter states
		foreach (var (_, filter) in world.FilterIndex)
		{
			TakeFilterSnapshot(filter);
		}

		// copy components
		foreach (var (typeId, componentStorage) in world.ComponentIndex)
		{
			TakeComponentSnapshot(typeId, componentStorage);
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

		// copy entity component index
		// FIXME: arghhhh this is so slow
		foreach (var (id, componentTypeSet) in world.EntityComponentIndex)
		{
			if (!EntityComponentIndex.ContainsKey(id))
			{
				EntityComponentIndex.Add(id, new IndexableSet<TypeId>());
			}

			EntityComponentIndex[id].Clear();

			foreach (var typeId in componentTypeSet)
			{
				EntityComponentIndex[id].Add(typeId);
			}
		}

		// copy entity tags
		foreach (var (id, s) in world.EntityTags)
		{
			EntityTags[id] = s;
		}
	}

	private void TakeFilterSnapshot(Filter filter)
	{
		if (!Filters.TryGetValue(filter.Signature, out var entities))
		{
			entities = new List<Entity>();
			Filters.Add(filter.Signature, entities);
		}

		entities.Clear();

		foreach (var entity in filter.EntitySet.AsSpan())
		{
			entities.Add(entity);
		}
	}

	private void TakeComponentSnapshot(TypeId typeId, ComponentStorage componentStorage)
	{
		if (!ComponentSnapshots.TryGetValue(typeId, out var componentSnapshot))
		{
			componentSnapshot = new ComponentSnapshot(componentStorage.Components.ElementSize);
			ComponentSnapshots.Add(typeId, componentSnapshot);
		}

		componentSnapshot.Take(componentStorage);
	}

	private void TakeRelationSnapshot(TypeId typeId, RelationStorage relationStorage)
	{
		if (!RelationSnapshots.TryGetValue(typeId, out var snapshot))
		{
			snapshot = new RelationSnapshot(relationStorage.RelationDatas.ElementSize);
			RelationSnapshots.Add(typeId, snapshot);
		}

		snapshot.Take(relationStorage);
	}

	private class ComponentSnapshot : IDisposable
	{
		private readonly NativeArray Components;
		private readonly NativeArray<Entity> EntityIDs;

		private bool IsDisposed;

		public ComponentSnapshot(int elementSize)
		{
			Components = new NativeArray(elementSize);
			EntityIDs = new NativeArray<Entity>();
		}

		public void Take(ComponentStorage componentStorage)
		{
			componentStorage.Components.CopyAllTo(Components);
			componentStorage.EntityIDs.CopyTo(EntityIDs);
		}

		public void Restore(ComponentStorage componentStorage)
		{
			Components.CopyAllTo(componentStorage.Components);
			EntityIDs.CopyTo(componentStorage.EntityIDs);

			componentStorage.EntityIDToStorageIndex.Clear();
			for (int i = 0; i < EntityIDs.Count; i += 1)
			{
				var entityID = EntityIDs[i];
				componentStorage.EntityIDToStorageIndex[entityID] = i;
			}
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!IsDisposed)
			{
				if (disposing)
				{
					Components.Dispose();
					EntityIDs.Dispose();
				}

				IsDisposed = true;
			}
		}

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}

	private class RelationSnapshot : IDisposable
	{
		private NativeArray Relations;
		private NativeArray RelationDatas;

		private bool IsDisposed;

		public RelationSnapshot(int elementSize)
		{
			Relations = new NativeArray(Unsafe.SizeOf<(Entity, Entity)>());
			RelationDatas = new NativeArray(elementSize);
		}

		public void Take(RelationStorage relationStorage)
		{
			relationStorage.Relations.CopyAllTo(Relations);
			relationStorage.RelationDatas.CopyAllTo(RelationDatas);
		}

		public void Restore(RelationStorage relationStorage)
		{
			relationStorage.Clear();

			Relations.CopyAllTo(relationStorage.Relations);
			RelationDatas.CopyAllTo(relationStorage.RelationDatas);

			for (int index = 0; index < Relations.Count; index += 1)
			{
				var relation = Relations.Get<(Entity, Entity)>(index);
				relationStorage.Indices[relation] = index;

				relationStorage.Indices[relation] = index;

				if (!relationStorage.OutRelationSets.ContainsKey(relation.Item1))
				{
					relationStorage.OutRelationSets[relation.Item1] =
						relationStorage.AcquireHashSetFromPool();
				}

				relationStorage.OutRelationSets[relation.Item1].Add(relation.Item2);

				if (!relationStorage.InRelationSets.ContainsKey(relation.Item2))
				{
					relationStorage.InRelationSets[relation.Item2] =
						relationStorage.AcquireHashSetFromPool();
				}

				relationStorage.InRelationSets[relation.Item2].Add(relation.Item1);
			}
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!IsDisposed)
			{
				if (disposing)
				{
					Relations.Dispose();
					RelationDatas.Dispose();
				}

				IsDisposed = true;
			}
		}

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!IsDisposed)
		{
			if (disposing)
			{
				foreach (var componentSnapshot in ComponentSnapshots.Values)
				{
					componentSnapshot.Dispose();
				}

				foreach (var relationSnapshot in RelationSnapshots.Values)
				{
					relationSnapshot.Dispose();
				}

				foreach (var componentSet in EntityComponentIndex.Values)
				{
					componentSet.Dispose();
				}

				foreach (var relationSet in EntityRelationIndex.Values)
				{
					relationSet.Dispose();
				}

				EntityIdAssigner.Dispose();
			}

			IsDisposed = true;
		}
	}

	public void Dispose()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}
