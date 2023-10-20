using System;
using System.Collections.Generic;

namespace MoonTools.ECS.Rev2
{
	public class ArchetypeSignature : IEquatable<ArchetypeSignature>
	{
		public static ArchetypeSignature Empty = new ArchetypeSignature(0);

		List<int> Ids;

		public int Count => Ids.Count;

		public ComponentId this[int i] => new ComponentId(Ids[i]);

		public ArchetypeSignature()
		{
			Ids = new List<int>();
		}

		public ArchetypeSignature(int capacity)
		{
			Ids = new List<int>(capacity);
		}

		// Maintains sorted order
		public void Insert(ComponentId componentId)
		{
			var index = Ids.BinarySearch(componentId.Id);

			if (index < 0)
			{
				Ids.Insert(~index, componentId.Id);
			}
		}

		public void Remove(ComponentId componentId)
		{
			var index = Ids.BinarySearch(componentId.Id);

			if (index >= 0)
			{
				Ids.RemoveAt(index);
			}
		}

		public void CopyTo(ArchetypeSignature other)
		{
			other.Ids.AddRange(Ids);
		}

		public override bool Equals(object? obj)
		{
			return obj is ArchetypeSignature signature && Equals(signature);
		}

		public bool Equals(ArchetypeSignature? other)
		{
			if (other == null)
			{
				return false;
			}

			if (Ids.Count != other.Ids.Count)
			{
				return false;
			}

			for (int i = 0; i < Ids.Count; i += 1)
			{
				if (Ids[i] != other.Ids[i])
				{
					return false;
				}
			}

			return true;
		}

		public override int GetHashCode()
		{
			var hashcode = 1;

			foreach (var id in Ids)
			{
				hashcode = HashCode.Combine(hashcode, id);
			}

			return hashcode;
		}
	}
}
