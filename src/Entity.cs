namespace MoonTools.ECS;

public struct Entity
{
	public int ID { get; }

	internal Entity(int id)
	{
		ID = id;
	}
}
