using System;
using MoonTools.ECS.Collections;

namespace MoonTools.ECS;

internal class IdAssigner : IDisposable
{
	uint Next;
	NativeArray<uint> AvailableIds = new NativeArray<uint>();

	private bool IsDisposed;

	public uint Assign()
	{
		if (AvailableIds.TryPop(out var id))
		{
			return id;
		}

		id = Next;
		Next += 1;
		return id;
	}

	public void Unassign(uint id)
	{
		AvailableIds.Append(id);
	}

	public void CopyTo(IdAssigner other)
	{
		AvailableIds.CopyTo(other.AvailableIds);
		other.Next = Next;
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!IsDisposed)
		{
			if (disposing)
			{
				AvailableIds.Dispose();
			}

			IsDisposed = true;
		}
	}

	// // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
	// ~IdAssigner()
	// {
	//     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
	//     Dispose(disposing: false);
	// }

	public void Dispose()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}
