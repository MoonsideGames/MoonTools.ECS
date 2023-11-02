using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MoonTools.ECS.Collections;

#if DEBUG
using System.Reflection;
#endif

namespace MoonTools.ECS.Rev2;

public class World : IDisposable
{
	// Get TypeId from a Type
	internal Dictionary<Type, TypeId> TypeToId = new Dictionary<Type, TypeId>();

	#if DEBUG
	private Dictionary<TypeId, Type> IdToType = new Dictionary<TypeId, Type>();
	#endif

	// Get element size from a TypeId
	private Dictionary<TypeId, int> ElementSizes = new Dictionary<TypeId, int>();

	// Lookup from ArchetypeSignature to Archetype
	internal Dictionary<ArchetypeSignature, Archetype> ArchetypeIndex = new Dictionary<ArchetypeSignature, Archetype>();

	// Going from EntityId to Archetype and storage row
	internal Dictionary<EntityId, Record> EntityIndex = new Dictionary<EntityId, Record>();

	// TODO: can we make the tag an inline array of chars at some point?
	internal Dictionary<EntityId, string> EntityTags = new Dictionary<EntityId, string>();

	// Relation Storages
	internal Dictionary<TypeId, RelationStorage> RelationIndex =
		new Dictionary<TypeId, RelationStorage>();

	// Entity Relation Tracking
	internal Dictionary<EntityId, IndexableSet<TypeId>> EntityRelationIndex =
		new Dictionary<EntityId, IndexableSet<TypeId>>();

	// Message Storages
	private Dictionary<TypeId, MessageStorage> MessageIndex =
		new Dictionary<TypeId, MessageStorage>();

	// Filters with a single Include type for Singleton/Some implementation
	internal Dictionary<TypeId, Filter> SingleTypeFilters = new Dictionary<TypeId, Filter>();

	// Entity ID Management
	internal IdAssigner EntityIdAssigner = new IdAssigner();
	private IdAssigner TypeIdAssigner = new IdAssigner();

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

	public EntityId CreateEntity(string tag = "")
	{
		var entityId = new EntityId(EntityIdAssigner.Assign());
		EntityIndex.Add(entityId, new Record(EmptyArchetype, EmptyArchetype.Count));
		EmptyArchetype.RowToEntity.Add(entityId);

		if (!EntityRelationIndex.ContainsKey(entityId))
		{
			EntityRelationIndex.Add(entityId, new IndexableSet<TypeId>());
		}

		EntityTags[entityId] = tag;

		return entityId;
	}

	public void Tag(in EntityId entityId, string tag)
	{
		EntityTags[entityId] = tag;
	}

	public string GetTag(in EntityId entityId)
	{
		return EntityTags[entityId];
	}

	internal TypeId GetTypeId<T>() where T : unmanaged
	{
		if (TypeToId.ContainsKey(typeof(T)))
		{
			return TypeToId[typeof(T)];
		}

		var typeId = new TypeId(TypeIdAssigner.Assign());
		TypeToId.Add(typeof(T), typeId);
		ElementSizes.Add(typeId, Unsafe.SizeOf<T>());

		#if DEBUG
		IdToType.Add(typeId, typeof(T));
		#endif
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
	private TypeId GetComponentId<T>() where T : unmanaged
	{
		return TypeToId[typeof(T)];
	}

	private void RegisterRelationType(TypeId typeId)
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

	private TypeId GetMessageTypeId<T>() where T : unmanaged
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
	public bool Has<T>(EntityId entityId) where T : unmanaged
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
	public unsafe ref T Get<T>(EntityId entityId) where T : unmanaged
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

	public EntityId GetSingletonEntity<T>() where T : unmanaged
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

	public unsafe void Set<T>(in EntityId entityId, in T component) where T : unmanaged
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

	private void Add<T>(EntityId entityId, in T component) where T : unmanaged
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

	public void Remove<T>(EntityId entityId) where T : unmanaged
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

	public void Relate<T>(in EntityId entityA, in EntityId entityB, in T relation) where T : unmanaged
	{
		TryRegisterRelationType<T>();
		var relationStorage = GetRelationStorage<T>();
		relationStorage.Set(entityA, entityB, relation);
		EntityRelationIndex[entityA].Add(TypeToId[typeof(T)]);
		EntityRelationIndex[entityB].Add(TypeToId[typeof(T)]);
	}

	public void Unrelate<T>(in EntityId entityA, in EntityId entityB) where T : unmanaged
	{
		var relationStorage = GetRelationStorage<T>();
		relationStorage.Remove(entityA, entityB);
	}

	public void UnrelateAll<T>(in EntityId entity) where T : unmanaged
	{
		var relationStorage = GetRelationStorage<T>();
		relationStorage.RemoveEntity(entity);
	}

	public bool Related<T>(in EntityId entityA, in EntityId entityB) where T : unmanaged
	{
		var relationStorage = GetRelationStorage<T>();
		return relationStorage.Has(entityA, entityB);
	}

	public T GetRelationData<T>(in EntityId entityA, in EntityId entityB) where T : unmanaged
	{
		var relationStorage = GetRelationStorage<T>();
		return relationStorage.Get<T>(entityA, entityB);
	}

	public ReverseSpanEnumerator<(EntityId, EntityId)> Relations<T>() where T : unmanaged
	{
		var relationStorage = GetRelationStorage<T>();
		return relationStorage.All();
	}

	public ReverseSpanEnumerator<EntityId> OutRelations<T>(EntityId entity) where T : unmanaged
	{
		var relationStorage = GetRelationStorage<T>();
		return relationStorage.OutRelations(entity);
	}

	public EntityId OutRelationSingleton<T>(in EntityId entity) where T : unmanaged
	{
		var relationStorage = GetRelationStorage<T>();
		return relationStorage.OutFirst(entity);
	}

	public bool HasOutRelation<T>(in EntityId entity) where T : unmanaged
	{
		var relationStorage = GetRelationStorage<T>();
		return relationStorage.HasOutRelation(entity);
	}

	public ReverseSpanEnumerator<EntityId> InRelations<T>(EntityId entity) where T : unmanaged
	{
		var relationStorage = GetRelationStorage<T>();
		return relationStorage.InRelations(entity);
	}

	public EntityId InRelationSingleton<T>(in EntityId entity) where T : unmanaged
	{
		var relationStorage = GetRelationStorage<T>();
		return relationStorage.InFirst(entity);
	}

	public bool HasInRelation<T>(in EntityId entity) where T : unmanaged
	{
		var relationStorage = GetRelationStorage<T>();
		return relationStorage.HasInRelation(entity);
	}

	// used as a fast path by Archetype.ClearAll and snapshot restore
	internal void FreeEntity(EntityId entityId)
	{
		EntityIndex.Remove(entityId);
		EntityIdAssigner.Unassign(entityId.Value);

		foreach (var relationTypeIndex in EntityRelationIndex[entityId])
		{
			var relationStorage = RelationIndex[relationTypeIndex];
			relationStorage.RemoveEntity(entityId);
		}

		EntityRelationIndex[entityId].Clear();
	}

	public void Destroy(EntityId entityId)
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
		EntityIdAssigner.Unassign(entityId.Value);

		foreach (var relationTypeIndex in EntityRelationIndex[entityId])
		{
			var relationStorage = RelationIndex[relationTypeIndex];
			relationStorage.RemoveEntity(entityId);
		}

		EntityRelationIndex[entityId].Clear();
	}

	private void MoveEntityToHigherArchetype(EntityId entityId, int row, Archetype from, Archetype to)
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

	private void MoveEntityToLowerArchetype(EntityId entityId, int row, Archetype from, Archetype to, TypeId removed)
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

	// DEBUG
	// NOTE: these methods are very inefficient
	// they should only be used in debugging contexts!!
	#if DEBUG
	public ComponentEnumerator Debug_GetAllComponents(EntityId entity)
	{
		return new ComponentEnumerator(this, EntityIndex[entity]);
	}

	public Filter.EntityEnumerator Debug_GetEntities(Type componentType)
	{
		var typeId = TypeToId[componentType];
		return SingleTypeFilters[typeId].Entities;
	}

	public IEnumerable<Type> Debug_SearchComponentType(string typeString)
	{
		foreach (var type in TypeToId.Keys)
		{
			if (type.ToString().ToLower().Contains(typeString.ToLower()))
			{
				yield return type;
			}
		}
	}

	public ref struct ComponentEnumerator
	{
		private World World;
		private Record Record;
		private int ComponentIndex;

		public ComponentEnumerator GetEnumerator() => this;

		internal ComponentEnumerator(
			World world,
			Record record
		)
		{
			World = world;
			Record = record;
			ComponentIndex = -1;
		}

		public bool MoveNext()
		{
			ComponentIndex += 1;
			return ComponentIndex < Record.Archetype.ComponentColumns.Length;
		}

		public unsafe object Current
		{
			get
			{
				var elt = Record.Archetype.ComponentColumns[ComponentIndex].Get(Record.Row);
				return Pointer.Box(elt, World.IdToType[Record.Archetype.Signature[ComponentIndex]]);
			}
		}
	}
	#endif

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
