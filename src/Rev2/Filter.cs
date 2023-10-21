using System;
using System.Runtime.InteropServices;

namespace MoonTools.ECS.Rev2
{
	// TODO: do we want to get fancy with queries beyond Include and Exclude?
	// TODO: need an edge iterator as part of this nested horseshit
	public class Filter
	{
		private Archetype Start;

		public EntityEnumerator Entities => new EntityEnumerator(Start);

		private ref struct FilterEnumerator
		{
			private Archetype CurrentArchetype;
			private bool Active;

			public FilterEnumerator(Archetype start)
			{
				CurrentArchetype = start;
				Active = false;
			}

			public bool MoveNext()
			{
				if (!Active)
				{
					Active = true;
					return true;
				}

				// TODO: go to next available edge
			}

			public Archetype Current => CurrentArchetype;
		}

		public ref struct EntityEnumerator
		{
			private FilterEnumerator FilterEnumerator;
			private ReverseSpanEnumerator<EntityId> EntityListEnumerator;
			private bool EntityListEnumeratorActive;

			public EntityEnumerator GetEnumerator() => this;

			public EntityEnumerator(Archetype start)
			{
				FilterEnumerator = new FilterEnumerator(start);
			}

			public bool MoveNext()
			{
				if (!EntityListEnumeratorActive || !EntityListEnumerator.MoveNext())
				{
					if (!FilterEnumerator.MoveNext())
					{
						return false;
					}

					if (FilterEnumerator.Current.RowToEntity.Count != 0)
					{
						EntityListEnumerator = new ReverseSpanEnumerator<EntityId>(CollectionsMarshal.AsSpan(FilterEnumerator.Current.RowToEntity));
						EntityListEnumeratorActive = true;
					}

					return MoveNext();
				}

				return true;
			}

			public EntityId Current => EntityListEnumerator.Current;
		}
	}
}
