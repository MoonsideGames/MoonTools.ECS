namespace MoonTools.ECS;

internal abstract class ComponentStorage
{
	public abstract bool Has(int entityID);
	public abstract void Remove(int entityID);
}

internal class ComponentStorage<TComponent> : ComponentStorage where TComponent : struct
{
	private int nextID;
	private IDStorage idStorage = new IDStorage();
	private readonly Dictionary<int, int> entityIDToStorageIndex = new Dictionary<int, int>();
	private Entity[] storageIndexToEntities = new Entity[64];
	private TComponent[] components = new TComponent[64];

	public bool Any()
	{
		return nextID > 0;
	}

	public override bool Has(int entityID)
	{
		return entityIDToStorageIndex.ContainsKey(entityID);
	}

	public ref readonly TComponent Get(int entityID)
	{
		return ref components[entityIDToStorageIndex[entityID]];
	}

	public ref readonly TComponent Get()
	{
		#if DEBUG
		if (nextID == 0)
		{
			throw new ArgumentOutOfRangeException("Component storage is empty!");
		}
		#endif
		return ref components[0];
	}

	public void Set(int entityID, in TComponent component)
	{
		if (!entityIDToStorageIndex.ContainsKey(entityID))
		{
			var index = nextID;
			nextID += 1;

			if (index >= components.Length)
			{
				Array.Resize(ref components, components.Length * 2);
				Array.Resize(ref storageIndexToEntities, storageIndexToEntities.Length * 2);
			}

			entityIDToStorageIndex[entityID] = index;
			storageIndexToEntities[index] = new Entity(entityID);
		}

		components[entityIDToStorageIndex[entityID]] = component;
	}

	public override void Remove(int entityID)
	{
		if (entityIDToStorageIndex.ContainsKey(entityID))
		{
			var storageIndex = entityIDToStorageIndex[entityID];
			entityIDToStorageIndex.Remove(entityID);

			var lastElementIndex = nextID - 1;

			// move a component into the hole to maintain contiguous memory
			if (entityIDToStorageIndex.Count > 0 && storageIndex != lastElementIndex)
			{
				var lastEntity = storageIndexToEntities[lastElementIndex];

				entityIDToStorageIndex[lastEntity.ID] = storageIndex;
				storageIndexToEntities[storageIndex] = lastEntity;
				components[storageIndex] = components[lastElementIndex];
			}

			nextID -= 1;
		}
	}

	public void Clear()
	{
		nextID = 0;
		entityIDToStorageIndex.Clear();
	}

	public ReadOnlySpan<Entity> AllEntities()
	{
		return new ReadOnlySpan<Entity>(storageIndexToEntities, 0, nextID);
	}

	public ReadOnlySpan<TComponent> AllComponents()
	{
		return new ReadOnlySpan<TComponent>(components, 0, nextID);
	}

	public ref readonly Entity FirstEntity()
	{
		return ref storageIndexToEntities[0];
	}
}
