using System;
using System.Collections.Generic;

namespace MoonTools.ECS
{
	public abstract class System : EntityComponentReader
	{
		internal MessageDepot MessageDepot => World.MessageDepot;

		public System(World world) : base(world) { }

		public abstract void Update(TimeSpan delta);

		protected Entity CreateEntity() => World.CreateEntity();

		protected void Set<TComponent>(in Entity entity, in TComponent component) where TComponent : unmanaged => World.Set<TComponent>(entity, component);

		protected void Remove<TComponent>(in Entity entity) where TComponent : unmanaged
		{
			if (EntityStorage.RemoveComponent(entity.ID, ComponentTypeIndices.GetIndex<TComponent>()))
			{
				ComponentDepot.Remove<TComponent>(entity.ID);
				FilterStorage.Check<TComponent>(entity.ID);
			}
		}

		protected void Set<TComponent>(in Template template, in TComponent component) where TComponent : unmanaged => World.Set<TComponent>(template, component);

		// This feature is EXPERIMENTAL. USe at your own risk!!
		protected Entity Instantiate(in Template template) => World.Instantiate(template);

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

		protected void Send<TMessage>(in TMessage message) where TMessage : unmanaged
		{
			MessageDepot.Add(message);
		}

		protected void Send<TMessage>(in Entity entity, in TMessage message) where TMessage : unmanaged
		{
			MessageDepot.Add(entity.ID, message);
		}

		protected void Relate<TRelationKind>(in Entity entityA, in Entity entityB, TRelationKind relationData) where TRelationKind : unmanaged
		{
			RelationDepot.Set(entityA, entityB, relationData);
			var relationTypeIndex = RelationTypeIndices.GetIndex<TRelationKind>();
			EntityStorage.AddRelationKind(entityA.ID, relationTypeIndex);
			EntityStorage.AddRelationKind(entityB.ID, relationTypeIndex);
		}

		protected void Unrelate<TRelationKind>(in Entity entityA, in Entity entityB) where TRelationKind : unmanaged
		{
			var (aEmpty, bEmpty) = RelationDepot.Remove<TRelationKind>(entityA, entityB);

			if (aEmpty)
			{
				EntityStorage.RemoveRelation(entityA.ID, RelationTypeIndices.GetIndex<TRelationKind>());
			}

			if (bEmpty)
			{
				EntityStorage.RemoveRelation(entityB.ID, RelationTypeIndices.GetIndex<TRelationKind>());
			}
		}

		protected void UnrelateAll<TRelationKind>(in Entity entity) where TRelationKind : unmanaged
		{
			RelationDepot.UnrelateAll<TRelationKind>(entity.ID);
			EntityStorage.RemoveRelation(entity.ID, RelationTypeIndices.GetIndex<TRelationKind>());
		}

		protected void Destroy(in Entity entity) => World.Destroy(entity);
	}
}
