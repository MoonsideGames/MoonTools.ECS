using System;
using System.Collections.Generic;

namespace MoonTools.ECS
{
	public abstract class EntityComponentReader
	{
		internal readonly World World;
		internal EntityStorage EntityStorage => World.EntityStorage;
		internal ComponentDepot ComponentDepot => World.ComponentDepot;
		internal RelationDepot RelationDepot => World.RelationDepot;
		protected FilterBuilder FilterBuilder => new FilterBuilder(ComponentDepot);

		public EntityComponentReader(World world)
		{
			World = world;
		}

		protected ReadOnlySpan<TComponent> ReadComponents<TComponent>() where TComponent : unmanaged
		{
			return ComponentDepot.ReadComponents<TComponent>();
		}

		protected bool Has<TComponent>(in Entity entity) where TComponent : unmanaged
		{
			return ComponentDepot.Has<TComponent>(entity.ID);
		}

		protected bool Some<TComponent>() where TComponent : unmanaged
		{
			return ComponentDepot.Some<TComponent>();
		}

		protected ref readonly TComponent Get<TComponent>(in Entity entity) where TComponent : unmanaged
		{
			return ref ComponentDepot.Get<TComponent>(entity.ID);
		}

		protected ref readonly TComponent GetSingleton<TComponent>() where TComponent : unmanaged
		{
			return ref ComponentDepot.Get<TComponent>();
		}

		protected Entity GetSingletonEntity<TComponent>() where TComponent : unmanaged
		{
			return ComponentDepot.GetSingletonEntity<TComponent>();
		}

		protected bool Exists(in Entity entity)
		{
			return EntityStorage.Exists(entity);
		}

		protected IEnumerable<(Entity, Entity, TRelationKind)> Relations<TRelationKind>() where TRelationKind : unmanaged
		{
			return RelationDepot.Relations<TRelationKind>();
		}

		protected bool Related<TRelationKind>(in Entity a, in Entity b) where TRelationKind : unmanaged
		{
			return RelationDepot.Related<TRelationKind>(a.ID, b.ID);
		}

		protected IEnumerable<(Entity, TRelationKind)> RelatedToA<TRelationKind>(in Entity entity) where TRelationKind : unmanaged
		{
			return RelationDepot.RelatedToA<TRelationKind>(entity.ID);
		}

		protected IEnumerable<(Entity, TRelationKind)> RelatedToB<TRelationKind>(in Entity entity) where TRelationKind : unmanaged
		{
			return RelationDepot.RelatedToB<TRelationKind>(entity.ID);
		}
	}
}
