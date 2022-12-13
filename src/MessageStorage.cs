using System;
using System.Collections.Generic;

namespace MoonTools.ECS
{
	internal abstract class MessageStorage
	{
		public abstract void Clear();
	}

	internal class MessageStorage<TMessage> : MessageStorage where TMessage : unmanaged
	{
		private int count = 0;
		private int capacity = 128;
		private TMessage[] messages;
		// duplicating storage here for fast iteration
		private Dictionary<int, DynamicArray<TMessage>> entityToMessages = new Dictionary<int, DynamicArray<TMessage>>();

		public MessageStorage()
		{
			messages = new TMessage[capacity];
		}

		public void Add(in TMessage message)
		{
			if (count == capacity)
			{
				capacity *= 2;
				Array.Resize(ref messages, capacity);
			}

			messages[count] = message;
			count += 1;
		}

		public void Add(int entityID, in TMessage message)
		{
			if (!entityToMessages.ContainsKey(entityID))
			{
				entityToMessages.Add(entityID, new DynamicArray<TMessage>());
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
			return new ReadOnlySpan<TMessage>(messages, 0, count);
		}

		public TMessage First()
		{
			return messages[0];
		}

		public ReverseSpanEnumerator<TMessage> WithEntity(int entityID)
		{
			if (entityToMessages.TryGetValue(entityID, out var messages))
			{
				return messages.GetEnumerator();
			}
			else
			{
				return ReverseSpanEnumerator<TMessage>.Empty;
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
	}
}
