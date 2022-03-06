namespace MoonTools.ECS;

public class World
{
	private readonly List<System> Systems = new List<System>();
	private readonly List<Renderer> Renderers = new List<Renderer>();
	private readonly List<Filter> Filters = new List<Filter>();
	private EntityStorage EntityStorage { get; } = new EntityStorage();
	private ComponentDepot ComponentDepot { get; } = new ComponentDepot();

	internal void AddSystem(System system)
	{
		system.RegisterEntityStorage(EntityStorage);
		system.RegisterComponentDepot(ComponentDepot);
		Systems.Add(system);
	}

	internal void AddRenderer(Renderer renderer)
	{
		renderer.RegisterEntityStorage(EntityStorage);
		renderer.RegisterComponentDepot(ComponentDepot);
		Renderers.Add(renderer);
	}

	public void Update(TimeSpan delta)
	{
		foreach (var system in Systems)
		{
			system.Update(delta);
		}
	}

	public void Draw(TimeSpan delta)
	{
		foreach (var renderer in Renderers)
		{
			renderer.Draw(delta);
		}
	}
}
