using System;
using System.Collections.Generic;

namespace MoonTools.ECS
{
	public abstract class System : EntityComponentReader
	{
		internal MessageDepot MessageDepot => World.MessageDepot;

		public System(World world) : base(world) { }

		public abstract void Update(TimeSpan delta);

		protected Entity CreateEntity()
		{
			return EntityStorage.Create();
		}

		protected void Set<TComponent>(in Entity entity, in TComponent component) where TComponent : unmanaged
		{
#if DEBUG
			// check for use after destroy
			if (!Exists(entity))
			{
				throw new ArgumentException("This entity is not valid!");
			}
#endif
			ComponentDepot.Set<TComponent>(entity.ID, component);
		}

		protected void Remove<TComponent>(in Entity entity) where TComponent : unmanaged
		{
			ComponentDepot.Remove<TComponent>(entity.ID);
		}

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

		protected IEnumerable<TMessage> ReadMessagesWithEntity<TMessage>(in Entity entity) where TMessage : unmanaged, IHasEntity
		{
			return MessageDepot.WithEntity<TMessage>(entity.ID);
		}

		protected ref readonly TMessage ReadMessageWithEntity<TMessage>(in Entity entity) where TMessage : unmanaged, IHasEntity
		{
			return ref MessageDepot.FirstWithEntity<TMessage>(entity.ID);
		}

		protected bool SomeMessageWithEntity<TMessage>(in Entity entity) where TMessage : unmanaged, IHasEntity
		{
			return MessageDepot.SomeWithEntity<TMessage>(entity.ID);
		}

		protected void Send<TMessage>(in TMessage message) where TMessage : unmanaged
		{
			MessageDepot.Add(message);
		}

		protected void Relate<TRelationKind>(in Entity entityA, in Entity entityB, TRelationKind relationData) where TRelationKind : unmanaged
		{
			RelationDepot.Set<TRelationKind>(new Relation(entityA, entityB), relationData);
		}

		protected void Unrelate<TRelationKind>(in Entity entityA, in Entity entityB) where TRelationKind : unmanaged
		{
			RelationDepot.Remove<TRelationKind>(new Relation(entityA, entityB));
		}

		protected void UnrelateAll<TRelationKind>(in Entity entity) where TRelationKind : unmanaged
		{
			RelationDepot.UnrelateAll<TRelationKind>(entity.ID);
		}

		// FIXME: this is insanely inefficient
		protected void Destroy(in Entity entity)
		{
			ComponentDepot.OnEntityDestroy(entity.ID);
			RelationDepot.OnEntityDestroy(entity.ID);
			EntityStorage.Destroy(entity);
		}
	}
}
