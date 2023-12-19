namespace MoonTools.ECS;

public readonly record struct Entity(int ID)
{
	public static readonly Entity Null = new Entity(int.MaxValue);
}
