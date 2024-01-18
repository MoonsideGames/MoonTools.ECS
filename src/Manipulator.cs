namespace MoonTools.ECS;

public abstract class Manipulator : EntityComponentReader
{
	public Manipulator(World world) : base(world)
	{
	}

	protected Entity CreateEntity(string tag = "") => World.CreateEntity(tag);
	protected void Tag(Entity entity, string tag) => World.Tag(entity, tag);
	protected void Set<TComponent>(in Entity entity, in TComponent component) where TComponent : unmanaged => World.Set<TComponent>(entity, component);
	protected void Remove<TComponent>(in Entity entity) where TComponent : unmanaged => World.Remove<TComponent>(entity);

	protected void Relate<TRelationKind>(in Entity entityA, in Entity entityB, TRelationKind relationData) where TRelationKind : unmanaged => World.Relate(entityA, entityB, relationData);
	protected void Unrelate<TRelationKind>(in Entity entityA, in Entity entityB) where TRelationKind : unmanaged => World.Unrelate<TRelationKind>(entityA, entityB);
	protected void UnrelateAll<TRelationKind>(in Entity entity) where TRelationKind : unmanaged => World.UnrelateAll<TRelationKind>(entity);
	protected void Destroy(in Entity entity) => World.Destroy(entity);

	protected void Send<TMessage>(in TMessage message) where TMessage : unmanaged => World.Send(message);
}
