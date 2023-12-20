using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MoonTools.ECS.Collections;

namespace MoonTools.ECS;

// TODO: we should implement a NativeDictionary that can be memcopied
public class Snapshot : IDisposable
{
	private List<ComponentSnapshot> ComponentSnapshots = new List<ComponentSnapshot>();

	// FIXME: we could just have a filter ID
	private Dictionary<FilterSignature, List<Entity>> Filters = new Dictionary<FilterSignature, List<Entity>>();

	private List<RelationSnapshot> RelationSnapshots = new List<RelationSnapshot>();

	private List<IndexableSet<TypeId>> EntityRelationIndex =
		new List<IndexableSet<TypeId>>();

	private List<IndexableSet<TypeId>> EntityComponentIndex =
		new List<IndexableSet<TypeId>>();

	private List<string> EntityTags = new List<string>();

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
		foreach (var componentStorage in world.ComponentIndex)
		{
			componentStorage.Clear();
		}

		// clear all relation storages in case any were created after snapshot
		// FIXME: this can be eliminated via component discovery
		foreach (var relationStorage in world.RelationIndex)
		{
			relationStorage.Clear();
		}

		for (var i = 0; i < ComponentSnapshots.Count; i += 1)
		{
			var componentStorage = world.ComponentIndex[i];
			ComponentSnapshots[i].Restore(componentStorage);
		}

		// restore relation state
		for (var i = 0; i < RelationSnapshots.Count; i += 1)
		{
			var relationStorage = world.RelationIndex[i];
			RelationSnapshots[i].Restore(relationStorage);
		}

		// restore entity relation index state
		// FIXME: arghhhh this is so slow

		foreach (var relationTypeSet in world.EntityRelationIndex)
		{
			relationTypeSet.Clear();
		}

		for (var i = 0; i < EntityRelationIndex.Count; i += 1)
		{
			var relationTypeSet = EntityRelationIndex[i];

			foreach (var typeId in relationTypeSet)
			{
				world.EntityRelationIndex[i].Add(typeId);
			}
		}

		// restore entity component index state
		// FIXME: arrghghhh this is so slow

		foreach (var componentTypeSet in world.EntityComponentIndex)
		{
			componentTypeSet.Clear();
		}

		for (var i = 0; i < EntityComponentIndex.Count; i += 1)
		{
			var componentTypeSet = EntityComponentIndex[i];

			foreach (var typeId in componentTypeSet)
			{
				world.EntityComponentIndex[i].Add(typeId);
			}
		}

		// restore entity tags
		for (var i = 0; i < EntityTags.Count; i += 1)
		{
			world.EntityTags[i] = EntityTags[i];
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
		for (var i = ComponentSnapshots.Count; i < world.ComponentIndex.Count; i += 1)
		{
			ComponentSnapshots.Add(new ComponentSnapshot(world.ComponentIndex[i].ElementSize));
		}

		for (var i = 0; i < world.ComponentIndex.Count; i += 1)
		{
			ComponentSnapshots[i].Take(world.ComponentIndex[i]);
		}

		// copy relations
		for (var i = RelationSnapshots.Count; i < world.RelationIndex.Count; i += 1)
		{
			RelationSnapshots.Add(new RelationSnapshot(world.RelationIndex[i].ElementSize));
		}

		for (var i = 0; i < world.RelationIndex.Count; i += 1)
		{
			RelationSnapshots[i].Take(world.RelationIndex[i]);
		}

		// fill in missing index structures

		for (var i = EntityComponentIndex.Count; i < world.EntityComponentIndex.Count; i += 1)
		{
			EntityComponentIndex.Add(new IndexableSet<TypeId>());
		}

		for (var i = EntityRelationIndex.Count; i < world.EntityRelationIndex.Count; i += 1)
		{
			EntityRelationIndex.Add(new IndexableSet<TypeId>());
		}

		// copy entity relation index
		// FIXME: arghhhh this is so slow
		for (var i = 0; i < world.EntityRelationIndex.Count; i += 1)
		{
			EntityRelationIndex[i].Clear();

			foreach (var typeId in world.EntityRelationIndex[i])
			{
				EntityRelationIndex[i].Add(typeId);
			}
		}

		// copy entity component index
		// FIXME: arghhhh this is so slow
		for (var i = 0; i < world.EntityComponentIndex.Count; i += 1)
		{
			EntityComponentIndex[i].Clear();

			foreach (var typeId in world.EntityComponentIndex[i])
			{
				EntityComponentIndex[i].Add(typeId);
			}
		}

		// copy entity tags
		EntityTags.Clear();
		foreach (var s in world.EntityTags)
		{
			EntityTags.Add(s);
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
				foreach (var componentSnapshot in ComponentSnapshots)
				{
					componentSnapshot.Dispose();
				}

				foreach (var relationSnapshot in RelationSnapshots)
				{
					relationSnapshot.Dispose();
				}

				foreach (var componentSet in EntityComponentIndex)
				{
					componentSet.Dispose();
				}

				foreach (var relationSet in EntityRelationIndex)
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
