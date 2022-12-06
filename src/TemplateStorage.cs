using System.Collections.Generic;

namespace MoonTools.ECS
{
	public class TemplateStorage
	{
		private int nextID = 0;

		private Dictionary<int, HashSet<int>> TemplateToComponentTypeIndices = new Dictionary<int, HashSet<int>>();

		public Template Create()
		{
			TemplateToComponentTypeIndices.Add(nextID, new HashSet<int>());
			return new Template(NextID());
		}

		public bool SetComponent(int templateID, int componentTypeIndex)
		{
			return TemplateToComponentTypeIndices[templateID].Add(componentTypeIndex);
		}

		public IEnumerable<int> ComponentTypeIndices(int templateID)
		{
			return TemplateToComponentTypeIndices[templateID];
		}

		private int NextID()
		{
			var id = nextID;
			nextID += 1;
			return id;
		}
	}
}
