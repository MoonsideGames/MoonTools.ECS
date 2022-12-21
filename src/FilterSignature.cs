using System;
using System.Collections.Generic;

namespace MoonTools.ECS
{
	internal struct FilterSignature : IEquatable<FilterSignature>
	{
		public readonly HashSet<int> Included;
		public readonly HashSet<int> Excluded;
		public readonly HashSet<int> InRelations;
		public readonly HashSet<int> OutRelations;

		public FilterSignature(
			HashSet<int> included,
			HashSet<int> excluded,
			HashSet<int> inRelations,
			HashSet<int> outRelations
		) {
			Included = included;
			Excluded = excluded;
			InRelations = inRelations;
			OutRelations = outRelations;
		}

		public override bool Equals(object? obj)
		{
			return obj is FilterSignature signature && Equals(signature);
		}

		public bool Equals(FilterSignature other)
		{
			return
				Included.SetEquals(other.Included) &&
				Excluded.SetEquals(other.Excluded) &&
				InRelations.SetEquals(other.InRelations) &&
				OutRelations.SetEquals(other.OutRelations);
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

			foreach (var type in InRelations)
			{
				hashcode = HashCode.Combine(hashcode, type);
			}

			foreach (var type in OutRelations)
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
