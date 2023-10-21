using System;
using System.Runtime.InteropServices;

namespace MoonTools.ECS.Rev2
{
	public unsafe class Column : IDisposable
	{
		public nint Elements;
		public int ElementSize;
		public int Count;
		public int Capacity;

		private bool IsDisposed;

		public Column(int elementSize)
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

		// Fills gap by copying final element to the deleted index
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

		public void Append<T>(T component) where T : unmanaged
		{
			if (Count >= Capacity)
			{
				Resize();
			}

			((T*) Elements)[Count] = component;
			Count += 1;
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

		protected virtual void Dispose(bool disposing)
		{
			if (!IsDisposed)
			{
				NativeMemory.Free((void*) Elements);
				IsDisposed = true;
			}
		}

		~Column()
		{
		    // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		    Dispose(disposing: false);
		}

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}
