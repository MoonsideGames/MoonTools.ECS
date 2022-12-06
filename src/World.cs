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
			if (EntityStorage.SetComponent(entity.ID, ComponentTypeIndices.GetIndex<TComponent>()))
			{
				FilterStorage.Check<TComponent>(entity.ID);
			}

			ComponentDepot.Set<TComponent>(entity.ID, component);
		}

		public Template CreateTemplate()
		{
			return TemplateStorage.Create();
		}

		public void Set<TComponent>(in Template template, in TComponent component) where TComponent : unmanaged
		{
			TemplateStorage.SetComponent(template.ID, ComponentTypeIndices.GetIndex<TComponent>());
			TemplateComponentDepot.Set(template.ID, component);
		}

		// TODO: TEST ME!!!
		public Entity Instantiate(in Template template)
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
