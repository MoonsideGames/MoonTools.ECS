using System;
using System.Collections.Generic;

namespace MoonTools.ECS
{
	internal abstract class RelationStorage
	{
		public abstract bool HasInRelation(int entityID);
		public abstract bool HasOutRelation(int entityID);
		public abstract unsafe void Set(int entityA, int entityB, void* relationData);
		public abstract int GetStorageIndex(int entityA, int entityB);
		public abstract unsafe void* Get(int relationStorageIndex);
		public abstract void UnrelateAll(int entityID);
		public abstract ReverseSpanEnumerator<Entity> OutRelations(int entityID);
		public abstract RelationStorage CreateStorage();
		public abstract void Clear();
	}

	// Relation is the two entities, A related to B.
	// TRelation is the data attached to the relation.
	internal class RelationStorage<TRelation> : RelationStorage where TRelation : unmanaged
	{
		private int count = 0;
		private Dictionary<(Entity, Entity), int> indices = new Dictionary<(Entity, Entity), int>(16);
		private (Entity, Entity)[] relations = new (Entity, Entity)[16];
		private TRelation[] relationDatas = new TRelation[16];
		private Dictionary<int, IndexableSet<Entity>> outRelations = new Dictionary<int, IndexableSet<Entity>>(16);
		private Dictionary<int, IndexableSet<Entity>> inRelations = new Dictionary<int, IndexableSet<Entity>>(16);
		private Stack<IndexableSet<Entity>> listPool = new Stack<IndexableSet<Entity>>();

		public ReverseSpanEnumerator<(Entity, Entity)> All()
		{
			return new ReverseSpanEnumerator<(Entity, Entity)>(new Span<(Entity, Entity)>(relations, 0, count));
		}

		public void Set(in Entity entityA, in Entity entityB, TRelation relationData)
		{
			var relation = (entityA, entityB);

			if (indices.TryGetValue(relation, out var index))
			{
				relationDatas[index] = relationData;
				return;
			}

			var idA = entityA.ID;
			var idB = entityB.ID;

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

		public TRelation Get(in Entity entityA, in Entity entityB)
		{
			return relationDatas[indices[(entityA, entityB)]];
		}

		public bool Has((Entity, Entity) relation)
		{
			return indices.ContainsKey(relation);
		}

		public override ReverseSpanEnumerator<Entity> OutRelations(int entityID)
		{
			if (outRelations.TryGetValue(entityID, out var entityOutRelations))
			{
				return entityOutRelations.GetEnumerator();
			}
			else
			{
				return ReverseSpanEnumerator<Entity>.Empty;
			}
		}

		public Entity OutFirst(int entityID)
		{
#if DEBUG
			if (!outRelations.ContainsKey(entityID) || outRelations[entityID].Count == 0)
			{
				throw new KeyNotFoundException("No out relations to this entity!");
			}
#endif
			return outRelations[entityID][0];
		}

		public override bool HasOutRelation(int entityID)
		{
			return outRelations.ContainsKey(entityID) && outRelations[entityID].Count > 0;
		}

		public int OutRelationCount(int entityID)
		{
			return outRelations.TryGetValue(entityID, out var entityOutRelations) ? entityOutRelations.Count : 0;
		}

		public ReverseSpanEnumerator<Entity> InRelations(int entityID)
		{
			if (inRelations.TryGetValue(entityID, out var entityInRelations))
			{
				return entityInRelations.GetEnumerator();
			}
			else
			{
				return ReverseSpanEnumerator<Entity>.Empty;
			}
		}

		public Entity InFirst(int entityID)
		{
#if DEBUG
			if (!inRelations.ContainsKey(entityID) || inRelations[entityID].Count == 0)
			{
				throw new KeyNotFoundException("No out relations to this entity!");
			}
#endif

			return inRelations[entityID][0];
		}

		public override bool HasInRelation(int entityID)
		{
			return inRelations.ContainsKey(entityID) && inRelations[entityID].Count > 0;
		}

		public int InRelationCount(int entityID)
		{
			return inRelations.TryGetValue(entityID, out var entityInRelations) ? entityInRelations.Count : 0;
		}

		public (bool, bool) Remove(in Entity entityA, in Entity entityB)
		{
			var aEmpty = false;
			var bEmpty = false;
			var relation = (entityA, entityB);

			if (outRelations.TryGetValue(entityA.ID, out var entityOutRelations))
			{
				entityOutRelations.Remove(entityB.ID);
				if (outRelations[entityA.ID].Count == 0)
				{
					aEmpty = true;
				}
			}

			if (inRelations.TryGetValue(entityB.ID, out var entityInRelations))
			{
				entityInRelations.Remove(entityA.ID);
				if (inRelations[entityB.ID].Count == 0)
				{
					bEmpty = true;
				}
			}

			if (indices.TryGetValue(relation, out var index))
			{
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

		private IndexableSet<Entity> AcquireHashSetFromPool()
		{
			if (listPool.Count == 0)
			{
				listPool.Push(new IndexableSet<Entity>());
			}

			return listPool.Pop();
		}

		private void ReturnHashSetToPool(IndexableSet<Entity> hashSet)
		{
			hashSet.Clear();
			listPool.Push(hashSet);
		}

		// untyped methods used for internal implementation

		public override unsafe void Set(int entityA, int entityB, void* relationData)
		{
			Set(entityA, entityB, *((TRelation*) relationData));
		}

		public override int GetStorageIndex(int entityA, int entityB)
		{
			return indices[(entityA, entityB)];
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
			if (outRelations.TryGetValue(entityID, out var entityOutRelations))
			{
				foreach (var entityB in entityOutRelations)
				{
					Remove(entityID, entityB);
				}

				ReturnHashSetToPool(entityOutRelations);
				outRelations.Remove(entityID);
			}

			if (inRelations.TryGetValue(entityID, out var entityInRelations))
			{
				foreach (var entityA in entityInRelations)
				{
					Remove(entityA, entityID);
				}

				ReturnHashSetToPool(entityInRelations);
				inRelations.Remove(entityID);
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
