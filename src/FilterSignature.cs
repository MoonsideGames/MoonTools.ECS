namespace MoonTools.ECS;

public struct FilterSignature
{
	private const int HASH_FACTOR = 97;

	public HashSet<Type> Included;
	public HashSet<Type> Excluded;

	public FilterSignature(HashSet<Type> included, HashSet<Type> excluded)
	{
		Included = included;
		Excluded = excluded;
	}

	public override bool Equals(object? obj)
	{
		return obj is FilterSignature signature && Equals(signature);
	}

	public bool Equals(FilterSignature other)
	{
		return Included.SetEquals(other.Included) && Excluded.SetEquals(other.Excluded);
	}

	private int GuidToInt(Guid guid)
	{
		return BitConverter.ToInt32(guid.ToByteArray());
	}

	public override int GetHashCode()
	{
		int result = 1;
		foreach (var type in Included)
		{
			result *= HASH_FACTOR + GuidToInt(type.GUID);
		}

		// FIXME: Is there a way to avoid collisions when this is the same set as included?
		foreach (var type in Excluded)
		{
			result *= HASH_FACTOR + GuidToInt(type.GUID);
		}

		return result;
	}
}
