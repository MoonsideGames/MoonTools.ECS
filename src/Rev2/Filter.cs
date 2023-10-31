using System;
using System.Collections.Generic;

namespace MoonTools.ECS.Rev2;

// TODO: do we want to get fancy with queries beyond Include and Exclude?
public class Filter
{
	private Archetype EmptyArchetype;
	private HashSet<Id> Included;
	private HashSet<Id> Excluded;

	public EntityEnumerator Entities => new EntityEnumerator(this);
	internal ArchetypeEnumerator Archetypes => new ArchetypeEnumerator(this);
	public RandomEntityEnumerator EntitiesInRandomOrder => new RandomEntityEnumerator(this);

	public bool Empty
	{
		get
		{
			var empty = true;

			foreach (var archetype in Archetypes)
			{
				if (archetype.Count > 0)
				{
					return false;
				}
			}

			return empty;
		}
	}

	public int Count
	{
		get
		{
			var count = 0;

			foreach (var archetype in Archetypes)
			{
				count += archetype.Count;
			}

			return count;
		}
	}

	public Id RandomEntity
	{
		get
		{
			var randomIndex = RandomManager.Next(Count);
			return NthEntity(randomIndex);
		}
	}

	// WARNING: this WILL crash if the index is out of range!
	public Id NthEntity(int index)
	{
		var count = 0;

		foreach (var archetype in Archetypes)
		{
			count += archetype.Count;
			if (index < count)
			{
				return archetype.RowToEntity[index];
			}

			index -= count;
		}

		throw new InvalidOperationException("Filter index out of range!");
	}

	public void DestroyAllEntities()
	{
		foreach (var archetype in Archetypes)
		{
			archetype.ClearAll();
		}
	}

	internal Filter(Archetype emptyArchetype, HashSet<Id> included, HashSet<Id> excluded)
	{
		EmptyArchetype = emptyArchetype;
		Included = included;
		Excluded = excluded;
	}

	internal ref struct ArchetypeEnumerator
	{
		private Archetype CurrentArchetype;

		// TODO: pool these
		private Queue<Archetype> ArchetypeQueue = new Queue<Archetype>();
		private Queue<Archetype> ArchetypeSearchQueue = new Queue<Archetype>();
		private HashSet<Archetype> Explored = new HashSet<Archetype>();

		public ArchetypeEnumerator GetEnumerator() => this;

		public ArchetypeEnumerator(Filter filter)
		{
			var empty = filter.EmptyArchetype;
			ArchetypeSearchQueue.Enqueue(empty);

			while (ArchetypeSearchQueue.TryDequeue(out var current))
			{
				// exclude the empty archetype
				var satisfiesFilter = filter.Included.Count != 0;

				foreach (var componentId in filter.Included)
				{
					if (!current.ComponentToColumnIndex.ContainsKey(componentId))
					{
						satisfiesFilter = false;
					}
				}

				foreach (var componentId in filter.Excluded)
				{
					if (current.ComponentToColumnIndex.ContainsKey(componentId))
					{
						satisfiesFilter = false;
					}
				}

				if (satisfiesFilter)
				{
					ArchetypeQueue.Enqueue(current);
				}

				// breadth-first search
				// ignore excluded component edges
				foreach (var (componentId, edge) in current.Edges)
				{
					if (!Explored.Contains(edge.Add) && !filter.Excluded.Contains(componentId))
					{
						Explored.Add(edge.Add);
						ArchetypeSearchQueue.Enqueue(edge.Add);
					}
				}
			}
		}

		public bool MoveNext()
		{
			return ArchetypeQueue.TryDequeue(out CurrentArchetype!);
		}

		public Archetype Current => CurrentArchetype;
	}

	public ref struct EntityEnumerator
	{
		private Id CurrentEntity;

		public EntityEnumerator GetEnumerator() => this;

		// TODO: pool this
		Queue<Id> EntityQueue = new Queue<Id>();

		internal EntityEnumerator(Filter filter)
		{
			var archetypeEnumerator = new ArchetypeEnumerator(filter);

			foreach (var archetype in archetypeEnumerator)
			{
				foreach (var entity in archetype.RowToEntity)
				{
					EntityQueue.Enqueue(entity);
				}
			}
		}

		public bool MoveNext()
		{
			return EntityQueue.TryDequeue(out CurrentEntity);
		}

		public Id Current => CurrentEntity;
	}

	public ref struct RandomEntityEnumerator
	{
		private Filter Filter;
		private LinearCongruentialEnumerator LinearCongruentialEnumerator;

		public RandomEntityEnumerator GetEnumerator() => this;

		internal RandomEntityEnumerator(Filter filter)
		{
			Filter = filter;
			LinearCongruentialEnumerator =
				RandomManager.LinearCongruentialSequence(filter.Count);
		}

		public bool MoveNext() => LinearCongruentialEnumerator.MoveNext();
		public Id Current => Filter.NthEntity(LinearCongruentialEnumerator.Current);
	}
}
