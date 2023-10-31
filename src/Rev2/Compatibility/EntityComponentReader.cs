namespace MoonTools.ECS.Rev2.Compatibility;

public abstract class EntityComponentReader
{
	protected World World;

	protected EntityComponentReader(World world)
	{
		World = world;
	}

	protected bool Has<T>(in Id entityId) where T : unmanaged => World.Has<T>(entityId);
	protected bool Some<T>() where T : unmanaged => World.Some<T>();
	protected ref T Get<T>(in Id entityId) where T : unmanaged => ref World.Get<T>(entityId);
	protected ref T GetSingleton<T>() where T : unmanaged => ref World.GetSingleton<T>();
	protected Id GetSingletonEntity<T>() where T : unmanaged => World.GetSingletonEntity<T>();

	protected ReverseSpanEnumerator<(Id, Id)> Relations<T>() where T : unmanaged => World.Relations<T>();
	protected bool Related<T>(in Id entityA, in Id entityB) where T : unmanaged => World.Related<T>(entityA, entityB);
	protected T GetRelationData<T>(in Id entityA, in Id entityB) where T : unmanaged => World.GetRelationData<T>(entityA, entityB);

	protected ReverseSpanEnumerator<Id> OutRelations<T>(in Id entity) where T : unmanaged => World.OutRelations<T>(entity);
	protected Id OutRelationSingleton<T>(in Id entity) where T : unmanaged => World.OutRelationSingleton<T>(entity);
	protected bool HasOutRelation<T>(in Id entity) where T : unmanaged => World.HasOutRelation<T>(entity);

	protected ReverseSpanEnumerator<Id> InRelations<T>(in Id entity) where T : unmanaged => World.InRelations<T>(entity);
	protected Id InRelationSingleton<T>(in Id entity) where T : unmanaged => World.InRelationSingleton<T>(entity);
	protected bool HasInRelation<T>(in Id entity) where T : unmanaged => World.HasInRelation<T>(entity);
}
