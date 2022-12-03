using System.Collections.Generic;

namespace MoonTools.ECS
{
    internal class EntityStorageState
    {
		public int NextID;
		public List<int> availableIDs = new List<int>();

		public Dictionary<int, HashSet<int>> EntityToComponentTypeIndices = new Dictionary<int, HashSet<int>>();
		public Dictionary<int, HashSet<int>> EntityToRelationTypeIndices = new Dictionary<int, HashSet<int>>();
	}
}
