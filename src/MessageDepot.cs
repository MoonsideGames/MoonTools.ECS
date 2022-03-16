namespace MoonTools.ECS;

internal class MessageDepot
{
	private Dictionary<Type, MessageStorage> storages = new Dictionary<Type, MessageStorage>();

	private MessageStorage<TMessage> Lookup<TMessage>() where TMessage : struct
	{
		if (!storages.ContainsKey(typeof(TMessage)))
		{
			storages.Add(typeof(TMessage), new MessageStorage<TMessage>());
		}

		return storages[typeof(TMessage)] as MessageStorage<TMessage>;
	}

	public void Add<TMessage>(in TMessage message) where TMessage : struct
	{
		Lookup<TMessage>().Add(message);
	}

	public bool Some<TMessage>() where TMessage : struct
	{
		return Lookup<TMessage>().Some();
	}

	public ReadOnlySpan<TMessage> ReadAll<TMessage>() where TMessage : struct
	{
		return Lookup<TMessage>().All();
	}

	public TMessage ReadFirst<TMessage>() where TMessage : struct
	{
		return Lookup<TMessage>().First();
	}

	public void Clear()
	{
		foreach (var storage in storages.Values)
		{
			storage.Clear();
		}
	}
}
