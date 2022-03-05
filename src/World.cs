namespace MoonTools.ECS;

public class World
{
	private readonly List<System> systems = new List<System>();
	private readonly List<Renderer> renderers = new List<Renderer>();
	private EntityStorage EntityStorage { get; } = new EntityStorage();
	private ComponentDepot ComponentDepot { get; } = new ComponentDepot();

	public void AddSystem(System system)
	{
		system.RegisterEntityStorage(EntityStorage);
		system.RegisterComponentDepot(ComponentDepot);
		systems.Add(system);
	}

	public void AddRenderer(Renderer renderer)
	{
		renderer.RegisterEntityStorage(EntityStorage);
		renderer.RegisterComponentDepot(ComponentDepot);
		renderers.Add(renderer);
	}

	public void Update(TimeSpan delta)
	{
		foreach (var system in systems)
		{
			system.Update(delta);
		}
	}

	public void Draw(TimeSpan delta)
	{
		foreach (var renderer in renderers)
		{
			renderer.Draw(delta);
		}
	}
}
