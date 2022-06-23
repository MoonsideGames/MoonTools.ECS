using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MoonTools.ECS
{
	internal abstract class RelationStorage
	{
		public abstract RelationStorageState CreateState();
		public abstract void Save(RelationStorageState state);
		public abstract void Load(RelationStorageState state);
		public abstract void OnEntityDestroy(int entityID);
	}

	// Relation is the two entities, A related to B.
	// TRelation is the data attached to the relation.
	internal class RelationStorage<TRelation> : RelationStorage where TRelation : unmanaged
	{
		private int count = 0;
		private Dictionary<Relation, int> indices = new Dictionary<Relation, int>(16);
		private Relation[] relations = new Relation[16];
		private TRelation[] relationDatas = new TRelation[16];
		private Dictionary<int, HashSet<int>> entitiesRelatedToA = new Dictionary<int, HashSet<int>>(16);
		private Dictionary<int, HashSet<int>> entitiesRelatedToB = new Dictionary<int, HashSet<int>>(16);
		private Stack<HashSet<int>> listPool = new Stack<HashSet<int>>();

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

			if (!entitiesRelatedToA.ContainsKey(idA))
			{
				entitiesRelatedToA[idA] = AcquireHashSetFromPool();
			}
			entitiesRelatedToA[idA].Add(idB);

			if (!entitiesRelatedToB.ContainsKey(idB))
			{
				entitiesRelatedToB[idB] = AcquireHashSetFromPool();
			}
			entitiesRelatedToB[idB].Add(idA);

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

		// FIXME: is there a more descriptive name for these?
		public IEnumerable<(Entity, TRelation)> RelatedToA(int entityID)
		{
			if (entitiesRelatedToA.ContainsKey(entityID))
			{
				foreach (var id in entitiesRelatedToA[entityID])
				{
					var relation = new Relation(entityID, id);
					yield return (relation.B, relationDatas[indices[relation]]);
				}
			}
		}

		public IEnumerable<(Entity, TRelation)> RelatedToB(int entityID)
		{
			if (entitiesRelatedToB.ContainsKey(entityID))
			{
				foreach (var id in entitiesRelatedToB[entityID])
				{
					var relation = new Relation(id, entityID);
					yield return (relation.A, relationDatas[indices[relation]]);
				}
			}
		}

		public bool Remove(Relation relation)
		{
			if (entitiesRelatedToA.ContainsKey(relation.A.ID))
			{
				entitiesRelatedToA[relation.A.ID].Remove(relation.B.ID);
			}

			if (entitiesRelatedToB.ContainsKey(relation.B.ID))
			{
				entitiesRelatedToB[relation.B.ID].Remove(relation.A.ID);
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
				return true;
			}

			return false;
		}

		public override void OnEntityDestroy(int entityID)
		{
			if (entitiesRelatedToA.ContainsKey(entityID))
			{
				foreach (var entityB in entitiesRelatedToA[entityID])
				{
					Remove(new Relation(entityID, entityB));
				}

				ReturnHashSetToPool(entitiesRelatedToA[entityID]);
				entitiesRelatedToA.Remove(entityID);
			}

			if (entitiesRelatedToB.ContainsKey(entityID))
			{
				foreach (var entityA in entitiesRelatedToB[entityID])
				{
					Remove(new Relation(entityA, entityID));
				}

				ReturnHashSetToPool(entitiesRelatedToB[entityID]);
				entitiesRelatedToB.Remove(entityID);
			}
		}

		private HashSet<int> AcquireHashSetFromPool()
		{
			if (listPool.Count == 0)
			{
				listPool.Push(new HashSet<int>());
			}

			return listPool.Pop();
		}

		private void ReturnHashSetToPool(HashSet<int> hashSet)
		{
			hashSet.Clear();
			listPool.Push(hashSet);
		}

		public override RelationStorageState CreateState()
		{
			return RelationStorageState.Create<TRelation>(count);
		}

		public override void Save(RelationStorageState state)
		{
			ReadOnlySpan<byte> relationBytes = MemoryMarshal.Cast<Relation, byte>(relations);

			if (relationBytes.Length > state.Relations.Length)
			{
				Array.Resize(ref state.Relations, relationBytes.Length);
			}
			relationBytes.CopyTo(state.Relations);

			ReadOnlySpan<byte> relationDataBytes = MemoryMarshal.Cast<TRelation, byte>(relationDatas);

			if (relationDataBytes.Length > state.RelationDatas.Length)
			{
				Array.Resize(ref state.RelationDatas, relationDataBytes.Length);
			}
			relationDataBytes.CopyTo(state.RelationDatas);

			state.Count = count;
		}

		public override void Load(RelationStorageState state)
		{
			state.Relations.CopyTo(MemoryMarshal.Cast<Relation, byte>(relations));
			state.RelationDatas.CopyTo(MemoryMarshal.Cast<TRelation, byte>(relationDatas));

			indices.Clear();
			entitiesRelatedToA.Clear();
			entitiesRelatedToB.Clear();
			for (var i = 0; i < state.Count; i += 1)
			{
				var relation = relations[i];
				indices[relation] = i;

				if (!entitiesRelatedToA.ContainsKey(relation.A.ID))
				{
					entitiesRelatedToA[relation.A.ID] = AcquireHashSetFromPool();
				}
				entitiesRelatedToA[relation.A.ID].Add(relation.B.ID);

				if (!entitiesRelatedToB.ContainsKey(relation.B.ID))
				{
					entitiesRelatedToB[relation.B.ID] = AcquireHashSetFromPool();
				}
				entitiesRelatedToB[relation.B.ID].Add(relation.A.ID);
			}

			count = state.Count;
		}
	}
}
