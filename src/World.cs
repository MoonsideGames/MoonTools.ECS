namespace MoonTools.ECS;

public class World
{
	private readonly List<System> Systems = new List<System>();
	private readonly List<Renderer> Renderers = new List<Renderer>();
	private readonly EntityStorage EntityStorage = new EntityStorage();
	private readonly ComponentDepot ComponentDepot = new ComponentDepot();
	private MessageDepot MessageDepot = new MessageDepot();

	internal void AddSystem(System system)
	{
		system.RegisterEntityStorage(EntityStorage);
		system.RegisterComponentDepot(ComponentDepot);
		system.RegisterMessageDepot(MessageDepot);
		Systems.Add(system);
	}

	internal void AddRenderer(Renderer renderer)
	{
		renderer.RegisterEntityStorage(EntityStorage);
		renderer.RegisterComponentDepot(ComponentDepot);
		Renderers.Add(renderer);
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

	public void Update(TimeSpan delta)
	{
		foreach (var system in Systems)
		{
			system.Update(delta);
		}

		MessageDepot.Clear();
	}

	public void Draw(TimeSpan delta)
	{
		foreach (var renderer in Renderers)
		{
			renderer.Draw(delta);
		}
	}
}
