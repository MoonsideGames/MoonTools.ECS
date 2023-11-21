using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MoonTools.ECS.Collections;

namespace MoonTools.ECS;

public class World : IDisposable
{
	// Get TypeId from a Type
	private readonly Dictionary<Type, TypeId> TypeToId = new Dictionary<Type, TypeId>();

#if DEBUG
	private Dictionary<TypeId, Type> IdToType = new Dictionary<TypeId, Type>();
#endif

	// Get element size from a TypeId
	private readonly Dictionary<TypeId, int> ElementSizes = new Dictionary<TypeId, int>();

	// Filters
	internal readonly Dictionary<FilterSignature, Filter> FilterIndex = new Dictionary<FilterSignature, Filter>();
	private readonly Dictionary<TypeId, List<Filter>> TypeToFilter = new Dictionary<TypeId, List<Filter>>();

	// TODO: can we make the tag an native array of chars at some point?
	internal Dictionary<Entity, string> EntityTags = new Dictionary<Entity, string>();

	// Relation Storages
	internal Dictionary<TypeId, RelationStorage> RelationIndex = new Dictionary<TypeId, RelationStorage>();
	internal Dictionary<Entity, IndexableSet<TypeId>> EntityRelationIndex = new Dictionary<Entity, IndexableSet<TypeId>>();

	// Message Storages
	private Dictionary<TypeId, MessageStorage> MessageIndex = new Dictionary<TypeId, MessageStorage>();

	public FilterBuilder FilterBuilder => new FilterBuilder(this);

	internal readonly Dictionary<TypeId, ComponentStorage> ComponentIndex = new Dictionary<TypeId, ComponentStorage>();
	internal Dictionary<Entity, IndexableSet<TypeId>> EntityComponentIndex = new Dictionary<Entity, IndexableSet<TypeId>>();

	internal IdAssigner EntityIdAssigner = new IdAssigner();
	private IdAssigner TypeIdAssigner = new IdAssigner();

	private bool IsDisposed;

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

	internal TypeId GetComponentTypeId<T>() where T : unmanaged
	{
		var typeId = GetTypeId<T>();
		if (ComponentIndex.TryGetValue(typeId, out var componentStorage))
		{
			return typeId;
		}

		componentStorage = new ComponentStorage(typeId, ElementSizes[typeId]);
		ComponentIndex.Add(typeId, componentStorage);
		TypeToFilter.Add(typeId, new List<Filter>());
		return typeId;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private ComponentStorage GetComponentStorage<T>() where T : unmanaged
	{
		var typeId = GetTypeId<T>();
		if (ComponentIndex.TryGetValue(typeId, out var componentStorage))
		{
			return componentStorage;
		}

		componentStorage = new ComponentStorage(typeId, ElementSizes[typeId]);
		ComponentIndex.Add(typeId, componentStorage);
		TypeToFilter.Add(typeId, new List<Filter>());
		return componentStorage;
	}

	// FILTERS

	internal Filter GetFilter(FilterSignature signature)
	{
		if (!FilterIndex.TryGetValue(signature, out var filter))
		{
			filter = new Filter(this, signature);

			foreach (var typeId in signature.Included)
			{
				TypeToFilter[typeId].Add(filter);
			}

			foreach (var typeId in signature.Excluded)
			{
				TypeToFilter[typeId].Add(filter);
			}

			FilterIndex.Add(signature, filter);
		}

		return filter;
	}

	// ENTITIES

	public Entity CreateEntity(string tag = "")
	{
		var entity = new Entity(EntityIdAssigner.Assign());

		if (!EntityComponentIndex.ContainsKey(entity))
		{
			EntityRelationIndex.Add(entity, new IndexableSet<TypeId>());
			EntityComponentIndex.Add(entity, new IndexableSet<TypeId>());
		}

		EntityTags[entity] = tag;

		return entity;
	}

	public void Tag(Entity entity, string tag)
	{
		EntityTags[entity] = tag;
	}

	public string GetTag(Entity entity)
	{
		return EntityTags[entity];
	}

	public void Destroy(in Entity entity)
	{
		// remove all components from storages
		foreach (var componentTypeIndex in EntityComponentIndex[entity])
		{
			var componentStorage = ComponentIndex[componentTypeIndex];
			componentStorage.Remove(entity);

			foreach (var filter in TypeToFilter[componentTypeIndex])
			{
				filter.RemoveEntity(entity);
			}
		}

		// remove all relations from storage
		foreach (var relationTypeIndex in EntityRelationIndex[entity])
		{
			var relationStorage = RelationIndex[relationTypeIndex];
			relationStorage.RemoveEntity(entity);
		}

		EntityComponentIndex[entity].Clear();
		EntityRelationIndex[entity].Clear();

		// recycle ID
		EntityIdAssigner.Unassign(entity.ID);
	}

	// COMPONENTS

	public bool Has<T>(in Entity entity) where T : unmanaged
	{
		var storage = GetComponentStorage<T>();
		return storage.Has(entity);
	}

	internal bool Has(in Entity entity, in TypeId typeId)
	{
		return EntityComponentIndex[entity].Contains(typeId);
	}

	public bool Some<T>() where T : unmanaged
	{
		var storage = GetComponentStorage<T>();
		return storage.Any();
	}

	public ref T Get<T>(in Entity entity) where T : unmanaged
	{
		var storage = GetComponentStorage<T>();
		return ref storage.Get<T>(entity);
	}

	public ref T GetSingleton<T>() where T : unmanaged
	{
		var storage = GetComponentStorage<T>();
		return ref storage.GetFirst<T>();
	}

	public Entity GetSingletonEntity<T>() where T : unmanaged
	{
		var storage = GetComponentStorage<T>();
		return storage.FirstEntity();
	}

	public void Set<T>(in Entity entity, in T component) where T : unmanaged
	{
		var componentStorage = GetComponentStorage<T>();

		if (!componentStorage.Set(entity, component))
		{
			EntityComponentIndex[entity].Add(componentStorage.TypeId);

			foreach (var filter in TypeToFilter[componentStorage.TypeId])
			{
				filter.Check(entity);
			}
		}
	}

	public void Remove<T>(in Entity entity) where T : unmanaged
	{
		var componentStorage = GetComponentStorage<T>();

		if (componentStorage.Remove(entity))
		{
			EntityComponentIndex[entity].Remove(componentStorage.TypeId);

			foreach (var filter in TypeToFilter[componentStorage.TypeId])
			{
				filter.Check(entity);
			}
		}
	}

	// RELATIONS

	private RelationStorage RegisterRelationType(TypeId typeId)
	{
		var relationStorage = new RelationStorage(ElementSizes[typeId]);
		RelationIndex.Add(typeId, relationStorage);
		return relationStorage;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private RelationStorage GetRelationStorage<T>() where T : unmanaged
	{
		var typeId = GetTypeId<T>();
		if (RelationIndex.TryGetValue(typeId, out var relationStorage))
		{
			return relationStorage;
		}

		return RegisterRelationType(typeId);
	}

	public void Relate<T>(in Entity entityA, in Entity entityB, in T relation) where T : unmanaged
	{
		var relationStorage = GetRelationStorage<T>();
		relationStorage.Set(entityA, entityB, relation);
		EntityRelationIndex[entityA].Add(TypeToId[typeof(T)]);
		EntityRelationIndex[entityB].Add(TypeToId[typeof(T)]);
	}

	public void Unrelate<T>(in Entity entityA, in Entity entityB) where T : unmanaged
	{
		var relationStorage = GetRelationStorage<T>();
		relationStorage.Remove(entityA, entityB);
	}

	public void UnrelateAll<T>(in Entity entity) where T : unmanaged
	{
		var relationStorage = GetRelationStorage<T>();
		relationStorage.RemoveEntity(entity);
	}

	public bool Related<T>(in Entity entityA, in Entity entityB) where T : unmanaged
	{
		var relationStorage = GetRelationStorage<T>();
		return relationStorage.Has(entityA, entityB);
	}

	public T GetRelationData<T>(in Entity entityA, in Entity entityB) where T : unmanaged
	{
		var relationStorage = GetRelationStorage<T>();
		return relationStorage.Get<T>(entityA, entityB);
	}

	public ReverseSpanEnumerator<(Entity, Entity)> Relations<T>() where T : unmanaged
	{
		var relationStorage = GetRelationStorage<T>();
		return relationStorage.All();
	}

	public ReverseSpanEnumerator<Entity> OutRelations<T>(Entity entity) where T : unmanaged
	{
		var relationStorage = GetRelationStorage<T>();
		return relationStorage.OutRelations(entity);
	}

	public Entity OutRelationSingleton<T>(in Entity entity) where T : unmanaged
	{
		var relationStorage = GetRelationStorage<T>();
		return relationStorage.OutFirst(entity);
	}

	public bool HasOutRelation<T>(in Entity entity) where T : unmanaged
	{
		var relationStorage = GetRelationStorage<T>();
		return relationStorage.HasOutRelation(entity);
	}

	public int OutRelationCount<T>(in Entity entity) where T : unmanaged
	{
		var relationStorage = GetRelationStorage<T>();
		return relationStorage.OutRelationCount(entity);
	}

	public Entity NthOutRelation<T>(in Entity entity, int n) where T : unmanaged
	{
		var relationStorage = GetRelationStorage<T>();
		return relationStorage.OutNth(entity, n);
	}

	public ReverseSpanEnumerator<Entity> InRelations<T>(Entity entity) where T : unmanaged
	{
		var relationStorage = GetRelationStorage<T>();
		return relationStorage.InRelations(entity);
	}

	public Entity InRelationSingleton<T>(in Entity entity) where T : unmanaged
	{
		var relationStorage = GetRelationStorage<T>();
		return relationStorage.InFirst(entity);
	}

	public bool HasInRelation<T>(in Entity entity) where T : unmanaged
	{
		var relationStorage = GetRelationStorage<T>();
		return relationStorage.HasInRelation(entity);
	}

	public int InRelationCount<T>(in Entity entity) where T : unmanaged
	{
		var relationStorage = GetRelationStorage<T>();
		return relationStorage.InRelationCount(entity);
	}

	public Entity NthInRelation<T>(in Entity entity, int n) where T : unmanaged
	{
		var relationStorage = GetRelationStorage<T>();
		return relationStorage.InNth(entity, n);
	}

	// MESSAGES

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

	public void ClearMessages<T>() where T : unmanaged
	{
		var typeId = GetMessageTypeId<T>();
		MessageIndex[typeId].Clear();
	}

	// TODO: temporary component storage?
	public void FinishUpdate()
	{
		foreach (var (_, messageStorage) in MessageIndex)
		{
			messageStorage.Clear();
		}
	}

	// DEBUG
	// NOTE: these methods are very inefficient
	// they should only be used in debugging contexts!!
#if DEBUG
	public ComponentTypeEnumerator Debug_GetAllComponentTypes(Entity entity)
	{
		return new ComponentTypeEnumerator(this, EntityComponentIndex[entity]);
	}

	public IEnumerable<Entity> Debug_GetEntities(Type componentType)
	{
		var storage = ComponentIndex[TypeToId[componentType]];
		return storage.Debug_GetEntities();
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

	public ref struct ComponentTypeEnumerator
	{
		private World World;
		private IndexableSet<TypeId> Types;
		private int ComponentIndex;

		public ComponentTypeEnumerator GetEnumerator() => this;

		internal ComponentTypeEnumerator(
			World world,
			IndexableSet<TypeId> types
		)
		{
			World = world;
			Types = types;
			ComponentIndex = -1;
		}

		public bool MoveNext()
		{
			ComponentIndex += 1;
			return ComponentIndex < Types.Count;
		}

		public Type Current => World.IdToType[Types[ComponentIndex]];
	}
#endif

	protected virtual void Dispose(bool disposing)
	{
		if (!IsDisposed)
		{
			if (disposing)
			{
				foreach (var componentStorage in ComponentIndex.Values)
				{
					componentStorage.Dispose();
				}

				foreach (var relationStorage in RelationIndex.Values)
				{
					relationStorage.Dispose();
				}

				foreach (var messageStorage in MessageIndex.Values)
				{
					messageStorage.Dispose();
				}

				foreach (var typeSet in EntityComponentIndex.Values)
				{
					typeSet.Dispose();
				}

				foreach (var typeSet in EntityRelationIndex.Values)
				{
					typeSet.Dispose();
				}

				foreach (var filter in FilterIndex.Values)
				{
					filter.Dispose();
				}

				EntityIdAssigner.Dispose();
				TypeIdAssigner.Dispose();
			}

			IsDisposed = true;
		}
	}

	// // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
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
