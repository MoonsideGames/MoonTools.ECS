using System;

namespace MoonTools.ECS.Rev2
{
	public readonly record struct ComponentId(int Id) : IHasId, IComparable<ComponentId>
	{
		public int CompareTo(ComponentId other)
		{
			return Id.CompareTo(other.Id);
		}
	}
}