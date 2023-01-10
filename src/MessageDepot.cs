using System;
using System.Collections.Generic;

namespace MoonTools.ECS
{
	internal class MessageDepot
	{
		private Dictionary<Type, MessageStorage> storages = new Dictionary<Type, MessageStorage>();

		private MessageStorage<TMessage> Lookup<TMessage>() where TMessage : unmanaged
		{
			if (!storages.ContainsKey(typeof(TMessage)))
			{
				storages.Add(typeof(TMessage), new MessageStorage<TMessage>());
			}

			return storages[typeof(TMessage)] as MessageStorage<TMessage>;
		}

		public void Add<TMessage>(in TMessage message) where TMessage : unmanaged
		{
			Lookup<TMessage>().Add(message);
		}

		public void Add<TMessage>(int entityID, in TMessage message) where TMessage : unmanaged
		{
			Lookup<TMessage>().Add(entityID, message);
		}

		public bool Some<TMessage>() where TMessage : unmanaged
		{
			return Lookup<TMessage>().Some();
		}

		public ReadOnlySpan<TMessage> All<TMessage>() where TMessage : unmanaged
		{
			return Lookup<TMessage>().All();
		}

		public TMessage First<TMessage>() where TMessage : unmanaged
		{
			return Lookup<TMessage>().First();
		}

		public ReverseSpanEnumerator<TMessage> WithEntity<TMessage>(int entityID) where TMessage : unmanaged
		{
			return Lookup<TMessage>().WithEntity(entityID);
		}

		public ref readonly TMessage FirstWithEntity<TMessage>(int entityID) where TMessage : unmanaged
		{
			return ref Lookup<TMessage>().FirstWithEntity(entityID);
		}

		public bool SomeWithEntity<TMessage>(int entityID) where TMessage : unmanaged
		{
			return Lookup<TMessage>().SomeWithEntity(entityID);
		}

		public void Clear()
		{
			foreach (var storage in storages.Values)
			{
				storage.Clear();
			}
		}
	}
}
