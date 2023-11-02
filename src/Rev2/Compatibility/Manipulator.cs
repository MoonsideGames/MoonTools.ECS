namespace MoonTools.ECS.Rev2.Compatibility;

public class Manipulator : EntityComponentReader
{
	public Manipulator(World world) : base(world) { }

	protected EntityId CreateEntity(string tag = "") => World.CreateEntity(tag);
	protected void Tag(in EntityId entity, string tag) => World.Tag(entity, tag);
	protected void Set<TComponent>(in EntityId entity, in TComponent component) where TComponent : unmanaged => World.Set<TComponent>(entity, component);
	protected void Remove<TComponent>(in EntityId entity) where TComponent : unmanaged => World.Remove<TComponent>(entity);

	protected void Relate<T>(in EntityId entityA, in EntityId entityB, in T relation) where T : unmanaged => World.Relate(entityA, entityB, relation);
	protected void Unrelate<T>(in EntityId entityA, in EntityId entityB) where T : unmanaged => World.Unrelate<T>(entityA, entityB);
	protected void UnrelateAll<T>(in EntityId entity) where T : unmanaged => World.UnrelateAll<T>(entity);
	protected void Destroy(in EntityId entity) => World.Destroy(entity);
}
