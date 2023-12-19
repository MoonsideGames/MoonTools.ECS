using System;
using MoonTools.ECS.Collections;

namespace MoonTools.ECS;

internal class IdAssigner : IDisposable
{
	int Next = 0;
	NativeArray<int> AvailableIds = new NativeArray<int>();

	private bool IsDisposed;

	public int Assign()
	{
		if (AvailableIds.TryPop(out var id))
		{
			return id;
		}

		id = Next;
		Next += 1;
		return id;
	}

	public void Unassign(int id)
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

	public void Dispose()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}
