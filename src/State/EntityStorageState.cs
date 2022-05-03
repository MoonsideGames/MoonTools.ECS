using System.Collections.Generic;

namespace MoonTools.ECS
{
    internal class EntityStorageState
    {
		public int NextID;
		public List<int> availableIDs = new List<int>();
	}
}
