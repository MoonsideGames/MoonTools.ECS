using System;
using System.Collections.Generic;

namespace MoonTools.ECS
{
	internal class FilterStorage
	{
		private EntityStorage EntityStorage;
		private TypeIndices ComponentTypeIndices;
		private Dictionary<FilterSignature, IndexableSet<int>> filterSignatureToEntityIDs = new Dictionary<FilterSignature, IndexableSet<int>>();
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
				filterSignatureToEntityIDs.Add(filterSignature, new IndexableSet<int>());

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

		public IEnumerable<Entity> FilterEntities(FilterSignature filterSignature)
		{
			foreach (var id in filterSignatureToEntityIDs[filterSignature])
			{
				yield return new Entity(id);
			}
		}

		public IEnumerable<Entity> FilterEntitiesRandom(FilterSignature filterSignature)
		{
			foreach (var index in RandomGenerator.LinearCongruentialGenerator(FilterCount(filterSignature)))
			{
				yield return new Entity(filterSignatureToEntityIDs[filterSignature][index]);
			}
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
			if (typeToFilterSignatures.ContainsKey(componentTypeIndex))
			{
				foreach (var filterSignature in typeToFilterSignatures[componentTypeIndex])
				{
					CheckFilter(entityID, filterSignature);
				}
			}
		}

		public void Check<TComponent>(int entityID) where TComponent : unmanaged
		{
			Check(entityID, ComponentTypeIndices.GetIndex<TComponent>());
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
			if (typeToFilterSignatures.ContainsKey(componentTypeIndex))
			{
				foreach (var filterSignature in typeToFilterSignatures[componentTypeIndex])
				{
					filterSignatureToEntityIDs[filterSignature].Remove(entityID);
				}
			}
		}
	}
}
