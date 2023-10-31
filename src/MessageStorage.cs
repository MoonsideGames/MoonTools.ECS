using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MoonTools.ECS.Collections;

namespace MoonTools.ECS
{
	internal abstract class MessageStorage
	{
		public abstract void Clear();
	}

	internal unsafe class MessageStorage<TMessage> : MessageStorage, IDisposable where TMessage : unmanaged
	{
		private int count = 0;
		private int capacity = 128;
		private TMessage* messages;
		// duplicating storage here for fast iteration
		private Dictionary<int, NativeArray<TMessage>> entityToMessages = new Dictionary<int, NativeArray<TMessage>>();
		private bool disposed;

		public MessageStorage()
		{
			messages = (TMessage*) NativeMemory.Alloc((nuint) (capacity * Unsafe.SizeOf<TMessage>()));
		}

		public void Add(in TMessage message)
		{
			if (count == capacity)
			{
				capacity *= 2;
				messages = (TMessage*) NativeMemory.Realloc(messages, (nuint) (capacity * Unsafe.SizeOf<TMessage>()));
			}

			messages[count] = message;
			count += 1;
		}

		public void Add(int entityID, in TMessage message)
		{
			if (!entityToMessages.ContainsKey(entityID))
			{
				entityToMessages.Add(entityID, new NativeArray<TMessage>());
			}
			entityToMessages[entityID].Add(message);

			Add(message);
		}

		public bool Some()
		{
			return count > 0;
		}

		public ReadOnlySpan<TMessage> All()
		{
			return new ReadOnlySpan<TMessage>(messages, count);
		}

		public TMessage First()
		{
			return messages[0];
		}

		public Span<TMessage>.Enumerator WithEntity(int entityID)
		{
			if (entityToMessages.TryGetValue(entityID, out var messages))
			{
				return messages.GetEnumerator();
			}
			else
			{
				return Span<TMessage>.Empty.GetEnumerator();
			}
		}

		public ref readonly TMessage FirstWithEntity(int entityID)
		{
			return ref entityToMessages[entityID][0];
		}

		public bool SomeWithEntity(int entityID)
		{
			return entityToMessages.ContainsKey(entityID) && entityToMessages[entityID].Count > 0;
		}

		public override void Clear()
		{
			count = 0;
			foreach (var set in entityToMessages.Values)
			{
				set.Clear();
			}
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposed)
			{
				Clear();

				if (disposing)
				{
					foreach (var array in entityToMessages.Values)
					{
						array.Dispose();
					}
				}

				NativeMemory.Free(messages);
				messages = null;

				disposed = true;
			}
		}

		~MessageStorage()
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
