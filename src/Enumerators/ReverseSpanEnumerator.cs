using System;
using System.Runtime.CompilerServices;

namespace MoonTools.ECS
{
	public ref struct ReverseSpanEnumerator<T>
	{
		private ReadOnlySpan<T> Span;
		private int index;

		public ReverseSpanEnumerator<T> GetEnumerator() => this;

		public T Current => Span[index];

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool MoveNext()
		{
			if (index > 0)
			{
				index -= 1;
				return true;
			}

			return false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ReverseSpanEnumerator(Span<T> span)
		{
			Span = span;
			index = span.Length;
		}

		public static ReverseSpanEnumerator<T> Empty => new ReverseSpanEnumerator<T>();
	}
}
