namespace MoonTools.ECS
{
    public class WorldState
    {
		internal readonly ComponentDepotState ComponentDepotState;
		internal readonly EntityStorageState EntityStorageState;
		internal readonly RelationDepotState RelationDepotState;

		public WorldState()
        {
			ComponentDepotState = new ComponentDepotState();
			EntityStorageState = new EntityStorageState();
			RelationDepotState = new RelationDepotState();
		}
	}
}
