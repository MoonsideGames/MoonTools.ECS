using System;

namespace MoonTools.ECS
{
	public abstract class System : Manipulator
	{
		internal MessageDepot MessageDepot => World.MessageDepot;

		public System(World world) : base(world) { }

		public abstract void Update(TimeSpan delta);

		protected ReadOnlySpan<TMessage> ReadMessages<TMessage>() where TMessage : unmanaged
		{
			return MessageDepot.All<TMessage>();
		}

		protected TMessage ReadMessage<TMessage>() where TMessage : unmanaged
		{
			return MessageDepot.First<TMessage>();
		}

		protected bool SomeMessage<TMessage>() where TMessage : unmanaged
		{
			return MessageDepot.Some<TMessage>();
		}

		protected ReverseSpanEnumerator<TMessage> ReadMessagesWithEntity<TMessage>(in Entity entity) where TMessage : unmanaged
		{
			return MessageDepot.WithEntity<TMessage>(entity.ID);
		}

		protected ref readonly TMessage ReadMessageWithEntity<TMessage>(in Entity entity) where TMessage : unmanaged
		{
			return ref MessageDepot.FirstWithEntity<TMessage>(entity.ID);
		}

		protected bool SomeMessageWithEntity<TMessage>(in Entity entity) where TMessage : unmanaged
		{
			return MessageDepot.SomeWithEntity<TMessage>(entity.ID);
		}

		protected void Send<TMessage>(in TMessage message) where TMessage : unmanaged => World.Send(message);

		protected void Send<TMessage>(in Entity entity, in TMessage message) where TMessage : unmanaged => World.Send(entity, message);
	}
}
