using System;

namespace MoonTools.ECS.Rev2;

public readonly record struct Id : IComparable<Id>
{
	public readonly ulong Value;

	private const ulong HI = 0xFFFFFFFF00000000;
	private const ulong LO = 0xFFFFFFFF;

	public bool IsPair => (HI & Value) != 0;
	public uint Low => (uint)(LO & Value);
	public uint High => (uint)((HI & Value) >>> 32);

	public Id(ulong value)
	{
		Value = value;
	}

	public Id(uint relation, uint target)
	{
		Value = (relation << 31) | target;
	}

	public int CompareTo(Id other)
	{
		return Value.CompareTo(other.Value);
	}
}
