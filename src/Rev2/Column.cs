using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MoonTools.ECS.Rev2
{
	public unsafe class Column
	{
		public nint Elements;
		public int ElementSize;
		public int Count;
		public int Capacity;

		public static Column Create<T>() where T : unmanaged
		{
			return new Column(Unsafe.SizeOf<T>());
		}

		private Column(int elementSize)
		{
			Capacity = 16;
			Count = 0;
			ElementSize = elementSize;

			Elements = (nint) NativeMemory.Alloc((nuint) (ElementSize * Capacity));
		}

		private void Resize()
		{
			Capacity *= 2;
			Elements = (nint) NativeMemory.Realloc((void*) Elements, (nuint) (ElementSize * Capacity));
		}

		public void Delete(int index)
		{
			if (Count > 1)
			{
				NativeMemory.Copy(
					(void*) (Elements + ((Count - 1) * ElementSize)),
					(void*) (Elements + (index * ElementSize)),
					(nuint) ElementSize
				);
			}

			Count -= 1;
		}

		public void CopyToEnd(int index, Column other)
		{
			if (other.Count >= other.Capacity)
			{
				other.Resize();
			}

			NativeMemory.Copy(
				(void*) (Elements + (index * ElementSize)),
				(void*) (other.Elements + (other.Count * ElementSize)),
				(nuint) ElementSize
			);

			other.Count += 1;
		}
	}
}
