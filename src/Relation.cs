using System;

namespace MoonTools.ECS
{
	public struct Relation : IEquatable<Relation>
	{
		public Entity A { get; }
		public Entity B { get; }

		internal Relation(Entity entityA, Entity entityB)
		{
			A = entityA;
			B = entityB;
		}

		internal Relation(int idA, int idB)
		{
			A = new Entity(idA);
			B = new Entity(idB);
		}

		public override bool Equals(object? obj)
		{
			return obj is Relation relation && Equals(relation);
		}

		public bool Equals(Relation other)
		{
			return A.Equals(other.A) && B.Equals(other.B);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(A, B);
		}
	}
}
