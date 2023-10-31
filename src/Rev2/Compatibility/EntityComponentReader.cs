namespace MoonTools.ECS.Rev2.Compatibility;

public abstract class EntityComponentReader
{
	protected World World;

	protected EntityComponentReader(World world)
	{
		World = world;
	}

	protected bool Has<T>(in EntityId entityId) where T : unmanaged => World.Has<T>(entityId);
	protected bool Some<T>() where T : unmanaged => World.Some<T>();
	protected ref T Get<T>(in EntityId entityId) where T : unmanaged => ref World.Get<T>(entityId);
	protected ref T GetSingleton<T>() where T : unmanaged => ref World.GetSingleton<T>();
	protected EntityId GetSingletonEntity<T>() where T : unmanaged => World.GetSingletonEntity<T>();

	protected ReverseSpanEnumerator<(EntityId, EntityId)> Relations<T>() where T : unmanaged => World.Relations<T>();
	protected bool Related<T>(in EntityId entityA, in EntityId entityB) where T : unmanaged => World.Related<T>(entityA, entityB);
	protected T GetRelationData<T>(in EntityId entityA, in EntityId entityB) where T : unmanaged => World.GetRelationData<T>(entityA, entityB);

	protected ReverseSpanEnumerator<EntityId> OutRelations<T>(in EntityId entity) where T : unmanaged => World.OutRelations<T>(entity);
	protected EntityId OutRelationSingleton<T>(in EntityId entity) where T : unmanaged => World.OutRelationSingleton<T>(entity);
	protected bool HasOutRelation<T>(in EntityId entity) where T : unmanaged => World.HasOutRelation<T>(entity);

	protected ReverseSpanEnumerator<EntityId> InRelations<T>(in EntityId entity) where T : unmanaged => World.InRelations<T>(entity);
	protected EntityId InRelationSingleton<T>(in EntityId entity) where T : unmanaged => World.InRelationSingleton<T>(entity);
	protected bool HasInRelation<T>(in EntityId entity) where T : unmanaged => World.HasInRelation<T>(entity);
}
