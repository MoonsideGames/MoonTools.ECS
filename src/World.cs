using System;
using System.Collections.Generic;

namespace MoonTools.ECS
{
	public class World
	{
		internal readonly static TypeIndices ComponentTypeIndices = new TypeIndices();
		internal readonly static TypeIndices RelationTypeIndices = new TypeIndices();
		internal readonly EntityStorage EntityStorage = new EntityStorage();
		internal readonly ComponentDepot ComponentDepot;
		internal readonly MessageDepot MessageDepot = new MessageDepot();
		internal readonly RelationDepot RelationDepot;
		internal readonly FilterStorage FilterStorage;
		public FilterBuilder FilterBuilder => new FilterBuilder(FilterStorage, ComponentTypeIndices);

		public World()
		{
			ComponentDepot = new ComponentDepot(ComponentTypeIndices);
			RelationDepot = new RelationDepot(EntityStorage, RelationTypeIndices);
			FilterStorage = new FilterStorage(EntityStorage, ComponentTypeIndices);
		}

		public Entity CreateEntity(string tag = "")
		{
			return EntityStorage.Create(tag);
		}

		public void Tag(Entity entity, string tag)
		{
			EntityStorage.Tag(entity, tag);
		}

		public string GetTag(Entity entity)
		{
			return EntityStorage.Tag(entity);
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

		// untyped version for Transfer
		// no filter check because filter state is copied directly
		internal unsafe void Set(Entity entity, int componentTypeIndex, void* component)
		{
			ComponentDepot.Set(entity.ID, componentTypeIndex, component);
			EntityStorage.SetComponent(entity.ID, componentTypeIndex);
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

		public void Clear()
		{
			EntityStorage.Clear();
			MessageDepot.Clear();
			RelationDepot.Clear();
			ComponentDepot.Clear();
			FilterStorage.Clear();
		}

		private Dictionary<int, int> WorldToTransferID = new Dictionary<int, int>();

		/// <summary>
		/// If you are using the World Transfer feature, call this once after your systems/filters have all been initialized.
		/// </summary>
		public void PrepareTransferTo(World other)
		{
			other.FilterStorage.CreateMissingStorages(FilterStorage);
		}

		// FIXME: there's probably a better way to handle Filters so they are not world-bound
		public unsafe void Transfer(World other, Filter filter, Filter otherFilter)
		{
			WorldToTransferID.Clear();
			other.ComponentDepot.CreateMissingStorages(ComponentDepot);
			other.RelationDepot.CreateMissingStorages(RelationDepot);

			// destroy all entities matching the filter
			foreach (var entity in otherFilter.Entities)
			{
				other.Destroy(entity);
			}

			// create entities
			foreach (var entity in filter.Entities)
			{
				var otherWorldEntity = other.CreateEntity(GetTag(entity));
				WorldToTransferID.Add(entity.ID, otherWorldEntity.ID);
			}

			// transfer relations
			RelationDepot.TransferStorage(WorldToTransferID, other.RelationDepot);

			// set components
			foreach (var entity in filter.Entities)
			{
				var otherWorldEntity = WorldToTransferID[entity.ID];

				foreach (var componentTypeIndex in EntityStorage.ComponentTypeIndices(entity.ID))
				{
					other.Set(otherWorldEntity, componentTypeIndex, ComponentDepot.UntypedGet(entity.ID, componentTypeIndex));
				}
			}

			// transfer filters last so callbacks trigger correctly
			FilterStorage.TransferStorage(WorldToTransferID, other.FilterStorage);
		}
	}
}
