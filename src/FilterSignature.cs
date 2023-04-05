using System;
using System.Collections.Generic;

namespace MoonTools.ECS
{
	public struct FilterSignature : IEquatable<FilterSignature>
	{
		public readonly HashSet<int> Included;
		public readonly HashSet<int> Excluded;

		public FilterSignature(HashSet<int> included, HashSet<int> excluded)
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
			// workaround for HashSet<T>.SetEquals generating garbage
			// maybe fixed in .NET 8?
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
}
