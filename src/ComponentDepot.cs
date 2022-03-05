namespace MoonTools.ECS;

internal class ComponentDepot
{
	private Dictionary<Type, ComponentStorage> storages = new Dictionary<Type, ComponentStorage>();

	private Dictionary<int, HashSet<Type>> entityComponentMap = new Dictionary<int, HashSet<Type>>();

	private ComponentStorage<TComponent> Lookup<TComponent>() where TComponent : struct
	{
		// TODO: is it possible to optimize this?
		if (!storages.ContainsKey(typeof(TComponent)))
		{
			storages.Add(typeof(TComponent), new ComponentStorage<TComponent>());
		}

		return storages[typeof(TComponent)] as ComponentStorage<TComponent>;
	}

	public bool Some<TComponent>() where TComponent : struct
	{
		return Lookup<TComponent>().Any();
	}

	public bool Has<TComponent>(int entityID) where TComponent : struct
	{
		return Lookup<TComponent>().Has(entityID);
	}

	public ref readonly TComponent Get<TComponent>(int entityID) where TComponent : struct
	{
		return ref Lookup<TComponent>().Get(entityID);
	}

	public void Set<TComponent>(int entityID, in TComponent component) where TComponent : struct
	{
		Lookup<TComponent>().Set(entityID, component);

		if (!entityComponentMap.ContainsKey(entityID))
		{
			entityComponentMap.Add(entityID, new HashSet<Type>());
		}

		entityComponentMap[entityID].Add(typeof(TComponent));
	}

	public ReadOnlySpan<Entity> ReadEntities<TComponent>() where TComponent : struct
	{
		return Lookup<TComponent>().AllEntities();
	}

	public ReadOnlySpan<TComponent> ReadComponents<TComponent>() where TComponent : struct
	{
		return Lookup<TComponent>().AllComponents();
	}

	public void Remove<TComponent>(int entityID) where TComponent : struct
	{
		Lookup<TComponent>().Remove(entityID);

		entityComponentMap[entityID].Remove(typeof(TComponent));
	}

	public void OnEntityDestroy(int entityID)
	{
		if (entityComponentMap.ContainsKey(entityID))
		{
			foreach (var type in entityComponentMap[entityID])
			{
				storages[type].Remove(entityID);
			}

			entityComponentMap.Remove(entityID);
		}
	}
}
