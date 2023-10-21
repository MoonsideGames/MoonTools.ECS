using System.Collections.Generic;

namespace MoonTools.ECS.Rev2
{
	public class Archetype
	{
		public ArchetypeSignature Signature;
		public ArchetypeId Id { get; private set; }
		public List<Column> Components = new List<Column>();
		public List<EntityId> RowToEntity = new List<EntityId>();

		public Dictionary<ComponentId, int> ComponentToColumnIndex =
			new Dictionary<ComponentId, int>();
		public SortedDictionary<ComponentId, ArchetypeEdge> Edges = new SortedDictionary<ComponentId, ArchetypeEdge>();

		public int Count;

		public Archetype(ArchetypeId id, ArchetypeSignature signature)
		{
			Id = id;
			Signature = signature;
			Count = 0;
		}
	}
}
