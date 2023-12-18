using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MoonTools.ECS.Collections;

namespace MoonTools.ECS;

// TODO: implement this entire class with NativeMemory equivalents, can just memcpy for snapshots
internal class RelationStorage
{
	internal NativeArray Relations;
	internal NativeArray RelationDatas;
	internal int ElementSize;
	internal Dictionary<(Entity, Entity), int> Indices = new Dictionary<(Entity, Entity), int>(16);
	internal Dictionary<Entity, IndexableSet<Entity>> OutRelationSets = new Dictionary<Entity, IndexableSet<Entity>>(16);
	internal Dictionary<Entity, IndexableSet<Entity>> InRelationSets = new Dictionary<Entity, IndexableSet<Entity>>(16);
	private Stack<IndexableSet<Entity>> ListPool = new Stack<IndexableSet<Entity>>();

	private bool IsDisposed;

	public RelationStorage(int relationDataSize)
	{
		ElementSize = relationDataSize;
		Relations = new NativeArray(Unsafe.SizeOf<(Entity, Entity)>());
		RelationDatas = new NativeArray(relationDataSize);
	}

	public ReverseSpanEnumerator<(Entity, Entity)> All()
	{
		return new ReverseSpanEnumerator<(Entity, Entity)>(Relations.ToSpan<(Entity, Entity)>());
	}

	public unsafe void Set<T>(in Entity entityA, in Entity entityB, in T relationData) where T : unmanaged
	{
		var relation = (entityA, entityB);

		if (Indices.TryGetValue(relation, out var index))
		{
			RelationDatas.Set(index, relationData);
			return;
		}

		if (!OutRelationSets.ContainsKey(entityA))
		{
			OutRelationSets[entityA] = AcquireHashSetFromPool();
		}
		OutRelationSets[entityA].Add(entityB);

		if (!InRelationSets.ContainsKey(entityB))
		{
			InRelationSets[entityB] = AcquireHashSetFromPool();
		}
		InRelationSets[entityB].Add(entityA);

		Relations.Append(relation);
		RelationDatas.Append(relationData);
		Indices.Add(relation, Relations.Count - 1);
	}

	public ref T Get<T>(in Entity entityA, in Entity entityB) where T : unmanaged
	{
		var relationIndex = Indices[(entityA, entityB)];
		return ref RelationDatas.Get<T>(relationIndex);
	}

	public bool Has(Entity entityA, Entity entityB)
	{
		return Indices.ContainsKey((entityA, entityB));
	}

	public ReverseSpanEnumerator<Entity> OutRelations(Entity Entity)
	{
		if (OutRelationSets.TryGetValue(Entity, out var entityOutRelations))
		{
			return entityOutRelations.GetEnumerator();
		}
		else
		{
			return ReverseSpanEnumerator<Entity>.Empty;
		}
	}

	public Entity OutFirst(Entity Entity)
	{
		return OutNth(Entity, 0);
	}

	public Entity OutNth(Entity Entity, int n)
	{
#if DEBUG
		if (!OutRelationSets.ContainsKey(Entity) || OutRelationSets[Entity].Count == 0)
		{
			throw new KeyNotFoundException("No out relations to this entity!");
		}
#endif
		return OutRelationSets[Entity][n];
	}

	public bool HasOutRelation(Entity Entity)
	{
		return OutRelationSets.ContainsKey(Entity) && OutRelationSets[Entity].Count > 0;
	}

	public int OutRelationCount(Entity Entity)
	{
		return OutRelationSets.TryGetValue(Entity, out var entityOutRelations) ? entityOutRelations.Count : 0;
	}

	public ReverseSpanEnumerator<Entity> InRelations(Entity Entity)
	{
		if (InRelationSets.TryGetValue(Entity, out var entityInRelations))
		{
			return entityInRelations.GetEnumerator();
		}
		else
		{
			return ReverseSpanEnumerator<Entity>.Empty;
		}
	}

	public Entity InFirst(Entity Entity)
	{
		return InNth(Entity, 0);
	}

	public Entity InNth(Entity Entity, int n)
	{
#if DEBUG
		if (!InRelationSets.ContainsKey(Entity) || InRelationSets[Entity].Count == 0)
		{
			throw new KeyNotFoundException("No in relations to this entity!");
		}
#endif

		return InRelationSets[Entity][n];
	}

	public bool HasInRelation(Entity Entity)
	{
		return InRelationSets.ContainsKey(Entity) && InRelationSets[Entity].Count > 0;
	}

	public int InRelationCount(Entity Entity)
	{
		return InRelationSets.TryGetValue(Entity, out var entityInRelations) ? entityInRelations.Count : 0;
	}

	public (bool, bool) Remove(in Entity entityA, in Entity entityB)
	{
		var aEmpty = false;
		var bEmpty = false;
		var relation = (entityA, entityB);

		if (OutRelationSets.TryGetValue(entityA, out var entityOutRelations))
		{
			entityOutRelations.Remove(entityB);
			if (OutRelationSets[entityA].Count == 0)
			{
				aEmpty = true;
			}
		}

		if (InRelationSets.TryGetValue(entityB, out var entityInRelations))
		{
			entityInRelations.Remove(entityA);
			if (InRelationSets[entityB].Count == 0)
			{
				bEmpty = true;
			}
		}

		if (Indices.TryGetValue(relation, out var index))
		{
			var lastElementIndex = Relations.Count - 1;

			// move an element into the hole
			if (index != lastElementIndex)
			{
				var lastRelation = Relations.Get<(Entity, Entity)>(lastElementIndex);
				Indices[lastRelation] = index;
			}

			RelationDatas.Delete(index);
			Relations.Delete(index);

			Indices.Remove(relation);
		}

		return (aEmpty, bEmpty);
	}

	public void RemoveEntity(in Entity entity)
	{
		if (OutRelationSets.TryGetValue(entity, out var entityOutRelations))
		{
			foreach (var entityB in entityOutRelations)
			{
				Remove(entity, entityB);
			}

			ReturnHashSetToPool(entityOutRelations);
			OutRelationSets.Remove(entity);
		}

		if (InRelationSets.TryGetValue(entity, out var entityInRelations))
		{
			foreach (var entityA in entityInRelations)
			{
				Remove(entityA, entity);
			}

			ReturnHashSetToPool(entityInRelations);
			InRelationSets.Remove(entity);
		}
	}

	internal IndexableSet<Entity> AcquireHashSetFromPool()
	{
		if (ListPool.Count == 0)
		{
			ListPool.Push(new IndexableSet<Entity>());
		}

		return ListPool.Pop();
	}

	private void ReturnHashSetToPool(IndexableSet<Entity> hashSet)
	{
		hashSet.Clear();
		ListPool.Push(hashSet);
	}

	public void Clear()
	{
		Indices.Clear();

		foreach (var set in InRelationSets.Values)
		{
			ReturnHashSetToPool(set);
		}
		InRelationSets.Clear();

		foreach (var set in OutRelationSets.Values)
		{
			ReturnHashSetToPool(set);
		}
		OutRelationSets.Clear();

		Relations.Clear();
		RelationDatas.Clear();
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!IsDisposed)
		{
			Clear();

			if (disposing)
			{
				foreach (var set in ListPool)
				{
					set.Dispose();
				}

				Relations.Dispose();
				RelationDatas.Dispose();
			}

			IsDisposed = true;
		}
	}

	// ~RelationStorage()
	// {
	// 	// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
	// 	Dispose(disposing: false);
	// }

	public void Dispose()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}
