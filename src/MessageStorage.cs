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

	public override void Clear()
	{
		count = 0;
	}
}
