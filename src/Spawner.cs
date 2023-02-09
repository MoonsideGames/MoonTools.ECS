namespace MoonTools.ECS
{
	public abstract class Spawner
	{
		private World World;

		public Spawner(World world)
		{
			World = world;
		}

		protected Entity CreateEntity() => World.CreateEntity();
		protected void Set<TComponent>(in Entity entity, in TComponent component) where TComponent : unmanaged => World.Set<TComponent>(entity, component);
		protected void Relate<TRelationKind>(in Entity entityA, in Entity entityB, TRelationKind relationData) where TRelationKind : unmanaged => World.Relate(entityA, entityB, relationData);
	}
}
