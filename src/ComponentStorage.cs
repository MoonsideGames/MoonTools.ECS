namespace MoonTools.ECS;

internal abstract class ComponentStorage
{
	public abstract bool Has(int entityID);
	public abstract void Remove(int entityID);
	public abstract object Debug_Get(int entityID);
}

// FIXME: we can probably get rid of this weird entity storage system by using filters
internal class ComponentStorage<TComponent> : ComponentStorage where TComponent : struct
{
	private int nextID;
	private readonly Dictionary<int, int> entityIDToStorageIndex = new Dictionary<int, int>(16);
	private int[] entityIDs = new int[16];
	private TComponent[] components = new TComponent[16];

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

	public override object Debug_Get(int entityID)
	{
		return components[entityIDToStorageIndex[entityID]];
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
				Array.Resize(ref entityIDs, entityIDs.Length * 2);
			}

			entityIDToStorageIndex[entityID] = index;
			entityIDs[index] = entityID;
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
			if (lastElementIndex != storageIndex)
			{
				var lastEntityID = entityIDs[lastElementIndex];
				entityIDToStorageIndex[lastEntityID] = storageIndex;
				components[storageIndex] = components[lastElementIndex];
				entityIDs[storageIndex] = lastEntityID;
			}

			nextID -= 1;
		}
	}

	public void Clear()
	{
		nextID = 0;
		entityIDToStorageIndex.Clear();
	}

	public ReadOnlySpan<TComponent> AllComponents()
	{
		return new ReadOnlySpan<TComponent>(components, 0, nextID);
	}

	public Entity FirstEntity()
	{
		return new Entity(entityIDs[0]);
	}
}
