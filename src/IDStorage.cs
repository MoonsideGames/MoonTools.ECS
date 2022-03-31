namespace MoonTools.ECS;

internal class IDStorage
{
	private int nextID = 0;

	private readonly Stack<int> availableIDs = new Stack<int>();

	public int NextID()
	{
		if (availableIDs.Count > 0)
		{
			return availableIDs.Pop();
		}
		else
		{
			var id = nextID;
			nextID += 1;
			return id;
		}
	}

	public bool Taken(int id)
	{
		return !availableIDs.Contains(id) && id < nextID;
	}

	public void Release(int id)
	{
		availableIDs.Push(id);
	}
}
