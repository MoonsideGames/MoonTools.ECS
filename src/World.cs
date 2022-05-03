namespace MoonTools.ECS
{
	public class World
	{
		internal readonly EntityStorage EntityStorage = new EntityStorage();
		internal readonly ComponentDepot ComponentDepot = new ComponentDepot();
		internal readonly MessageDepot MessageDepot = new MessageDepot();
		internal readonly RelationDepot RelationDepot = new RelationDepot();

		public Entity CreateEntity()
		{
			return EntityStorage.Create();
		}

		public void Set<TComponent>(Entity entity, in TComponent component) where TComponent : unmanaged
		{
			ComponentDepot.Set(entity.ID, component);
		}

		public void Send<TMessage>(in TMessage message) where TMessage : unmanaged
		{
			MessageDepot.Add(message);
		}

		public void FinishUpdate()
		{
			MessageDepot.Clear();
		}

		public void DisableSerialization<TComponent>() where TComponent : unmanaged
		{
			ComponentDepot.DisableSerialization<TComponent>();
		}

		public WorldState CreateState()
		{
			return new WorldState();
		}

		public void Save(WorldState state)
		{
			ComponentDepot.Save(state.ComponentDepotState);
			EntityStorage.Save(state.EntityStorageState);
			RelationDepot.Save(state.RelationDepotState);
		}

		public void Load(WorldState state)
		{
			ComponentDepot.Load(state.ComponentDepotState);
			EntityStorage.Load(state.EntityStorageState);
			RelationDepot.Load(state.RelationDepotState);
		}
	}
}
