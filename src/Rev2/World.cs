using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MoonTools.ECS.Collections;

namespace MoonTools.ECS.Rev2;

public class World : IDisposable
{
	// Get ComponentId from a Type
	internal static Dictionary<Type, Id> TypeToId = new Dictionary<Type, Id>();
	// Get element size from a ComponentId
	internal static Dictionary<Id, int> ElementSizes = new Dictionary<Id, int>();

	// Lookup from ArchetypeSignature to Archetype
	internal Dictionary<ArchetypeSignature, Archetype> ArchetypeIndex = new Dictionary<ArchetypeSignature, Archetype>();

	// Going from EntityId to Archetype and storage row
	internal Dictionary<Id, Record> EntityIndex = new Dictionary<Id, Record>();

	// Relation Storages
	internal Dictionary<Id, RelationStorage> RelationIndex =
		new Dictionary<Id, RelationStorage>();

	// Entity Relation Tracking
	internal Dictionary<Id, IndexableSet<Id>> EntityRelationIndex =
		new Dictionary<Id, IndexableSet<Id>>();

	// Message Storages
	private Dictionary<Id, MessageStorage> MessageIndex =
		new Dictionary<Id, MessageStorage>();

	// Filters with a single Include type for Singleton/Some implementation
	private Dictionary<Id, Filter> SingleTypeFilters = new Dictionary<Id, Filter>();

	// ID Management
	// FIXME: Entity and Type Ids should be separated
	internal IdAssigner IdAssigner = new IdAssigner();

	internal readonly Archetype EmptyArchetype;

	public FilterBuilder FilterBuilder => new FilterBuilder(this);

	public delegate void RefAction<T1, T2>(ref T1 arg1, ref T2 arg2);

	private bool IsDisposed;

	public World()
	{
		// Create the Empty Archetype
		EmptyArchetype = CreateArchetype(ArchetypeSignature.Empty);
	}

	internal Archetype CreateArchetype(ArchetypeSignature signature)
	{
		var archetype = new Archetype(this, signature);

		ArchetypeIndex.Add(signature, archetype);

		for (int i = 0; i < signature.Count; i += 1)
		{
			var componentId = signature[i];
			archetype.ComponentToColumnIndex.Add(componentId, i);
			archetype.ComponentColumns[i] = new NativeArray(ElementSizes[componentId]);
		}

		return archetype;
	}

	public Id CreateEntity(string tag = "")
	{
		var entityId = IdAssigner.Assign();
		EntityIndex.Add(entityId, new Record(EmptyArchetype, EmptyArchetype.Count));
		EmptyArchetype.RowToEntity.Add(entityId);

		if (!EntityRelationIndex.ContainsKey(entityId))
		{
			EntityRelationIndex.Add(entityId, new IndexableSet<Id>());
		}

		return entityId;
	}

	internal Id GetTypeId<T>() where T : unmanaged
	{
		if (TypeToId.ContainsKey(typeof(T)))
		{
			return TypeToId[typeof(T)];
		}

		var typeId = IdAssigner.Assign();
		TypeToId.Add(typeof(T), typeId);
		ElementSizes.Add(typeId, Unsafe.SizeOf<T>());
		return typeId;
	}

	private void TryRegisterComponentId<T>() where T : unmanaged
	{
		var typeId = GetTypeId<T>();
		if (!SingleTypeFilters.ContainsKey(typeId))
		{
			SingleTypeFilters.Add(typeId, FilterBuilder.Include<T>().Build());
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private Id GetComponentId<T>() where T : unmanaged
	{
		return TypeToId[typeof(T)];
	}

	private void RegisterRelationType(Id typeId)
	{
		RelationIndex.Add(typeId, new RelationStorage(ElementSizes[typeId]));
	}

	private void TryRegisterRelationType<T>() where T : unmanaged
	{
		var typeId = GetTypeId<T>();
		if (!RelationIndex.ContainsKey(typeId))
		{
			RegisterRelationType(typeId);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private RelationStorage GetRelationStorage<T>() where T : unmanaged
	{
		return RelationIndex[TypeToId[typeof(T)]];
	}

	// Messages

	private Id GetMessageTypeId<T>() where T : unmanaged
	{
		var typeId = GetTypeId<T>();

		if (!MessageIndex.ContainsKey(typeId))
		{
			MessageIndex.Add(typeId, new MessageStorage(Unsafe.SizeOf<T>()));
		}

		return typeId;
	}

	public void Send<T>(in T message) where T : unmanaged
	{
		var typeId = GetMessageTypeId<T>();
		MessageIndex[typeId].Add(message);
	}

	public bool SomeMessage<T>() where T : unmanaged
	{
		var typeId = GetMessageTypeId<T>();
		return MessageIndex[typeId].Some();
	}

	public ReadOnlySpan<T> ReadMessages<T>() where T : unmanaged
	{
		var typeId = GetMessageTypeId<T>();
		return MessageIndex[typeId].All<T>();
	}

	public T ReadMessage<T>() where T : unmanaged
	{
		var typeId = GetMessageTypeId<T>();
		return MessageIndex[typeId].First<T>();
	}

	public void ClearMessages()
	{
		foreach (var (_, messageStorage) in MessageIndex)
		{
			messageStorage.Clear();
		}
	}

	// Components
	public bool Has<T>(Id entityId) where T : unmanaged
	{
		var componentId = GetComponentId<T>();
		var record = EntityIndex[entityId];
		return record.Archetype.ComponentToColumnIndex.ContainsKey(componentId);
	}

	public bool Some<T>() where T : unmanaged
	{
		var componentTypeId = GetComponentId<T>();
		return SingleTypeFilters[componentTypeId].Count > 0;
	}

	// will throw if non-existent
	public unsafe ref T Get<T>(Id entityId) where T : unmanaged
	{
		var componentId = GetComponentId<T>();

		var record = EntityIndex[entityId];
		var columnIndex = record.Archetype.ComponentToColumnIndex[componentId];
		var column = record.Archetype.ComponentColumns[columnIndex];

		return ref ((T*) column.Elements)[record.Row];
	}

	public ref T GetSingleton<T>() where T : unmanaged
	{
		var componentId = GetComponentId<T>();

		foreach (var archetype in SingleTypeFilters[componentId].Archetypes)
		{
			if (archetype.Count > 0)
			{
				var columnIndex = archetype.ComponentToColumnIndex[componentId];
				return ref archetype.ComponentColumns[columnIndex].Get<T>(0);
			}
		}

		throw new InvalidOperationException("No component of this type exists!");
	}

	public Id GetSingletonEntity<T>() where T : unmanaged
	{
		var componentId = GetComponentId<T>();

		foreach (var archetype in SingleTypeFilters[componentId].Archetypes)
		{
			if (archetype.Count > 0)
			{
				return archetype.RowToEntity[0];
			}
		}

		throw new InvalidOperationException("No entity with this component type exists!");
	}

	public unsafe void Set<T>(in Id entityId, in T component) where T : unmanaged
	{
		TryRegisterComponentId<T>();
		var componentId = GetComponentId<T>();

		if (Has<T>(entityId))
		{
			var record = EntityIndex[entityId];
			var columnIndex = record.Archetype.ComponentToColumnIndex[componentId];
			var column = record.Archetype.ComponentColumns[columnIndex];

			((T*) column.Elements)[record.Row] = component;
		}
		else
		{
			Add(entityId, component);
		}
	}

	private void Add<T>(Id entityId, in T component) where T : unmanaged
	{
		Archetype? nextArchetype;

		var componentId = GetComponentId<T>();

		// move the entity to the new archetype
		var record = EntityIndex[entityId];
		var archetype = record.Archetype;

		if (archetype.Edges.TryGetValue(componentId, out var edge))
		{
			nextArchetype = edge.Add;
		}
		else
		{
			// FIXME: pool the signatures
			var nextSignature = new ArchetypeSignature(archetype.Signature.Count + 1);
			archetype.Signature.CopyTo(nextSignature);
			nextSignature.Insert(componentId);

			if (!ArchetypeIndex.TryGetValue(nextSignature, out nextArchetype))
			{
				nextArchetype = CreateArchetype(nextSignature);
			}

			var newEdge = new ArchetypeEdge(nextArchetype, archetype);
			archetype.Edges.Add(componentId, newEdge);
			nextArchetype.Edges.Add(componentId, newEdge);
		}

		MoveEntityToHigherArchetype(entityId, record.Row, archetype, nextArchetype);

		// add the new component to the new archetype
		var columnIndex = nextArchetype.ComponentToColumnIndex[componentId];
		var column = nextArchetype.ComponentColumns[columnIndex];
		column.Append(component);
	}

	public void Remove<T>(Id entityId) where T : unmanaged
	{
		Archetype? nextArchetype;

		var componentId = GetComponentId<T>();

		var (archetype, row) = EntityIndex[entityId];

		if (archetype.Edges.TryGetValue(componentId, out var edge))
		{
			nextArchetype = edge.Remove;
		}
		else
		{
			// FIXME: pool the signatures
			var nextSignature = new ArchetypeSignature(archetype.Signature.Count + 1);
			archetype.Signature.CopyTo(nextSignature);
			nextSignature.Remove(componentId);

			if (!ArchetypeIndex.TryGetValue(nextSignature, out nextArchetype))
			{
				nextArchetype = CreateArchetype(nextSignature);
			}

			var newEdge = new ArchetypeEdge(nextArchetype, archetype);
			archetype.Edges.Add(componentId, newEdge);
			nextArchetype.Edges.Add(componentId, newEdge);
		}

		MoveEntityToLowerArchetype(entityId, row, archetype, nextArchetype, componentId);
	}

	public void Relate<T>(in Id entityA, in Id entityB, in T relation) where T : unmanaged
	{
		TryRegisterRelationType<T>();
		var relationStorage = GetRelationStorage<T>();
		relationStorage.Set(entityA, entityB, relation);
		EntityRelationIndex[entityA].Add(TypeToId[typeof(T)]);
		EntityRelationIndex[entityB].Add(TypeToId[typeof(T)]);
	}

	public void Unrelate<T>(in Id entityA, in Id entityB) where T : unmanaged
	{
		var relationStorage = GetRelationStorage<T>();
		relationStorage.Remove(entityA, entityB);
	}

	public void UnrelateAll<T>(in Id entity) where T : unmanaged
	{
		var relationStorage = GetRelationStorage<T>();
		relationStorage.RemoveEntity(entity);
	}

	public bool Related<T>(in Id entityA, in Id entityB) where T : unmanaged
	{
		var relationStorage = GetRelationStorage<T>();
		return relationStorage.Has(entityA, entityB);
	}

	public T GetRelationData<T>(in Id entityA, in Id entityB) where T : unmanaged
	{
		var relationStorage = GetRelationStorage<T>();
		return relationStorage.Get<T>(entityA, entityB);
	}

	public ReverseSpanEnumerator<(Id, Id)> Relations<T>() where T : unmanaged
	{
		var relationStorage = GetRelationStorage<T>();
		return relationStorage.All();
	}

	public ReverseSpanEnumerator<Id> OutRelations<T>(Id entity) where T : unmanaged
	{
		var relationStorage = GetRelationStorage<T>();
		return relationStorage.OutRelations(entity);
	}

	public Id OutRelationSingleton<T>(in Id entity) where T : unmanaged
	{
		var relationStorage = GetRelationStorage<T>();
		return relationStorage.OutFirst(entity);
	}

	public bool HasOutRelation<T>(in Id entity) where T : unmanaged
	{
		var relationStorage = GetRelationStorage<T>();
		return relationStorage.HasOutRelation(entity);
	}

	public ReverseSpanEnumerator<Id> InRelations<T>(Id entity) where T : unmanaged
	{
		var relationStorage = GetRelationStorage<T>();
		return relationStorage.InRelations(entity);
	}

	public Id InRelationSingleton<T>(in Id entity) where T : unmanaged
	{
		var relationStorage = GetRelationStorage<T>();
		return relationStorage.InFirst(entity);
	}

	public bool HasInRelation<T>(in Id entity) where T : unmanaged
	{
		var relationStorage = GetRelationStorage<T>();
		return relationStorage.HasInRelation(entity);
	}

	private bool Has(Id entityId, Id typeId)
	{
		var record = EntityIndex[entityId];
		return record.Archetype.ComponentToColumnIndex.ContainsKey(typeId);
	}

	// used as a fast path by Archetype.ClearAll and snapshot restore
	internal void FreeEntity(Id entityId)
	{
		EntityIndex.Remove(entityId);
		IdAssigner.Unassign(entityId);

		foreach (var relationTypeIndex in EntityRelationIndex[entityId])
		{
			var relationStorage = RelationIndex[relationTypeIndex];
			relationStorage.RemoveEntity(entityId);
		}

		EntityRelationIndex[entityId].Clear();
	}

	public void Destroy(Id entityId)
	{
		var record = EntityIndex[entityId];
		var archetype = record.Archetype;
		var row = record.Row;

		for (int i = 0; i < archetype.Signature.Count; i += 1)
		{
			archetype.ComponentColumns[i].Delete(row);
		}

		if (row != archetype.Count - 1)
		{
			// move last row entity to open spot
			var lastRowEntity = archetype.RowToEntity[archetype.Count - 1];
			archetype.RowToEntity[row] = lastRowEntity;
			EntityIndex[lastRowEntity] = new Record(archetype, row);
		}

		archetype.RowToEntity.RemoveLastElement();
		EntityIndex.Remove(entityId);
		IdAssigner.Unassign(entityId);

		foreach (var relationTypeIndex in EntityRelationIndex[entityId])
		{
			var relationStorage = RelationIndex[relationTypeIndex];
			relationStorage.RemoveEntity(entityId);
		}

		EntityRelationIndex[entityId].Clear();
	}

	private void MoveEntityToHigherArchetype(Id entityId, int row, Archetype from, Archetype to)
	{
		for (int i = 0; i < from.Signature.Count; i += 1)
		{
			var componentId = from.Signature[i];
			var destinationColumnIndex = to.ComponentToColumnIndex[componentId];

			// copy all components to higher archetype
			from.ComponentColumns[i].CopyElementToEnd(row, to.ComponentColumns[destinationColumnIndex]);

			// delete row on from archetype
			from.ComponentColumns[i].Delete(row);
		}

		if (row != from.Count - 1)
		{
			// move last row entity to open spot
			var lastRowEntity = from.RowToEntity[from.Count - 1];
			from.RowToEntity[row] = lastRowEntity;
			EntityIndex[lastRowEntity] = new Record(from, row);
		}

		from.RowToEntity.RemoveLastElement();

		// update row to entity lookup on to archetype
		EntityIndex[entityId] = new Record(to, to.Count);
		to.RowToEntity.Add(entityId);
	}

	private void MoveEntityToLowerArchetype(Id entityId, int row, Archetype from, Archetype to, Id removed)
	{
		for (int i = 0; i < from.Signature.Count; i += 1)
		{
			var componentId = from.Signature[i];

			// delete the row
			from.ComponentColumns[i].Delete(row);

			// if this isn't the removed component, copy to the lower archetype
			if (componentId != removed)
			{
				var destinationColumnIndex = to.ComponentToColumnIndex[componentId];
				from.ComponentColumns[i].CopyElementToEnd(row, to.ComponentColumns[destinationColumnIndex]);
			}
		}

		if (row != from.Count - 1)
		{
			// update row to entity lookup on from archetype
			var lastRowEntity = from.RowToEntity[from.Count - 1];
			from.RowToEntity[row] = lastRowEntity;
			EntityIndex[lastRowEntity] = new Record(from, row);
		}

		from.RowToEntity.RemoveLastElement();

		// update row to entity lookup on to archetype
		EntityIndex[entityId] = new Record(to, to.Count);
		to.RowToEntity.Add(entityId);
	}

	public unsafe void ForEachEntity<T, T1, T2>(Filter filter,
		T rowForEachContainer) where T : IForEach<T1, T2> where T1 : unmanaged where T2 : unmanaged
	{
		foreach (var archetype in filter.Archetypes)
		{
			var componentIdOne = archetype.Signature[0];
			var columnIndexOne = archetype.ComponentToColumnIndex[componentIdOne];
			var columnOneElements = archetype.ComponentColumns[columnIndexOne].Elements;

			var componentIdTwo = archetype.Signature[1];
			var columnIndexTwo = archetype.ComponentToColumnIndex[componentIdTwo];
			var columnTwoElements = archetype.ComponentColumns[columnIndexTwo].Elements;

			for (int i = archetype.Count - 1; i >= 0; i -= 1)
			{
				rowForEachContainer.Update(ref ((T1*) columnOneElements)[i], ref ((T2*) columnTwoElements)[i]);
			}
		}
	}

	public unsafe void ForEachEntity<T1, T2>(Filter filter, RefAction<T1, T2> rowAction) where T1 : unmanaged where T2 : unmanaged
	{
		foreach (var archetype in filter.Archetypes)
		{
			var componentIdOne = archetype.Signature[0];
			var columnIndexOne = archetype.ComponentToColumnIndex[componentIdOne];
			var columnOneElements = archetype.ComponentColumns[columnIndexOne].Elements;

			var componentIdTwo = archetype.Signature[1];
			var columnIndexTwo = archetype.ComponentToColumnIndex[componentIdTwo];
			var columnTwoElements = archetype.ComponentColumns[columnIndexTwo].Elements;

			for (int i = archetype.Count - 1; i >= 0; i -= 1)
			{
				rowAction(ref ((T1*) columnOneElements)[i], ref ((T2*) columnTwoElements)[i]);
			}
		}
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!IsDisposed)
		{
			if (disposing)
			{
				// dispose managed state (managed objects)
				foreach (var archetype in ArchetypeIndex.Values)
				{
					for (var i = 0; i < archetype.Signature.Count; i += 1)
					{
						archetype.ComponentColumns[i].Dispose();
					}
				}
			}

			// TODO: free unmanaged resources (unmanaged objects) and override finalizer
			// TODO: set large fields to null
			IsDisposed = true;
		}
	}

	// TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
	// ~World()
	// {
	//     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
	//     Dispose(disposing: false);
	// }

	public void Dispose()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}
