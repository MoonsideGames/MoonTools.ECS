namespace MoonTools.ECS;

public abstract class System : EntityComponentReader
{
	public abstract void Update(TimeSpan delta);

	public System(World world)
	{
		world.AddSystem(this);
	}

	protected Entity CreateEntity()
	{
		return EntityStorage.Create();
	}

	protected FilterBuilder CreateFilterBuilder()
	{
		return new FilterBuilder(ComponentDepot);
	}

	protected void Set<TComponent>(in Entity entity, in TComponent component) where TComponent : struct
	{
		ComponentDepot.Set<TComponent>(entity.ID, component);
	}

	protected void Remove<TComponent>(in Entity entity) where TComponent : struct
	{
		ComponentDepot.Remove<TComponent>(entity.ID);
	}

	protected void Destroy(in Entity entity)
	{
		ComponentDepot.OnEntityDestroy(entity.ID);
		EntityStorage.Destroy(entity);
	}
}
