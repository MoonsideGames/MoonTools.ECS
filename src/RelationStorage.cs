﻿using System;
using System.Collections.Generic;

namespace MoonTools.ECS
{
	internal abstract class RelationStorage
	{
		public abstract unsafe void Set(int entityA, int entityB, void* relationData);
		public abstract unsafe void* Get(int relationStorageIndex);
		public abstract void UnrelateAll(int entityID);
		public abstract IEnumerable<(int, int)> OutRelationIndices(int entityID);
		public abstract RelationStorage CreateStorage();
		public abstract void Clear();
	}

	// Relation is the two entities, A related to B.
	// TRelation is the data attached to the relation.
	internal class RelationStorage<TRelation> : RelationStorage where TRelation : unmanaged
	{
		private int count = 0;
		private Dictionary<Relation, int> indices = new Dictionary<Relation, int>(16);
		private Relation[] relations = new Relation[16];
		private TRelation[] relationDatas = new TRelation[16];
		private Dictionary<int, IndexableSet<int>> outRelations = new Dictionary<int, IndexableSet<int>>(16);
		private Dictionary<int, IndexableSet<int>> inRelations = new Dictionary<int, IndexableSet<int>>(16);
		private Stack<IndexableSet<int>> listPool = new Stack<IndexableSet<int>>();

		public IEnumerable<(Entity, Entity, TRelation)> All()
		{
			for (var i = 0; i < count; i += 1)
			{
				var relation = relations[i];
				yield return (relation.A, relation.B, relationDatas[i]);
			}
		}

		public void Set(Relation relation, TRelation relationData)
		{
			if (indices.ContainsKey(relation))
			{
				var index = indices[relation];
				relationDatas[index] = relationData;
				return;
			}

			var idA = relation.A.ID;
			var idB = relation.B.ID;

			if (!outRelations.ContainsKey(idA))
			{
				outRelations[idA] = AcquireHashSetFromPool();
			}
			outRelations[idA].Add(idB);

			if (!inRelations.ContainsKey(idB))
			{
				inRelations[idB] = AcquireHashSetFromPool();
			}
			inRelations[idB].Add(idA);

			if (count >= relationDatas.Length)
			{
				Array.Resize(ref relations, relations.Length * 2);
				Array.Resize(ref relationDatas, relationDatas.Length * 2);
			}

			relations[count] = relation;
			relationDatas[count] = relationData;
			indices.Add(relation, count);
			count += 1;
		}

		public bool Has(Relation relation)
		{
			return indices.ContainsKey(relation);
		}

		// FIXME: creating the new Relation in here is slightly deranged
		public IEnumerable<(Entity, TRelation)> OutRelations(int entityID)
		{
			if (outRelations.ContainsKey(entityID))
			{
				foreach (var id in outRelations[entityID])
				{
					var relation = new Relation(entityID, id);
					yield return (relation.B, relationDatas[indices[relation]]);
				}
			}
		}

		public (Entity, TRelation) OutFirst(int entityID)
		{
#if DEBUG
			if (!outRelations.ContainsKey(entityID))
			{
				throw new KeyNotFoundException("No out relations to this entity!");
			}
#endif
			var relation = new Relation(entityID, outRelations[entityID][0]);
			return (relation.B, relationDatas[indices[relation]]);
		}

		public bool HasOutRelation(int entityID)
		{
			return outRelations.ContainsKey(entityID) && outRelations[entityID].Count > 0;
		}

		public int OutRelationCount(int entityID)
		{
			return outRelations.ContainsKey(entityID) ? outRelations[entityID].Count : 0;
		}

		public IEnumerable<(Entity, TRelation)> InRelations(int entityID)
		{
			if (inRelations.ContainsKey(entityID))
			{
				foreach (var id in inRelations[entityID])
				{
					var relation = new Relation(id, entityID);
					yield return (relation.A, relationDatas[indices[relation]]);
				}
			}
		}

		public (Entity, TRelation) InFirst(int entityID)
		{
#if DEBUG
			if (!inRelations.ContainsKey(entityID))
			{
				throw new KeyNotFoundException("No out relations to this entity!");
			}
#endif

			var relation = new Relation(inRelations[entityID][0], entityID);
			return (relation.A, relationDatas[indices[relation]]);
		}

		public bool HasInRelation(int entityID)
		{
			return inRelations.ContainsKey(entityID) && inRelations[entityID].Count > 0;
		}

		public int InRelationCount(int entityID)
		{
			return inRelations.ContainsKey(entityID) ? inRelations[entityID].Count : 0;
		}

		public (bool, bool) Remove(Relation relation)
		{
			var aEmpty = false;
			var bEmpty = false;

			if (outRelations.ContainsKey(relation.A.ID))
			{
				outRelations[relation.A.ID].Remove(relation.B.ID);
				if (outRelations[relation.A.ID].Count == 0)
				{
					aEmpty = true;
				}
			}

			if (inRelations.ContainsKey(relation.B.ID))
			{
				inRelations[relation.B.ID].Remove(relation.A.ID);
				if (inRelations[relation.B.ID].Count == 0)
				{
					bEmpty = true;
				}
			}

			if (indices.ContainsKey(relation))
			{
				var index = indices[relation];
				var lastElementIndex = count - 1;

				// move an element into the hole
				if (index != lastElementIndex)
				{
					var lastRelation = relations[lastElementIndex];
					indices[lastRelation] = index;
					relationDatas[index] = relationDatas[lastElementIndex];
					relations[index] = lastRelation;
				}

				count -= 1;
				indices.Remove(relation);
			}

			return (aEmpty, bEmpty);
		}

		private IndexableSet<int> AcquireHashSetFromPool()
		{
			if (listPool.Count == 0)
			{
				listPool.Push(new IndexableSet<int>());
			}

			return listPool.Pop();
		}

		private void ReturnHashSetToPool(IndexableSet<int> hashSet)
		{
			hashSet.Clear();
			listPool.Push(hashSet);
		}

		// untyped methods used for internal implementation

		public override unsafe void Set(int entityA, int entityB, void* relationData)
		{
			Set(new Relation(entityA, entityB), *((TRelation*) relationData));
		}

		public override unsafe void* Get(int relationStorageIndex)
		{
			fixed (void* p = &relations[relationStorageIndex])
			{
				return p;
			}
		}

		public override void UnrelateAll(int entityID)
		{
			if (outRelations.ContainsKey(entityID))
			{
				foreach (var entityB in outRelations[entityID])
				{
					Remove(new Relation(entityID, entityB));
				}

				ReturnHashSetToPool(outRelations[entityID]);
				outRelations.Remove(entityID);
			}

			if (inRelations.ContainsKey(entityID))
			{
				foreach (var entityA in inRelations[entityID])
				{
					Remove(new Relation(entityA, entityID));
				}

				ReturnHashSetToPool(inRelations[entityID]);
				inRelations.Remove(entityID);
			}
		}

		public override IEnumerable<(int, int)> OutRelationIndices(int entityID)
		{
			if (outRelations.ContainsKey(entityID))
			{
				foreach (var id in outRelations[entityID])
				{
					var relation = new Relation(entityID, id);
					yield return (id, indices[relation]);
				}
			}
		}

		public override RelationStorage<TRelation> CreateStorage()
		{
			return new RelationStorage<TRelation>();
		}

		public override void Clear()
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
	}
}
