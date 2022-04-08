using System;
using System.Collections.Generic;

namespace MoonTools.ECS
{
	public abstract class EntityComponentReader
	{
		internal EntityStorage EntityStorage;
		internal ComponentDepot ComponentDepot;
		internal RelationDepot RelationDepot;
		protected FilterBuilder FilterBuilder => new FilterBuilder(ComponentDepot);

		internal void RegisterEntityStorage(EntityStorage entityStorage)
		{
			EntityStorage = entityStorage;
		}

		internal void RegisterComponentDepot(ComponentDepot componentDepot)
		{
			ComponentDepot = componentDepot;
		}

		internal void RegisterRelationDepot(RelationDepot relationDepot)
		{
			RelationDepot = relationDepot;
		}

		protected ReadOnlySpan<TComponent> ReadComponents<TComponent>() where TComponent : struct
		{
			return ComponentDepot.ReadComponents<TComponent>();
		}

		protected bool Has<TComponent>(in Entity entity) where TComponent : struct
		{
			return ComponentDepot.Has<TComponent>(entity.ID);
		}

		protected bool Some<TComponent>() where TComponent : struct
		{
			return ComponentDepot.Some<TComponent>();
		}

		protected ref readonly TComponent Get<TComponent>(in Entity entity) where TComponent : struct
		{
			return ref ComponentDepot.Get<TComponent>(entity.ID);
		}

		protected ref readonly TComponent GetSingleton<TComponent>() where TComponent : struct
		{
			return ref ComponentDepot.Get<TComponent>();
		}

		protected Entity GetSingletonEntity<TComponent>() where TComponent : struct
		{
			return ComponentDepot.GetSingletonEntity<TComponent>();
		}

		protected bool Exists(in Entity entity)
		{
			return EntityStorage.Exists(entity);
		}

		protected IEnumerable<Relation> Relations<TRelationKind>()
		{
			return RelationDepot.Relations<TRelationKind>();
		}

		protected bool Related<TRelationKind>(in Entity a, in Entity b)
		{
			return RelationDepot.Related<TRelationKind>(a.ID, b.ID);
		}

		protected IEnumerable<Entity> RelatedToA<TRelationKind>(in Entity entity)
		{
			return RelationDepot.RelatedToA<TRelationKind>(entity.ID);
		}

		protected IEnumerable<Entity> RelatedToB<TRelationKind>(in Entity entity)
		{
			return RelationDepot.RelatedToB<TRelationKind>(entity.ID);
		}
	}
}
