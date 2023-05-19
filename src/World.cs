﻿using System;

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

		internal readonly TemplateStorage TemplateStorage = new TemplateStorage();
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

		public Template CreateTemplate()
		{
			return TemplateStorage.Create();
		}

		public void Set<TComponent>(in Template template, in TComponent component) where TComponent : unmanaged
		{
			var componentTypeIndex = ComponentTypeIndices.GetIndex<TComponent>();
			TemplateStorage.SetComponent(template.ID, componentTypeIndex);
			TemplateComponentDepot.Set(template.ID, component);
			ComponentDepot.Register<TComponent>(componentTypeIndex);
		}

		public unsafe Entity Instantiate(in Template template)
		{
			var entity = EntityStorage.Create();

			foreach (var componentTypeIndex in TemplateStorage.ComponentTypeIndices(template.ID))
			{
				EntityStorage.SetComponent(entity.ID, componentTypeIndex);
				FilterStorage.Check(entity.ID, componentTypeIndex);
				ComponentDepot.Set(entity.ID, componentTypeIndex, TemplateComponentDepot.UntypedGet(template.ID, componentTypeIndex));
			}

			return entity;
		}

		public void Send<TMessage>(in TMessage message) where TMessage : unmanaged
		{
			MessageDepot.Add(message);
		}

		public void Relate<TRelationKind>(in Entity entityA, in Entity entityB, TRelationKind relationData) where TRelationKind : unmanaged
		{
			RelationDepot.Set(entityA, entityB, relationData);
			var relationTypeIndex = RelationTypeIndices.GetIndex<TRelationKind>();
			EntityStorage.AddRelationKind(entityA.ID, relationTypeIndex);
			EntityStorage.AddRelationKind(entityB.ID, relationTypeIndex);
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
