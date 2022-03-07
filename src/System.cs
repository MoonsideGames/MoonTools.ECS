namespace MoonTools.ECS;

public abstract class System : EntityComponentReader
{
	internal MessageDepot MessageDepot;
	public FilterBuilder FilterBuilder => new FilterBuilder(ComponentDepot);

	public System(World world)
	{
		world.AddSystem(this);
	}

	internal void RegisterMessageDepot(MessageDepot messageDepot)
	{
		MessageDepot = messageDepot;
	}

	public abstract void Update(TimeSpan delta);

	protected Entity CreateEntity()
	{
		return EntityStorage.Create();
	}

	protected void Set<TComponent>(in Entity entity, in TComponent component) where TComponent : struct
	{
		ComponentDepot.Set<TComponent>(entity.ID, component);
	}

	protected void Remove<TComponent>(in Entity entity) where TComponent : struct
	{
		ComponentDepot.Remove<TComponent>(entity.ID);
	}

	protected ReadOnlySpan<TMessage> ReadMessages<TMessage>() where TMessage : struct
	{
		return MessageDepot.Read<TMessage>();
	}

	protected bool SomeMessage<TMessage>() where TMessage : struct
	{
		return MessageDepot.Some<TMessage>();
	}

	protected void Destroy(in Entity entity)
	{
		ComponentDepot.OnEntityDestroy(entity.ID);
		EntityStorage.Destroy(entity);
	}
}
