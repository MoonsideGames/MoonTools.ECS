using System;

namespace MoonTools.ECS
{
	public struct Entity : IEquatable<Entity>
	{
		public int ID { get; }

		internal Entity(int id)
		{
			ID = id;
		}

		public override bool Equals(object? obj)
		{
			return obj is Entity entity && Equals(entity);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(ID);
		}

		public bool Equals(Entity other)
		{
			return ID == other.ID;
		}

		public static bool operator ==(Entity a, Entity b)
		{
			return a.Equals(b);
		}

		public static bool operator !=(Entity a, Entity b)
		{
			return !a.Equals(b);
		}
	}
}
