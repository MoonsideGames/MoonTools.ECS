using System;
using System.Collections.Generic;

namespace MoonTools.ECS
{
	internal class FilterStorage
	{
		private EntityStorage EntityStorage;
		private RelationDepot RelationDepot;
		private TypeIndices ComponentTypeIndices;
		private TypeIndices RelationTypeIndices;
		private Dictionary<FilterSignature, IndexableSet<Entity>> filterSignatureToEntityIDs = new Dictionary<FilterSignature, IndexableSet<Entity>>();
		private Dictionary<int, HashSet<FilterSignature>> componentTypeToFilterSignatures = new Dictionary<int, HashSet<FilterSignature>>();
		private Dictionary<int, HashSet<FilterSignature>> relationTypeToFilterSignatures = new Dictionary<int, HashSet<FilterSignature>>();

		public FilterStorage(
			EntityStorage entityStorage,
			RelationDepot relationDepot,
			TypeIndices componentTypeIndices,
			TypeIndices relationTypeIndices
		) {
			EntityStorage = entityStorage;
			RelationDepot = relationDepot;
			ComponentTypeIndices = componentTypeIndices;
			RelationTypeIndices = relationTypeIndices;
		}

		public Filter CreateFilter(
			HashSet<int> included,
			HashSet<int> excluded,
			HashSet<int> inRelations,
			HashSet<int> outRelations
		) {
			var filterSignature = new FilterSignature(included, excluded, inRelations, outRelations);
			if (!filterSignatureToEntityIDs.ContainsKey(filterSignature))
			{
				filterSignatureToEntityIDs.Add(filterSignature, new IndexableSet<Entity>());

				foreach (var type in included)
				{
					if (!componentTypeToFilterSignatures.ContainsKey(type))
					{
						componentTypeToFilterSignatures.Add(type, new HashSet<FilterSignature>());
					}

					componentTypeToFilterSignatures[type].Add(filterSignature);
				}

				foreach (var type in excluded)
				{
					if (!componentTypeToFilterSignatures.ContainsKey(type))
					{
						componentTypeToFilterSignatures.Add(type, new HashSet<FilterSignature>());
					}

					componentTypeToFilterSignatures[type].Add(filterSignature);
				}

				foreach (var type in inRelations)
				{
					if (!relationTypeToFilterSignatures.ContainsKey(type))
					{
						relationTypeToFilterSignatures.Add(type, new HashSet<FilterSignature>());
					}

					relationTypeToFilterSignatures[type].Add(filterSignature);
				}

				foreach (var type in outRelations)
				{
					if (!relationTypeToFilterSignatures.ContainsKey(type))
					{
						relationTypeToFilterSignatures.Add(type, new HashSet<FilterSignature>());
					}

					relationTypeToFilterSignatures[type].Add(filterSignature);
				}
			}

			return new Filter(this, included, excluded, inRelations, outRelations);
		}

		public ReverseSpanEnumerator<Entity> FilterEntities(FilterSignature filterSignature)
		{
			return filterSignatureToEntityIDs[filterSignature].GetEnumerator();
		}

		public LinearCongruentialEnumerator FilterEntitiesRandom(FilterSignature filterSignature)
		{
			return RandomGenerator.LinearCongruentialGenerator(FilterCount(filterSignature));
		}

		public Entity FilterNthEntity(FilterSignature filterSignature, int index)
		{
			return new Entity(filterSignatureToEntityIDs[filterSignature][index]);
		}

		public Entity FilterRandomEntity(FilterSignature filterSignature)
		{
			var randomIndex = RandomGenerator.Next(FilterCount(filterSignature));
			return new Entity(filterSignatureToEntityIDs[filterSignature][randomIndex]);
		}

		public int FilterCount(FilterSignature filterSignature)
		{
			return filterSignatureToEntityIDs[filterSignature].Count;
		}

		public void CheckComponentChange(int entityID, int componentTypeIndex)
		{
			if (componentTypeToFilterSignatures.TryGetValue(componentTypeIndex, out var filterSignatures))
			{
				foreach (var filterSignature in filterSignatures)
				{
					CheckFilter(entityID, filterSignature);
				}
			}
		}

		public void CheckRelationChange(int entityID, int relationTypeIndex)
		{
			if (relationTypeToFilterSignatures.TryGetValue(relationTypeIndex, out var filterSignatures))
			{
				foreach (var filterSignature in filterSignatures)
				{
					CheckFilter(entityID, filterSignature);
				}
			}
		}

		public void Check<TComponent>(int entityID) where TComponent : unmanaged
		{
			CheckComponentChange(entityID, ComponentTypeIndices.GetIndex<TComponent>());
		}

		public bool CheckSatisfied(int entityID, FilterSignature filterSignature)
		{
			foreach (var type in filterSignature.Included)
			{
				if (!EntityStorage.HasComponent(entityID, type))
				{
					return false;
				}
			}

			foreach (var type in filterSignature.Excluded)
			{
				if (EntityStorage.HasComponent(entityID, type))
				{
					return false;
				}
			}

			foreach (var type in filterSignature.InRelations)
			{
				if (!RelationDepot.HasInRelation(entityID, type))
				{
					return false;
				}
			}

			foreach (var type in filterSignature.OutRelations)
			{
				if (!RelationDepot.HasOutRelation(entityID, type))
				{
					return false;
				}
			}

			return true;
		}

		private void CheckFilter(int entityID, FilterSignature filterSignature)
		{
			if (CheckSatisfied(entityID, filterSignature))
			{
				filterSignatureToEntityIDs[filterSignature].Remove(entityID);
			}
			else
			{
				filterSignatureToEntityIDs[filterSignature].Remove(entityID);
			}
		}

		public void RemoveEntity(int entityID, int componentTypeIndex)
		{
			if (componentTypeToFilterSignatures.TryGetValue(componentTypeIndex, out var filterSignatures))
			{
				foreach (var filterSignature in filterSignatures)
				{
					filterSignatureToEntityIDs[filterSignature].Remove(entityID);
				}
			}
		}
	}
}
