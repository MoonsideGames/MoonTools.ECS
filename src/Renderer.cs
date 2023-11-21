namespace MoonTools.ECS;

public abstract class Renderer : EntityComponentReader
{
	public Renderer(World world) : base(world) { }
}
