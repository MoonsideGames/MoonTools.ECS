﻿using System;
using System.Collections.Generic;

namespace MoonTools.ECS
{
	public abstract class EntityComponentReader
	{
		internal readonly World World;
		internal EntityStorage EntityStorage => World.EntityStorage;
		internal ComponentDepot ComponentDepot => World.ComponentDepot;
		internal RelationDepot RelationDepot => World.RelationDepot;
		protected FilterBuilder FilterBuilder => new FilterBuilder(FilterStorage, ComponentTypeIndices, RelationTypeIndices);
		internal FilterStorage FilterStorage => World.FilterStorage;
		internal TypeIndices ComponentTypeIndices => World.ComponentTypeIndices;
		internal TypeIndices RelationTypeIndices => World.RelationTypeIndices;
		internal TemplateStorage TemplateStorage => World.TemplateStorage;
		internal ComponentDepot TemplateComponentDepot => World.TemplateComponentDepot;

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
			var storageIndex = ComponentTypeIndices.GetIndex<TComponent>();
			return EntityStorage.HasComponent(entity.ID, storageIndex);
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
			return ref ComponentDepot.GetFirst<TComponent>();
		}

		protected Entity GetSingletonEntity<TComponent>() where TComponent : unmanaged
		{
			return ComponentDepot.GetSingletonEntity<TComponent>();
		}

		protected ReverseSpanEnumerator<(Entity, Entity)> Relations<TRelationKind>() where TRelationKind : unmanaged
		{
			return RelationDepot.Relations<TRelationKind>();
		}

		protected bool Related<TRelationKind>(in Entity a, in Entity b) where TRelationKind : unmanaged
		{
			return RelationDepot.Related<TRelationKind>(a.ID, b.ID);
		}

		protected TRelationKind GetRelationData<TRelationKind>(in Entity a, in Entity b) where TRelationKind : unmanaged
		{
			return RelationDepot.Get<TRelationKind>(a, b);
		}

		// relations go A->B, so given A, will give all entities in outgoing relations of this kind.
		protected ReverseSpanEnumerator<Entity> OutRelations<TRelationKind>(in Entity entity) where TRelationKind : unmanaged
		{
			return RelationDepot.OutRelations<TRelationKind>(entity.ID);
		}

		protected Entity OutRelationSingleton<TRelationKind>(in Entity entity) where TRelationKind : unmanaged
		{
			return RelationDepot.OutRelationSingleton<TRelationKind>(entity.ID);
		}

		protected bool HasOutRelation<TRelationKind>(in Entity entity) where TRelationKind : unmanaged
		{
			return RelationDepot.HasOutRelation<TRelationKind>(entity.ID);
		}

		protected int OutRelationCount<TRelationKind>(in Entity entity) where TRelationKind : unmanaged
		{
			return RelationDepot.OutRelationCount<TRelationKind>(entity.ID);
		}

		// Relations go A->B, so given B, will give all entities in incoming A relations of this kind.
		protected ReverseSpanEnumerator<Entity> InRelations<TRelationKind>(in Entity entity) where TRelationKind : unmanaged
		{
			return RelationDepot.InRelations<TRelationKind>(entity.ID);
		}

		protected Entity InRelationSingleton<TRelationKind>(in Entity entity) where TRelationKind : unmanaged
		{
			return RelationDepot.InRelationSingleton<TRelationKind>(entity.ID);
		}

		protected bool HasInRelation<TRelationKind>(in Entity entity) where TRelationKind : unmanaged
		{
			return RelationDepot.HasInRelation<TRelationKind>(entity.ID);
		}

		protected int InRelationCount<TRelationKind>(in Entity entity) where TRelationKind : unmanaged
		{
			return RelationDepot.InRelationCount<TRelationKind>(entity.ID);
		}
	}
}
