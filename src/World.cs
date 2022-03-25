namespace MoonTools.ECS;

public class World
{
	private readonly EntityStorage EntityStorage = new EntityStorage();
	private readonly ComponentDepot ComponentDepot = new ComponentDepot();
	private MessageDepot MessageDepot = new MessageDepot();

	internal void AddSystem(System system)
	{
		system.RegisterEntityStorage(EntityStorage);
		system.RegisterComponentDepot(ComponentDepot);
		system.RegisterMessageDepot(MessageDepot);
	}

	internal void AddRenderer(Renderer renderer)
	{
		renderer.RegisterEntityStorage(EntityStorage);
		renderer.RegisterComponentDepot(ComponentDepot);
	}

	public Entity CreateEntity()
	{
		return EntityStorage.Create();
	}

	public void Set<TComponent>(Entity entity, in TComponent component) where TComponent : struct
	{
		ComponentDepot.Set(entity.ID, component);
	}

	public void Send<TMessage>(in TMessage message) where TMessage : struct
	{
		MessageDepot.Add(message);
	}

	public void FinishUpdate()
	{
		MessageDepot.Clear();
	}
}
