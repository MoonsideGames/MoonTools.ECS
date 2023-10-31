using System;

namespace MoonTools.ECS.Rev2;

public readonly record struct EntityId(uint Value) : IComparable<EntityId>
{
	public int CompareTo(EntityId other)
	{
		return Value.CompareTo(other.Value);
	}
}
