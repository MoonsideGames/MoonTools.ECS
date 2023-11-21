using System;
using MoonTools.ECS.Collections;

namespace MoonTools.ECS;

public class Filter
{
	private World World;
	internal FilterSignature Signature;

	internal IndexableSet<Entity> EntitySet = new IndexableSet<Entity>();

	private bool IsDisposed;

	public ReverseSpanEnumerator<Entity> Entities => EntitySet.GetEnumerator();

	public bool Empty => EntitySet.Count == 0;
	public int Count => EntitySet.Count;

	// WARNING: this WILL crash if the index is out of range!
	public Entity NthEntity(int index) => EntitySet[index];

	// WARNING: this WILL crash if the filter is empty!
	public Entity RandomEntity => EntitySet[RandomManager.Next(EntitySet.Count)];
	public RandomEntityEnumerator EntitiesInRandomOrder => new RandomEntityEnumerator(this);

	internal Filter(World world, FilterSignature signature)
	{
		World = world;
		Signature = signature;
	}

	public void DestroyAllEntities()
	{
		foreach (var entity in EntitySet)
		{
			World.Destroy(entity);
		}
	}

	internal void Check(Entity entity)
	{
		foreach (var type in Signature.Included)
		{
			if (!World.Has(entity, type))
			{
				EntitySet.Remove(entity);
				return;
			}
		}

		foreach (var type in Signature.Excluded)
		{
			if (World.Has(entity, type))
			{
				EntitySet.Remove(entity);
				return;
			}
		}

		EntitySet.Add(entity);
	}

	internal void AddEntity(in Entity entity)
	{
		EntitySet.Add(entity);
	}

	internal void RemoveEntity(in Entity entity)
	{
		EntitySet.Remove(entity);
	}

	internal void Clear()
	{
		EntitySet.Clear();
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
		public Entity Current => Filter.NthEntity(LinearCongruentialEnumerator.Current);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!IsDisposed)
		{
			if (disposing)
			{
				EntitySet.Dispose();
			}

			IsDisposed = true;
		}
	}

	// // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
	// ~Filter()
	// {
	//     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
	//     Dispose(disposing: false);
	// }

	public void Dispose()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}
