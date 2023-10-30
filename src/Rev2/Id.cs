using System;

namespace MoonTools.ECS.Rev2;

public readonly record struct Id(uint Value) : IComparable<Id>
{
	public int CompareTo(Id other)
	{
		return Value.CompareTo(other.Value);
	}
}
