using System.Collections.Generic;

namespace MoonTools.ECS
{
	internal abstract class RelationStorage
	{
		public abstract void OnEntityDestroy(int entityID);
	}

	// Relation is the two entities, A related to B.
	// TRelation is the data attached to the relation.
	internal class RelationStorage<TRelation> : RelationStorage where TRelation : struct
	{
		private Dictionary<Relation, TRelation> relations = new Dictionary<Relation, TRelation>(16);
		private Dictionary<int, HashSet<int>> entitiesRelatedToA = new Dictionary<int, HashSet<int>>(16);
		private Dictionary<int, HashSet<int>> entitiesRelatedToB = new Dictionary<int, HashSet<int>>(16);
		private Stack<HashSet<int>> listPool = new Stack<HashSet<int>>();

		public IEnumerable<(Entity, Entity, TRelation)> All()
		{
			foreach (var relationData in relations)
			{
				yield return (relationData.Key.A, relationData.Key.B, relationData.Value);
			}
		}

		public void Add(Relation relation, TRelation relationData)
		{
			if (relations.ContainsKey(relation)) { return; }

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

			relations.Add(relation, relationData);
		}

		public bool Has(Relation relation)
		{
			return relations.ContainsKey(relation);
		}

		// FIXME: is there a more descriptive name for these?
		public IEnumerable<(Entity, TRelation)> RelatedToA(int entityID)
		{
			if (entitiesRelatedToA.ContainsKey(entityID))
			{
				foreach (var id in entitiesRelatedToA[entityID])
				{
					var relation = new Relation(entityID, id);
					yield return (relation.B, relations[relation]);
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
					yield return (relation.A, relations[relation]);
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

			return relations.Remove(relation);
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
	}
}
