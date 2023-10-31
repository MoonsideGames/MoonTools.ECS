using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MoonTools.ECS.Collections;

namespace MoonTools.ECS.Rev2;

// TODO: implement this entire class with NativeMemory equivalents, can just memcpy for snapshots
internal class RelationStorage
{
	internal NativeArray relations;
	internal NativeArray relationDatas;
	internal Dictionary<(EntityId, EntityId), int> indices = new Dictionary<(EntityId, EntityId), int>(16);
	internal Dictionary<EntityId, IndexableSet<EntityId>> outRelations = new Dictionary<EntityId, IndexableSet<EntityId>>(16);
	internal Dictionary<EntityId, IndexableSet<EntityId>> inRelations = new Dictionary<EntityId, IndexableSet<EntityId>>(16);
	private Stack<IndexableSet<EntityId>> listPool = new Stack<IndexableSet<EntityId>>();

	private bool disposed;

	public RelationStorage(int relationDataSize)
	{
		relations = new NativeArray(Unsafe.SizeOf<(EntityId, EntityId)>());
		relationDatas = new NativeArray(relationDataSize);
	}

	public ReverseSpanEnumerator<(EntityId, EntityId)> All()
	{
		return new ReverseSpanEnumerator<(EntityId, EntityId)>(relations.ToSpan<(EntityId, EntityId)>());
	}

	public unsafe void Set<T>(in EntityId entityA, in EntityId entityB, in T relationData) where T : unmanaged
	{
		var relation = (entityA, entityB);

		if (indices.TryGetValue(relation, out var index))
		{
			((T*) relationDatas.Elements)[index] = relationData;
			return;
		}

		if (!outRelations.ContainsKey(entityA))
		{
			outRelations[entityA] = AcquireHashSetFromPool();
		}
		outRelations[entityA].Add(entityB);

		if (!inRelations.ContainsKey(entityB))
		{
			inRelations[entityB] = AcquireHashSetFromPool();
		}
		inRelations[entityB].Add(entityA);

		relations.Append(relation);
		relationDatas.Append(relationData);
		indices.Add(relation, relations.Count - 1);
	}

	public ref T Get<T>(in EntityId entityA, in EntityId entityB) where T : unmanaged
	{
		var relationIndex = indices[(entityA, entityB)];
		return ref relationDatas.Get<T>(relationIndex);
	}

	public bool Has(EntityId entityA, EntityId entityB)
	{
		return indices.ContainsKey((entityA, entityB));
	}

	public ReverseSpanEnumerator<EntityId> OutRelations(EntityId entityID)
	{
		if (outRelations.TryGetValue(entityID, out var entityOutRelations))
		{
			return entityOutRelations.GetEnumerator();
		}
		else
		{
			return ReverseSpanEnumerator<EntityId>.Empty;
		}
	}

	public EntityId OutFirst(EntityId entityID)
	{
		return OutNth(entityID, 0);
	}

	public EntityId OutNth(EntityId entityID, int n)
	{
#if DEBUG
		if (!outRelations.ContainsKey(entityID) || outRelations[entityID].Count == 0)
		{
			throw new KeyNotFoundException("No out relations to this entity!");
		}
#endif
		return outRelations[entityID][n];
	}

	public bool HasOutRelation(EntityId entityID)
	{
		return outRelations.ContainsKey(entityID) && outRelations[entityID].Count > 0;
	}

	public int OutRelationCount(EntityId entityID)
	{
		return outRelations.TryGetValue(entityID, out var entityOutRelations) ? entityOutRelations.Count : 0;
	}

	public ReverseSpanEnumerator<EntityId> InRelations(EntityId entityID)
	{
		if (inRelations.TryGetValue(entityID, out var entityInRelations))
		{
			return entityInRelations.GetEnumerator();
		}
		else
		{
			return ReverseSpanEnumerator<EntityId>.Empty;
		}
	}

	public EntityId InFirst(EntityId entityID)
	{
		return InNth(entityID, 0);
	}

	public EntityId InNth(EntityId entityID, int n)
	{
#if DEBUG
		if (!inRelations.ContainsKey(entityID) || inRelations[entityID].Count == 0)
		{
			throw new KeyNotFoundException("No in relations to this entity!");
		}
#endif

		return inRelations[entityID][n];
	}

	public bool HasInRelation(EntityId entityID)
	{
		return inRelations.ContainsKey(entityID) && inRelations[entityID].Count > 0;
	}

	public int InRelationCount(EntityId entityID)
	{
		return inRelations.TryGetValue(entityID, out var entityInRelations) ? entityInRelations.Count : 0;
	}

	public (bool, bool) Remove(in EntityId entityA, in EntityId entityB)
	{
		var aEmpty = false;
		var bEmpty = false;
		var relation = (entityA, entityB);

		if (outRelations.TryGetValue(entityA, out var entityOutRelations))
		{
			entityOutRelations.Remove(entityB);
			if (outRelations[entityA].Count == 0)
			{
				aEmpty = true;
			}
		}

		if (inRelations.TryGetValue(entityB, out var entityInRelations))
		{
			entityInRelations.Remove(entityA);
			if (inRelations[entityB].Count == 0)
			{
				bEmpty = true;
			}
		}

		if (indices.TryGetValue(relation, out var index))
		{
			var lastElementIndex = relations.Count - 1;

			relationDatas.Delete(index);
			relations.Delete(index);

			// move an element into the hole
			if (index != lastElementIndex)
			{
				var lastRelation = relations.Get<(EntityId, EntityId)>(lastElementIndex);
				indices[lastRelation] = index;
			}

			indices.Remove(relation);
		}

		return (aEmpty, bEmpty);
	}

	public void RemoveEntity(in EntityId entity)
	{
		if (outRelations.TryGetValue(entity, out var entityOutRelations))
		{
			foreach (var entityB in entityOutRelations)
			{
				Remove(entity, entityB);
			}

			ReturnHashSetToPool(entityOutRelations);
			outRelations.Remove(entity);
		}

		if (inRelations.TryGetValue(entity, out var entityInRelations))
		{
			foreach (var entityA in entityInRelations)
			{
				Remove(entityA, entity);
			}

			ReturnHashSetToPool(entityInRelations);
			inRelations.Remove(entity);
		}
	}

	internal IndexableSet<EntityId> AcquireHashSetFromPool()
	{
		if (listPool.Count == 0)
		{
			listPool.Push(new IndexableSet<EntityId>());
		}

		return listPool.Pop();
	}

	private void ReturnHashSetToPool(IndexableSet<EntityId> hashSet)
	{
		hashSet.Clear();
		listPool.Push(hashSet);
	}

	public void Clear()
	{
		indices.Clear();

		foreach (var set in inRelations.Values)
		{
			ReturnHashSetToPool(set);
		}
		inRelations.Clear();

		foreach (var set in outRelations.Values)
		{
			ReturnHashSetToPool(set);
		}
		outRelations.Clear();

		relations.Count = 0;
		relationDatas.Count = 0;
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!disposed)
		{
			Clear();

			if (disposing)
			{
				foreach (var set in listPool)
				{
					set.Dispose();
				}

				relations.Dispose();
				relationDatas.Dispose();

				relations = null;
				relationDatas = null;
			}

			disposed = true;
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
