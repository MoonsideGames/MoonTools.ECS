using System.Collections.Generic;

namespace MoonTools.ECS.Rev2
{
	public class Archetype
	{
		public ArchetypeSignature Signature;
		public ArchetypeId Id { get; private set; }
		/* FIXME: make this native memory too */
		public List<Column> Components = new List<Column>();
		public Dictionary<ComponentId, ArchetypeEdge> Edges = new Dictionary<ComponentId, ArchetypeEdge>();

		public int Count;

		public Archetype(ArchetypeId id, ArchetypeSignature signature)
		{
			Id = id;
			Signature = signature;
			Count = 0;
		}
	}
}
