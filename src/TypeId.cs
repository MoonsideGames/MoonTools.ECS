using System;

namespace MoonTools.ECS;

public readonly record struct TypeId(uint Value) : IComparable<TypeId>
{
	public int CompareTo(TypeId other)
	{
		return Value.CompareTo(other.Value);
	}
}
