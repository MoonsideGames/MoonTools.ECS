namespace MoonTools.ECS;

internal class EntityStorage
{
	public IDStorage idStorage = new IDStorage();

	public Entity Create()
	{
		return new Entity(idStorage.NextID());
	}

	public void Destroy(in Entity entity)
	{
		idStorage.Release(entity.ID);
	}
}
