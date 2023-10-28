using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MoonTools.ECS.Collections;

namespace MoonTools.ECS.Rev2;

internal class Relation
{
	private int count = 0;
	private int capacity = 16;
	private Column relations;
	private Column relationDatas;
	private Dictionary<(Id, Id), int> indices = new Dictionary<(Id, Id), int>(16);
	private Dictionary<Id, IndexableSet<Id>> outRelations = new Dictionary<Id, IndexableSet<Id>>(16);
	private Dictionary<Id, IndexableSet<Id>> inRelations = new Dictionary<Id, IndexableSet<Id>>(16);
	private Stack<IndexableSet<Id>> listPool = new Stack<IndexableSet<Id>>();

	private bool disposed;

	public Relation(int relationDataSize)
	{
		relations = new Column(Unsafe.SizeOf<(Id, Id)>());
		relationDatas = new Column(relationDataSize);
	}

	public unsafe ReverseSpanEnumerator<(Id, Id)> All()
	{
		return new ReverseSpanEnumerator<(Id, Id)>(new Span<(Id, Id)>((void*) relations.Elements, count));
	}

	public unsafe void Set<T>(in Id entityA, in Id entityB, in T relationData) where T : unmanaged
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

		if (count >= capacity)
		{
			relations.Resize();
			relationDatas.Resize();
		}

		(((Id, Id)*) relationDatas.Elements)[count] = relation;
		((T*) relationDatas.Elements)[count] = relationData;
		indices.Add(relation, count);
		count += 1;
	}

	public ref T Get<T>(in Id entityA, in Id entityB) where T : unmanaged
	{
		var relationIndex = indices[(entityA, entityB)];
		return ref relationDatas.Get<T>(relationIndex);
	}

	public bool Has((Id, Id) relation)
	{
		return indices.ContainsKey(relation);
	}

	public ReverseSpanEnumerator<Id> OutRelations(Id entityID)
	{
		if (outRelations.TryGetValue(entityID, out var entityOutRelations))
		{
			return entityOutRelations.GetEnumerator();
		}
		else
		{
			return ReverseSpanEnumerator<Id>.Empty;
		}
	}

	public Id OutFirst(Id entityID)
	{
		return OutNth(entityID, 0);
	}

	public Id OutNth(Id entityID, int n)
	{
#if DEBUG
		if (!outRelations.ContainsKey(entityID) || outRelations[entityID].Count == 0)
		{
			throw new KeyNotFoundException("No out relations to this entity!");
		}
#endif
		return outRelations[entityID][n];
	}

	public bool HasOutRelation(Id entityID)
	{
		return outRelations.ContainsKey(entityID) && outRelations[entityID].Count > 0;
	}

	public int OutRelationCount(Id entityID)
	{
		return outRelations.TryGetValue(entityID, out var entityOutRelations) ? entityOutRelations.Count : 0;
	}

	public ReverseSpanEnumerator<Id> InRelations(Id entityID)
	{
		if (inRelations.TryGetValue(entityID, out var entityInRelations))
		{
			return entityInRelations.GetEnumerator();
		}
		else
		{
			return ReverseSpanEnumerator<Id>.Empty;
		}
	}

	public Id InFirst(Id entityID)
	{
		return InNth(entityID, 0);
	}

	public Id InNth(Id entityID, int n)
	{
#if DEBUG
		if (!inRelations.ContainsKey(entityID) || inRelations[entityID].Count == 0)
		{
			throw new KeyNotFoundException("No in relations to this entity!");
		}
#endif

		return inRelations[entityID][n];
	}

	public bool HasInRelation(Id entityID)
	{
		return inRelations.ContainsKey(entityID) && inRelations[entityID].Count > 0;
	}

	public int InRelationCount(Id entityID)
	{
		return inRelations.TryGetValue(entityID, out var entityInRelations) ? entityInRelations.Count : 0;
	}

	public (bool, bool) Remove(in Id entityA, in Id entityB)
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
			relationDatas.Delete(index);
			relations.Delete(index);

			var lastElementIndex = count - 1;

			// move an element into the hole
			if (index != lastElementIndex)
			{
				var lastRelation = relations.Get<(Id, Id)>(lastElementIndex);
				indices[lastRelation] = index;
			}

			count -= 1;
			indices.Remove(relation);
		}

		return (aEmpty, bEmpty);
	}

	private IndexableSet<Id> AcquireHashSetFromPool()
	{
		if (listPool.Count == 0)
		{
			listPool.Push(new IndexableSet<Id>());
		}

		return listPool.Pop();
	}

	private void ReturnHashSetToPool(IndexableSet<Id> hashSet)
	{
		hashSet.Clear();
		listPool.Push(hashSet);
	}

	public void Clear()
	{
		count = 0;
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
