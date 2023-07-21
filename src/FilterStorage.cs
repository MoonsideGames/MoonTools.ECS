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

		private Dictionary<FilterSignature, Action<Entity>> addCallbacks = new Dictionary<FilterSignature, Action<Entity>>();
		private Dictionary<FilterSignature, Action<Entity>> removeCallbacks = new Dictionary<FilterSignature, Action<Entity>>();

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

		public RandomEntityEnumerator FilterEntitiesRandom(FilterSignature filterSignature)
		{
			return new RandomEntityEnumerator(
				this,
				filterSignature,
				RandomGenerator.LinearCongruentialGenerator(FilterCount(filterSignature)));
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
					if (filterSignatureToEntityIDs[filterSignature].Remove(entityID))
					{
						if (removeCallbacks.TryGetValue(filterSignature, out var removeCallback))
						{
							removeCallback(entityID);
						}
					}
					return;
				}
			}

			foreach (var type in filterSignature.Excluded)
			{
				if (EntityStorage.HasComponent(entityID, type))
				{
					if (filterSignatureToEntityIDs[filterSignature].Remove(entityID))
					{
						if (removeCallbacks.TryGetValue(filterSignature, out var removeCallback))
						{
							removeCallback(entityID);
						}
					}
					return;
				}
			}

			filterSignatureToEntityIDs[filterSignature].Add(entityID);
			if (addCallbacks.TryGetValue(filterSignature, out var addCallback))
			{
				addCallback(entityID);
			}
		}

		public void RemoveEntity(int entityID, int componentTypeIndex)
		{
			if (typeToFilterSignatures.TryGetValue(componentTypeIndex, out var filterSignatures))
			{
				foreach (var filterSignature in filterSignatures)
				{
					if (filterSignatureToEntityIDs[filterSignature].Remove(entityID))
					{
						if (removeCallbacks.TryGetValue(filterSignature, out var removeCallback))
						{
							removeCallback(entityID);
						}
					}
				}
			}
		}

		// used by World.Clear, ignores callbacks
		public void Clear()
		{
			foreach (var (filterSignature, entityIDs) in filterSignatureToEntityIDs)
			{
				entityIDs.Clear();
			}
		}

		public void RegisterAddCallback(FilterSignature filterSignature, Action<Entity> callback)
		{
			addCallbacks.Add(filterSignature, callback);
		}

		public void RegisterRemoveCallback(FilterSignature filterSignature, Action<Entity> callback)
		{
			removeCallbacks.Add(filterSignature, callback);
		}
	}

	public ref struct RandomEntityEnumerator
	{
		public RandomEntityEnumerator GetEnumerator() => this;

		private FilterStorage FilterStorage;
		private FilterSignature FilterSignature;
		private LinearCongruentialEnumerator LinearCongruentialEnumerator;

		internal RandomEntityEnumerator(
			FilterStorage filterStorage,
			FilterSignature filterSignature,
			LinearCongruentialEnumerator linearCongruentialEnumerator)
		{
			FilterStorage = filterStorage;
			FilterSignature = filterSignature;
			LinearCongruentialEnumerator = linearCongruentialEnumerator;
		}

		public bool MoveNext() => LinearCongruentialEnumerator.MoveNext();
		public Entity Current => FilterStorage.FilterNthEntity(FilterSignature, LinearCongruentialEnumerator.Current);
	}
}
