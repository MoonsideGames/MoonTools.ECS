using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MoonTools.ECS.Collections;

namespace MoonTools.ECS;

public class World : IDisposable
{
#if DEBUG
	// TODO: is there a smarter way to do this?
	internal static Dictionary<Type, TypeId> ComponentTypeToId = new Dictionary<Type, TypeId>();
	internal static List<Type> ComponentTypeIdToType = new List<Type>();
#endif

	internal static List<int> ComponentTypeElementSizes = new List<int>();
	internal static List<int> RelationTypeElementSizes = new List<int>();
	internal static List<int> MessageTypeElementSizes = new List<int>();

	// Filters
	internal readonly Dictionary<FilterSignature, Filter> FilterIndex = new Dictionary<FilterSignature, Filter>();
	private readonly List<List<Filter>> ComponentTypeToFilter = new List<List<Filter>>();

	// TODO: can we make the tag an native array of chars at some point?
	internal List<string> EntityTags = new List<string>();

	// Relation Storages
	internal List<RelationStorage> RelationIndex = new List<RelationStorage>();
	internal List<IndexableSet<TypeId>> EntityRelationIndex = new List<IndexableSet<TypeId>>();

	// Message Storages
	private List<MessageStorage> MessageIndex = new List<MessageStorage>();

	public FilterBuilder FilterBuilder => new FilterBuilder(this);

	internal readonly List<ComponentStorage> ComponentIndex = new List<ComponentStorage>();
	internal List<IndexableSet<TypeId>> EntityComponentIndex = new List<IndexableSet<TypeId>>();

	internal IdAssigner EntityIdAssigner = new IdAssigner();

	private bool IsDisposed;

	internal TypeId GetComponentTypeId<T>() where T : unmanaged
	{
		var typeId = new TypeId(ComponentTypeIdAssigner<T>.Id);
		if (typeId < ComponentIndex.Count)
		{
			return typeId;
		}

		// add missing storages, it's possible for there to be multiples in multi-world scenarios
		for (var i = ComponentIndex.Count; i <= typeId; i += 1)
		{
			var missingTypeId = new TypeId((uint) i);
			var componentStorage = new ComponentStorage(missingTypeId, ComponentTypeElementSizes[i]);
			ComponentIndex.Add(componentStorage);
			ComponentTypeToFilter.Add(new List<Filter>());
		}

		return typeId;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private ComponentStorage GetComponentStorage<T>() where T : unmanaged
	{
		var typeId = GetComponentTypeId<T>();
		return ComponentIndex[typeId];
	}

	// FILTERS

	internal Filter GetFilter(FilterSignature signature)
	{
		if (!FilterIndex.TryGetValue(signature, out var filter))
		{
			filter = new Filter(this, signature);

			foreach (var typeId in signature.Included)
			{
				ComponentTypeToFilter[(int) typeId.Value].Add(filter);
			}

			foreach (var typeId in signature.Excluded)
			{
				ComponentTypeToFilter[(int) typeId.Value].Add(filter);
			}

			FilterIndex.Add(signature, filter);
		}

		return filter;
	}

	// ENTITIES

	public Entity CreateEntity(string tag = "")
	{
		var entity = new Entity(EntityIdAssigner.Assign());

		if (entity.ID == EntityComponentIndex.Count)
		{
			EntityRelationIndex.Add(new IndexableSet<TypeId>());
			EntityComponentIndex.Add(new IndexableSet<TypeId>());
			EntityTags.Add(tag);
		}

		return entity;
	}

	public void Tag(Entity entity, string tag)
	{
		EntityTags[(int) entity.ID] = tag;
	}

	public string GetTag(Entity entity)
	{
		return EntityTags[(int) entity.ID];
	}

	public void Destroy(in Entity entity)
	{
		var componentSet = EntityComponentIndex[(int) entity.ID];
		var relationSet = EntityRelationIndex[(int) entity.ID];

		// remove all components from storages
		foreach (var componentTypeIndex in componentSet)
		{
			var componentStorage = ComponentIndex[componentTypeIndex];
			componentStorage.Remove(entity);

			foreach (var filter in ComponentTypeToFilter[componentTypeIndex])
			{
				filter.RemoveEntity(entity);
			}
		}

		// remove all relations from storage
		foreach (var relationTypeIndex in relationSet)
		{
			var relationStorage = RelationIndex[relationTypeIndex];
			relationStorage.RemoveEntity(entity);
		}

		componentSet.Clear();
		relationSet.Clear();

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
		return EntityComponentIndex[(int) entity.ID].Contains(typeId);
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
			EntityComponentIndex[(int) entity.ID].Add(componentStorage.TypeId);

			foreach (var filter in ComponentTypeToFilter[componentStorage.TypeId])
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
			EntityComponentIndex[(int) entity.ID].Remove(componentStorage.TypeId);

			foreach (var filter in ComponentTypeToFilter[componentStorage.TypeId])
			{
				filter.Check(entity);
			}
		}
	}

	// RELATIONS

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private RelationStorage GetRelationStorage<T>() where T : unmanaged
	{
		var typeId = new TypeId(RelationTypeIdAssigner<T>.Id);
		if (typeId.Value < RelationIndex.Count)
		{
			return RelationIndex[typeId];
		}

		for (var i = RelationIndex.Count; i <= typeId; i += 1)
		{
			RelationIndex.Add(new RelationStorage(RelationTypeElementSizes[i]));
		}

		return RelationIndex[typeId];
	}

	public void Relate<T>(in Entity entityA, in Entity entityB, in T relation) where T : unmanaged
	{
		var relationStorage = GetRelationStorage<T>();
		relationStorage.Set(entityA, entityB, relation);
		EntityRelationIndex[(int) entityA.ID].Add(new TypeId(RelationTypeIdAssigner<T>.Id));
		EntityRelationIndex[(int) entityB.ID].Add(new TypeId(RelationTypeIdAssigner<T>.Id));
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

	private MessageStorage GetMessageStorage<T>() where T : unmanaged
	{
		var typeId = new TypeId(MessageTypeIdAssigner<T>.Id);

		if (typeId < MessageIndex.Count)
		{
			return MessageIndex[typeId];
		}

		for (var i = MessageIndex.Count; i <= typeId; i += 1)
		{
			MessageIndex.Add(new MessageStorage(MessageTypeElementSizes[i]));
		}

		return MessageIndex[typeId];
	}

	public void Send<T>(in T message) where T : unmanaged
	{
		GetMessageStorage<T>().Add(message);
	}

	public bool SomeMessage<T>() where T : unmanaged
	{
		return GetMessageStorage<T>().Some();
	}

	public ReadOnlySpan<T> ReadMessages<T>() where T : unmanaged
	{
		return GetMessageStorage<T>().All<T>();
	}

	public T ReadMessage<T>() where T : unmanaged
	{
		return GetMessageStorage<T>().First<T>();
	}

	public void ClearMessages<T>() where T : unmanaged
	{
		GetMessageStorage<T>().Clear();
	}

	// TODO: temporary component storage?
	public void FinishUpdate()
	{
		foreach (var messageStorage in MessageIndex)
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
		return new ComponentTypeEnumerator(this, EntityComponentIndex[(int) entity.ID]);
	}

	public IEnumerable<Entity> Debug_GetEntities(Type componentType)
	{
		var storage = ComponentIndex[ComponentTypeToId[componentType]];
		return storage.Debug_GetEntities();
	}

	public IEnumerable<Type> Debug_SearchComponentType(string typeString)
	{
		foreach (var type in ComponentTypeToId.Keys)
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

		public Type Current => ComponentTypeIdToType[Types[ComponentIndex]];
	}
#endif

	protected virtual void Dispose(bool disposing)
	{
		if (!IsDisposed)
		{
			if (disposing)
			{
				foreach (var componentStorage in ComponentIndex)
				{
					componentStorage.Dispose();
				}

				foreach (var relationStorage in RelationIndex)
				{
					relationStorage.Dispose();
				}

				foreach (var messageStorage in MessageIndex)
				{
					messageStorage.Dispose();
				}

				foreach (var componentSet in EntityComponentIndex)
				{
					componentSet.Dispose();
				}

				foreach (var relationSet in EntityRelationIndex)
				{
					relationSet.Dispose();
				}

				foreach (var filter in FilterIndex.Values)
				{
					filter.Dispose();
				}

				EntityIdAssigner.Dispose();
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
