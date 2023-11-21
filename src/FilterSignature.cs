using System;
using MoonTools.ECS.Collections;

namespace MoonTools.ECS;

public struct FilterSignature : IEquatable<FilterSignature>
{
	public readonly IndexableSet<TypeId> Included;
	public readonly IndexableSet<TypeId> Excluded;

	public FilterSignature(IndexableSet<TypeId> included, IndexableSet<TypeId> excluded)
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
		foreach (var included in Included)
		{
			if (!other.Included.Contains(included))
			{
				return false;
			}
		}

		foreach (var excluded in Excluded)
		{
			if (!other.Excluded.Contains(excluded))
			{
				return false;
			}
		}

		return true;
	}

	public override int GetHashCode()
	{
		var hashcode = 1;

		foreach (var type in Included)
		{
			hashcode = HashCode.Combine(hashcode, type);
		}

		foreach (var type in Excluded)
		{
			hashcode = HashCode.Combine(hashcode, type);
		}

		return hashcode;
	}

	public static bool operator ==(FilterSignature left, FilterSignature right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(FilterSignature left, FilterSignature right)
	{
		return !(left == right);
	}
}
