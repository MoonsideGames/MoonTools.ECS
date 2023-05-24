using System;

namespace MoonTools.ECS
{
	public class World
	{
		internal readonly TypeIndices ComponentTypeIndices = new TypeIndices();
		internal readonly TypeIndices RelationTypeIndices = new TypeIndices();
		internal readonly EntityStorage EntityStorage = new EntityStorage();
		internal readonly ComponentDepot ComponentDepot;
		internal readonly MessageDepot MessageDepot = new MessageDepot();
		internal readonly RelationDepot RelationDepot;
		internal readonly FilterStorage FilterStorage;
		public FilterBuilder FilterBuilder => new FilterBuilder(FilterStorage, ComponentTypeIndices);

		internal readonly ComponentDepot TemplateComponentDepot;

		public World()
		{
			ComponentDepot = new ComponentDepot(ComponentTypeIndices);
			RelationDepot = new RelationDepot(RelationTypeIndices);
			FilterStorage = new FilterStorage(EntityStorage, ComponentTypeIndices);
			TemplateComponentDepot = new ComponentDepot(ComponentTypeIndices);
		}

		public Entity CreateEntity()
		{
			return EntityStorage.Create();
		}

		public void Set<TComponent>(Entity entity, in TComponent component) where TComponent : unmanaged
		{
#if DEBUG
			// check for use after destroy
			if (!EntityStorage.Exists(entity))
			{
				throw new InvalidOperationException("This entity is not valid!");
			}
#endif
			ComponentDepot.Set<TComponent>(entity.ID, component);

			if (EntityStorage.SetComponent(entity.ID, ComponentTypeIndices.GetIndex<TComponent>()))
			{
				FilterStorage.Check<TComponent>(entity.ID);
			}
		}

		public void Remove<TComponent>(in Entity entity) where TComponent : unmanaged
		{
			if (EntityStorage.RemoveComponent(entity.ID, ComponentTypeIndices.GetIndex<TComponent>()))
			{
				// Run filter storage update first so that the entity state is still valid in the remove callback.
				FilterStorage.Check<TComponent>(entity.ID);
				ComponentDepot.Remove<TComponent>(entity.ID);
			}
		}

		public void Relate<TRelationKind>(in Entity entityA, in Entity entityB, TRelationKind relationData) where TRelationKind : unmanaged
		{
			RelationDepot.Set(entityA, entityB, relationData);
			var relationTypeIndex = RelationTypeIndices.GetIndex<TRelationKind>();
			EntityStorage.AddRelationKind(entityA.ID, relationTypeIndex);
			EntityStorage.AddRelationKind(entityB.ID, relationTypeIndex);
		}

		public void Unrelate<TRelationKind>(in Entity entityA, in Entity entityB) where TRelationKind : unmanaged
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

		public void UnrelateAll<TRelationKind>(in Entity entity) where TRelationKind : unmanaged
		{
			RelationDepot.UnrelateAll<TRelationKind>(entity.ID);
			EntityStorage.RemoveRelation(entity.ID, RelationTypeIndices.GetIndex<TRelationKind>());
		}

		public void Send<TMessage>(in TMessage message) where TMessage : unmanaged
		{
			MessageDepot.Add(message);
		}

		public void Send<TMessage>(in Entity entity, in TMessage message) where TMessage : unmanaged
		{
			MessageDepot.Add(entity.ID, message);
		}

		public void Destroy(in Entity entity)
		{
			foreach (var componentTypeIndex in EntityStorage.ComponentTypeIndices(entity.ID))
			{
				// Run filter storage update first so that the entity state is still valid in the remove callback.
				FilterStorage.RemoveEntity(entity.ID, componentTypeIndex);
				ComponentDepot.Remove(entity.ID, componentTypeIndex);
			}

			foreach (var relationTypeIndex in EntityStorage.RelationTypeIndices(entity.ID))
			{
				RelationDepot.UnrelateAll(entity.ID, relationTypeIndex);
				EntityStorage.RemoveRelation(entity.ID, relationTypeIndex);
			}

			EntityStorage.Destroy(entity);
		}


		public void FinishUpdate()
		{
			MessageDepot.Clear();
		}

		public Snapshot CreateSnapshot()
		{
			return new Snapshot(this);
		}
	}
}
