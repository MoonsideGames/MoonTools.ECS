using System.Collections.Generic;

namespace MoonTools.ECS
{
	internal class IDStorage
	{
		private int nextID = 0;

		private readonly Stack<int> availableIDs = new Stack<int>();
		private readonly HashSet<int> availableIDHash = new HashSet<int>();

		public int NextID()
		{
			if (availableIDs.Count > 0)
			{
				var id = availableIDs.Pop();
				availableIDHash.Remove(id);
				return id;
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
			return !availableIDHash.Contains(id) && id < nextID;
		}

		public void Release(int id)
		{
			availableIDs.Push(id);
			availableIDHash.Add(id);
		}
	}
}
