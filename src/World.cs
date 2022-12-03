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

		/*
		internal readonly TemplateStorage TemplateStorage = new TemplateStorage();
		internal readonly ComponentDepot TemplateComponentDepot = new ComponentDepot();
		*/

		public World()
		{
			ComponentDepot = new ComponentDepot(ComponentTypeIndices);
			RelationDepot = new RelationDepot(RelationTypeIndices);
			FilterStorage = new FilterStorage(EntityStorage, ComponentTypeIndices);
		}

		public Entity CreateEntity()
		{
			return EntityStorage.Create();
		}

		public void Set<TComponent>(Entity entity, in TComponent component) where TComponent : unmanaged
		{
			if (EntityStorage.SetComponent(entity.ID, ComponentTypeIndices.GetIndex<TComponent>()))
			{
				FilterStorage.Check<TComponent>(entity.ID);
			}

			ComponentDepot.Set<TComponent>(entity.ID, component);
		}

		/*
		public Template CreateTemplate()
		{
			return TemplateStorage.Create();
		}

		public void Set<TComponent>(Template template, in TComponent component) where TComponent : unmanaged
		{
			TemplateComponentDepot.Set(template.ID, component);
		}
		*/

		public Entity Instantiate(Template template)
		{
			var entity = EntityStorage.Create();

			return entity;
		}

		public void Send<TMessage>(in TMessage message) where TMessage : unmanaged
		{
			MessageDepot.Add(message);
		}

		public void FinishUpdate()
		{
			MessageDepot.Clear();
		}

		public WorldState CreateState()
		{
			return new WorldState();
		}
	}
}
