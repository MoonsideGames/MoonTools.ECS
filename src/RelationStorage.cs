using System.Collections.Generic;

namespace MoonTools.ECS
{
	internal class RelationStorage
	{
		private HashSet<Relation> relations = new HashSet<Relation>(16);
		private Dictionary<int, HashSet<int>> entitiesRelatedToA = new Dictionary<int, HashSet<int>>(16);
		private Dictionary<int, HashSet<int>> entitiesRelatedToB = new Dictionary<int, HashSet<int>>(16);
		private Stack<HashSet<int>> listPool = new Stack<HashSet<int>>();

		public IEnumerable<Relation> All()
		{
			foreach (var relation in relations)
			{
				yield return relation;
			}
		}

		public void Add(Relation relation)
		{
			if (relations.Contains(relation)) { return; }

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

			relations.Add(relation);
		}

		public bool Has(Relation relation)
		{
			return relations.Contains(relation);
		}

		public IEnumerable<Entity> RelatedToA(int entityID)
		{
			if (entitiesRelatedToA.ContainsKey(entityID))
			{
				foreach (var id in entitiesRelatedToA[entityID])
				{
					yield return new Entity(id);
				}
			}
		}

		public IEnumerable<Entity> RelatedToB(int entityID)
		{
			if (entitiesRelatedToB.ContainsKey(entityID))
			{
				foreach (var id in entitiesRelatedToB[entityID])
				{
					yield return new Entity(id);
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

		// this exists so we don't recurse in OnEntityDestroy
		private bool DestroyRemove(Relation relation)
		{
			return relations.Remove(relation);
		}

		public void OnEntityDestroy(int entityID)
		{
			if (entitiesRelatedToA.ContainsKey(entityID))
			{
				foreach (var entityB in entitiesRelatedToA[entityID])
				{
					DestroyRemove(new Relation(entityID, entityB));
				}

				ReturnHashSetToPool(entitiesRelatedToA[entityID]);
				entitiesRelatedToA.Remove(entityID);
			}

			if (entitiesRelatedToB.ContainsKey(entityID))
			{
				foreach (var entityA in entitiesRelatedToB[entityID])
				{
					DestroyRemove(new Relation(entityA, entityID));
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
			listPool.Push(hashSet);
		}
	}
}
