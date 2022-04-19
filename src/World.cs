namespace MoonTools.ECS
{
	public class World
	{
		private readonly EntityStorage EntityStorage = new EntityStorage();
		private readonly ComponentDepot ComponentDepot = new ComponentDepot();
		private readonly MessageDepot MessageDepot = new MessageDepot();
		private readonly RelationDepot RelationDepot = new RelationDepot();

		internal void AddSystem(System system)
		{
			system.RegisterEntityStorage(EntityStorage);
			system.RegisterComponentDepot(ComponentDepot);
			system.RegisterMessageDepot(MessageDepot);
			system.RegisterRelationDepot(RelationDepot);
		}

		internal void AddRenderer(Renderer renderer)
		{
			renderer.RegisterEntityStorage(EntityStorage);
			renderer.RegisterComponentDepot(ComponentDepot);
			renderer.RegisterRelationDepot(RelationDepot);
		}

		public Entity CreateEntity()
		{
			return EntityStorage.Create();
		}

		public void Set<TComponent>(Entity entity, in TComponent component) where TComponent : struct
		{
			ComponentDepot.Set(entity.ID, component);
		}

		public void Send<TMessage>(in TMessage message) where TMessage : struct
		{
			MessageDepot.Add(message);
		}

		public void FinishUpdate()
		{
			MessageDepot.Clear();
		}
	}
}
