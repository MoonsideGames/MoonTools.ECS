namespace MoonTools.ECS;

public abstract class EntityComponentReader
{
	internal EntityStorage EntityStorage;
	internal ComponentDepot ComponentDepot;

	internal void RegisterEntityStorage(EntityStorage entityStorage)
	{
		EntityStorage = entityStorage;
	}

	internal void RegisterComponentDepot(ComponentDepot componentDepot)
	{
		ComponentDepot = componentDepot;
	}

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

	protected TComponent Get<TComponent>(in Entity entity) where TComponent : struct
	{
		return ComponentDepot.Get<TComponent>(entity.ID);
	}
}
