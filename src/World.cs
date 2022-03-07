namespace MoonTools.ECS;

public class World
{
	private readonly List<System> Systems;
	private readonly List<Renderer> Renderers;
	private readonly EntityStorage EntityStorage;
	private readonly ComponentDepot ComponentDepot;
	private MessageDepot MessageDepot;

	internal World(
		List<System> systems,
		List<Renderer> renderers,
		ComponentDepot componentDepot,
		EntityStorage entityStorage,
		MessageDepot messageDepot
	)
	{
		Systems = systems;
		Renderers = renderers;
		ComponentDepot = componentDepot;
		EntityStorage = entityStorage;
		MessageDepot = messageDepot;
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
