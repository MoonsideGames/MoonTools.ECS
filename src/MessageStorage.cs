using System;
using MoonTools.ECS.Collections;

namespace MoonTools.ECS;

public class MessageStorage : IDisposable
{
	private NativeArray Messages;

	private bool IsDisposed;

	public MessageStorage(int elementSize)
	{
		Messages = new NativeArray(elementSize);
	}

	public void Add<T>(in T message) where T : unmanaged
	{
		Messages.Append(message);
	}

	public bool Some()
	{
		return Messages.Count > 0;
	}

	public ReadOnlySpan<T> All<T>() where T : unmanaged
	{
		return Messages.ToSpan<T>();
	}

	public T First<T>() where T : unmanaged
	{
		return Messages.Get<T>(0);
	}

	public void Clear()
	{
		Messages.Clear();
	}

	private void Dispose(bool disposing)
	{
		if (!IsDisposed)
		{
			Messages.Dispose();
			IsDisposed = true;
		}
	}

	// ~MessageStorage()
	// {
	// 	// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
	// 	Dispose(disposing: false);
	// }

	public void Dispose()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}
