using System;
using System.Runtime.CompilerServices;

namespace MoonTools.ECS;

public readonly record struct TypeId(uint Value) : IComparable<TypeId>
{
	public int CompareTo(TypeId other)
	{
		return Value.CompareTo(other.Value);
	}

	public static implicit operator int(TypeId typeId)
	{
		return (int) typeId.Value;
	}
}

public class ComponentTypeIdAssigner
{
	protected static ushort Counter;
}

public class ComponentTypeIdAssigner<T> : ComponentTypeIdAssigner
{
	public static readonly ushort Id;
	public static readonly int Size;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static ComponentTypeIdAssigner()
	{
		Id = Counter++;
		Size = Unsafe.SizeOf<T>();

		World.ComponentTypeElementSizes.Add(Size);

		#if DEBUG
		World.ComponentTypeToId[typeof(T)] = new TypeId(Id);
		World.ComponentTypeIdToType.Add(typeof(T));
		#endif
	}
}

public class RelationTypeIdAssigner
{
	protected static ushort Counter;
}

public class RelationTypeIdAssigner<T> : RelationTypeIdAssigner
{
	public static readonly ushort Id;
	public static readonly int Size;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static RelationTypeIdAssigner()
	{
		Id = Counter++;
		Size = Unsafe.SizeOf<T>();

		World.RelationTypeElementSizes.Add(Size);
	}
}

public class MessageTypeIdAssigner
{
	protected static ushort Counter;
}

public class MessageTypeIdAssigner<T> : MessageTypeIdAssigner
{
	public static readonly ushort Id;
	public static readonly int Size;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static MessageTypeIdAssigner()
	{
		Id = Counter++;
		Size = Unsafe.SizeOf<T>();

		World.MessageTypeElementSizes.Add(Size);
	}
}
