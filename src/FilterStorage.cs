using System;
using System.Collections.Generic;

namespace MoonTools.ECS
{
	internal class FilterStorage
	{
		private EntityStorage EntityStorage;
		private TypeIndices ComponentTypeIndices;
		private Dictionary<FilterSignature, IndexableSet<Entity>> filterSignatureToEntityIDs = new Dictionary<FilterSignature, IndexableSet<Entity>>();
		private Dictionary<int, HashSet<FilterSignature>> typeToFilterSignatures = new Dictionary<int, HashSet<FilterSignature>>();

		public FilterStorage(EntityStorage entityStorage, TypeIndices componentTypeIndices)
		{
			EntityStorage = entityStorage;
			ComponentTypeIndices = componentTypeIndices;
		}

		public Filter CreateFilter(HashSet<int> included, HashSet<int> excluded)
		{
			var filterSignature = new FilterSignature(included, excluded);
			if (!filterSignatureToEntityIDs.ContainsKey(filterSignature))
			{
				filterSignatureToEntityIDs.Add(filterSignature, new IndexableSet<Entity>());

				foreach (var type in included)
				{
					if (!typeToFilterSignatures.ContainsKey(type))
					{
						typeToFilterSignatures.Add(type, new HashSet<FilterSignature>());
					}

					typeToFilterSignatures[type].Add(filterSignature);
				}

				foreach (var type in excluded)
				{
					if (!typeToFilterSignatures.ContainsKey(type))
					{
						typeToFilterSignatures.Add(type, new HashSet<FilterSignature>());
					}

					typeToFilterSignatures[type].Add(filterSignature);
				}
			}
			return new Filter(this, included, excluded);
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

		public void Check(int entityID, int componentTypeIndex)
		{
			if (typeToFilterSignatures.TryGetValue(componentTypeIndex, out var filterSignatures))
			{
				foreach (var filterSignature in filterSignatures)
				{
					CheckFilter(entityID, filterSignature);
				}
			}
		}

		public void Check<TComponent>(int entityID) where TComponent : unmanaged
		{
			Check(entityID, ComponentTypeIndices.GetIndex<TComponent>());
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

			return true;
		}

		private void CheckFilter(int entityID, FilterSignature filterSignature)
		{
			foreach (var type in filterSignature.Included)
			{
				if (!EntityStorage.HasComponent(entityID, type))
				{
					filterSignatureToEntityIDs[filterSignature].Remove(entityID);
					return;
				}
			}

			foreach (var type in filterSignature.Excluded)
			{
				if (EntityStorage.HasComponent(entityID, type))
				{
					filterSignatureToEntityIDs[filterSignature].Remove(entityID);
					return;
				}
			}

			filterSignatureToEntityIDs[filterSignature].Add(entityID);
		}

		public void RemoveEntity(int entityID, int componentTypeIndex)
		{
			if (typeToFilterSignatures.TryGetValue(componentTypeIndex, out var filterSignatures))
			{
				foreach (var filterSignature in filterSignatures)
				{
					filterSignatureToEntityIDs[filterSignature].Remove(entityID);
				}
			}
		}
	}
}
