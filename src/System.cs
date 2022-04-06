namespace MoonTools.ECS;

public abstract class System : EntityComponentReader
{
	internal MessageDepot MessageDepot;

	internal void RegisterMessageDepot(MessageDepot messageDepot)
	{
		MessageDepot = messageDepot;
	}

	public System(World world)
	{
		world.AddSystem(this);
	}

	public abstract void Update(TimeSpan delta);

	public virtual void InitializeFilters()
	{

	}

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
		return MessageDepot.All<TMessage>();
	}

	protected TMessage ReadMessage<TMessage>() where TMessage : struct
	{
		return MessageDepot.First<TMessage>();
	}

	protected bool SomeMessage<TMessage>() where TMessage : struct
	{
		return MessageDepot.Some<TMessage>();
	}

	protected IEnumerable<TMessage> ReadMessagesWithEntity<TMessage>(in Entity entity) where TMessage : struct, IHasEntity
	{
		return MessageDepot.WithEntity<TMessage>(entity.ID);
	}

	protected ref readonly TMessage ReadMessageWithEntity<TMessage>(in Entity entity) where TMessage : struct, IHasEntity
	{
		return ref MessageDepot.FirstWithEntity<TMessage>(entity.ID);
	}

	protected bool SomeMessageWithEntity<TMessage>(in Entity entity) where TMessage : struct, IHasEntity
	{
		return MessageDepot.SomeWithEntity<TMessage>(entity.ID);
	}

	protected void Send<TMessage>(in TMessage message) where TMessage : struct
	{
		MessageDepot.Add(message);
	}

	protected void Relate<TRelationKind>(in Entity entityA, in Entity entityB)
	{
		RelationDepot.Add<TRelationKind>(new Relation(entityA, entityB));
	}

	protected void Unrelate<TRelationKind>(in Entity entityA, in Entity entityB)
	{
		RelationDepot.Remove<TRelationKind>(new Relation(entityA, entityB));
	}

	protected void Destroy(in Entity entity)
	{
		ComponentDepot.OnEntityDestroy(entity.ID);
		RelationDepot.OnEntityDestroy(entity.ID);
		EntityStorage.Destroy(entity);
	}
}
