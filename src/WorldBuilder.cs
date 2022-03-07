namespace MoonTools.ECS;

public class WorldBuilder
{
	private ComponentDepot componentDepot;
	private EntityStorage entityStorage;
	private MessageDepot messageDepot;

	private readonly List<System> systems = new List<System>();
	private readonly List<Renderer> renderers = new List<Renderer>();

	public WorldBuilder()
	{
		componentDepot = new ComponentDepot();
		entityStorage = new EntityStorage();
		messageDepot = new MessageDepot();
	}

	public void AddSystem(System system)
	{
		system.RegisterEntityStorage(entityStorage);
		system.RegisterComponentDepot(componentDepot);
		system.RegisterMessageDepot(messageDepot);
		systems.Add(system);
	}

	public void AddRenderer(Renderer renderer)
	{
		renderer.RegisterEntityStorage(entityStorage);
		renderer.RegisterComponentDepot(componentDepot);
		renderers.Add(renderer);
	}

	public Entity CreateEntity()
	{
		return entityStorage.Create();
	}

	public void Set<TComponent>(Entity entity, in TComponent component) where TComponent : struct
	{
		componentDepot.Set(entity.ID, component);
	}

	public void Send<TMessage>(in TMessage message) where TMessage : struct
	{
		messageDepot.Add(message);
	}

	public World Build()
	{
		return new World(
			systems,
			renderers,
			componentDepot,
			entityStorage,
			messageDepot
		);
	}
}
