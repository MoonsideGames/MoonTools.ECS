namespace MoonTools.ECS;

public abstract class EntityComponentReader
{
	internal EntityStorage EntityStorage;
	internal ComponentDepot ComponentDepot;
	protected FilterBuilder FilterBuilder => new FilterBuilder(ComponentDepot);

	internal void RegisterEntityStorage(EntityStorage entityStorage)
	{
		EntityStorage = entityStorage;
	}

	internal void RegisterComponentDepot(ComponentDepot componentDepot)
	{
		ComponentDepot = componentDepot;
	}

	// TODO: is this faster or slower than a single-component Filter?
	protected ReadOnlySpan<Entity> ReadEntities<TComponent>() where TComponent : struct
	{
		return ComponentDepot.ReadEntities<TComponent>();
	}

	protected ReadOnlySpan<TComponent> ReadComponents<TComponent>() where TComponent : struct
	{
		return ComponentDepot.ReadComponents<TComponent>();
	}

	protected bool Has<TComponent>(in Entity entity) where TComponent : struct
	{
		return ComponentDepot.Has<TComponent>(entity.ID);
	}

	protected bool Some<TComponent>() where TComponent : struct
	{
		return ComponentDepot.Some<TComponent>();
	}

	protected ref readonly TComponent Get<TComponent>(in Entity entity) where TComponent : struct
	{
		return ref ComponentDepot.Get<TComponent>(entity.ID);
	}

	protected ref readonly TComponent GetSingleton<TComponent>() where TComponent : struct
	{
		return ref ComponentDepot.Get<TComponent>();
	}

	protected ref readonly Entity GetEntity<TComponent>() where TComponent : struct
	{
		return ref ComponentDepot.GetEntity<TComponent>();
	}

	protected bool Exists(in Entity entity)
	{
		return EntityStorage.Exists(entity);
	}
}
