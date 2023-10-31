namespace MoonTools.ECS.Rev2.Compatibility;

public class Manipulator : EntityComponentReader
{
	public Manipulator(World world) : base(world) { }

	protected void Set<TComponent>(in Id entity, in TComponent component) where TComponent : unmanaged => World.Set<TComponent>(entity, component);
	protected void Remove<TComponent>(in Id entity) where TComponent : unmanaged => World.Remove<TComponent>(entity);

	protected void Unrelate<TRelationKind>(in Id entityA, in Id entityB) where TRelationKind : unmanaged => World.Unrelate<TRelationKind>(entityA, entityB);
	protected void UnrelateAll<TRelationKind>(in Id entity) where TRelationKind : unmanaged => World.UnrelateAll<TRelationKind>(entity);
	protected void Destroy(in Id entity) => World.Destroy(entity);
}
