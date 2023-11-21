using System;
using System.Runtime.CompilerServices;

namespace MoonTools.ECS;

public ref struct ReverseSpanEnumerator<T>
{
	private ReadOnlySpan<T> Span;
	private int Index;

	public ReverseSpanEnumerator<T> GetEnumerator() => this;

	public T Current => Span[Index];

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool MoveNext()
	{
		if (Index > 0)
		{
			Index -= 1;
			return true;
		}

		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ReverseSpanEnumerator(Span<T> span)
	{
		Span = span;
		Index = span.Length;
	}

	public static ReverseSpanEnumerator<T> Empty => new ReverseSpanEnumerator<T>();
}
