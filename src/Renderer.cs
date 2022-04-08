namespace MoonTools.ECS
{
	public abstract class Renderer : EntityComponentReader
	{
		public Renderer(World world)
		{
			world.AddRenderer(this);
		}
	}
}
