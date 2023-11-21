namespace MoonTools.ECS;

public abstract class EntityComponentReader
{
	protected readonly World World;
	public FilterBuilder FilterBuilder => World.FilterBuilder;

	protected EntityComponentReader(World world)
	{
		World = world;
	}

	protected string GetTag(in Entity entity) => World.GetTag(entity);

	protected bool Has<T>(in Entity Entity) where T : unmanaged => World.Has<T>(Entity);
	protected bool Some<T>() where T : unmanaged => World.Some<T>();
	protected ref T Get<T>(in Entity Entity) where T : unmanaged => ref World.Get<T>(Entity);
	protected ref T GetSingleton<T>() where T : unmanaged => ref World.GetSingleton<T>();
	protected Entity GetSingletonEntity<T>() where T : unmanaged => World.GetSingletonEntity<T>();

	protected ReverseSpanEnumerator<(Entity, Entity)> Relations<T>() where T : unmanaged => World.Relations<T>();
	protected bool Related<T>(in Entity entityA, in Entity entityB) where T : unmanaged => World.Related<T>(entityA, entityB);
	protected T GetRelationData<T>(in Entity entityA, in Entity entityB) where T : unmanaged => World.GetRelationData<T>(entityA, entityB);

	protected ReverseSpanEnumerator<Entity> OutRelations<T>(in Entity entity) where T : unmanaged => World.OutRelations<T>(entity);
	protected Entity OutRelationSingleton<T>(in Entity entity) where T : unmanaged => World.OutRelationSingleton<T>(entity);
	protected bool HasOutRelation<T>(in Entity entity) where T : unmanaged => World.HasOutRelation<T>(entity);
	protected int OutRelationCount<T>(in Entity entity) where T : unmanaged => World.OutRelationCount<T>(entity);
	protected Entity NthOutRelation<T>(in Entity entity, int n) where T : unmanaged => World.NthOutRelation<T>(entity, n);

	protected ReverseSpanEnumerator<Entity> InRelations<T>(in Entity entity) where T : unmanaged => World.InRelations<T>(entity);
	protected Entity InRelationSingleton<T>(in Entity entity) where T : unmanaged => World.InRelationSingleton<T>(entity);
	protected bool HasInRelation<T>(in Entity entity) where T : unmanaged => World.HasInRelation<T>(entity);
	protected int InRelationCount<T>(in Entity entity) where T : unmanaged => World.InRelationCount<T>(entity);
	protected Entity NthInRelation<T>(in Entity entity, int n) where T : unmanaged => World.NthInRelation<T>(entity, n);
}
