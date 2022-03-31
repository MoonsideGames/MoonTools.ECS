namespace MoonTools.ECS;

internal class EntityStorage
{
	public IDStorage idStorage = new IDStorage();

	public Entity Create()
	{
		return new Entity(idStorage.NextID());
	}

	public bool Exists(in Entity entity)
	{
		return idStorage.Taken(entity.ID);
	}

	public void Destroy(in Entity entity)
	{
		idStorage.Release(entity.ID);
	}
}
