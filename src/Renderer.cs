namespace MoonTools.ECS;

public abstract class Renderer : EntityComponentReader
{
	public abstract void Draw(TimeSpan delta);
}
