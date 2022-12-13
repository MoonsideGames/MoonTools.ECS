using System;

namespace MoonTools.ECS
{
	internal struct Relation : IEquatable<Relation>
	{
		public Entity A { get; }
		public Entity B { get; }

		internal Relation(Entity entityA, Entity entityB)
		{
			A = entityA;
			B = entityB;
		}

		public override bool Equals(object? obj)
		{
			return obj is Relation relation && Equals(relation);
		}

		public bool Equals(Relation other)
		{
			return A.ID == other.A.ID && B.ID == other.B.ID;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(A.ID, B.ID);
		}
	}
}
