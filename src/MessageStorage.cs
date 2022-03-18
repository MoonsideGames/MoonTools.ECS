namespace MoonTools.ECS;

internal abstract class MessageStorage
{
	public abstract void Clear();
}

internal class MessageStorage<TMessage> : MessageStorage where TMessage : struct
{
	private int count = 0;
	private int capacity = 128;
	private TMessage[] messages;
	private Dictionary<int, List<int>> entityToIndices = new Dictionary<int, List<int>>();

	public MessageStorage()
	{
		messages = new TMessage[capacity];
	}

	public void Add(in TMessage message)
	{
		if (count == capacity)
		{
			capacity *= 2;
			Array.Resize(ref messages, capacity);
		}

		messages[count] = message;

		if (message is IHasEntity entityMessage)
		{
			if (!entityToIndices.ContainsKey(entityMessage.Entity.ID))
			{
				entityToIndices.Add(entityMessage.Entity.ID, new List<int>());
			}

			entityToIndices[entityMessage.Entity.ID].Add(count);
		}

		count += 1;
	}

	public bool Some()
	{
		return count > 0;
	}

	public ReadOnlySpan<TMessage> All()
	{
		return new ReadOnlySpan<TMessage>(messages, 0, count);
	}

	public TMessage First()
	{
		return messages[0];
	}

	public IEnumerable<TMessage> WithEntity(int entityID)
	{
		if (entityToIndices.ContainsKey(entityID))
		{
			foreach (var index in entityToIndices[entityID])
			{
				yield return messages[index];
			}
		}
	}

	public ref readonly TMessage FirstWithEntity(int entityID)
	{
		return ref messages[entityToIndices[entityID][0]];
	}

	public bool SomeWithEntity(int entityID)
	{
		return entityToIndices.ContainsKey(entityID) && entityToIndices[entityID].Count > 0;
	}

	public override void Clear()
	{
		count = 0;
		foreach (var set in entityToIndices.Values)
		{
			set.Clear();
		}
	}
}
