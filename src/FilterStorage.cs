using System;
using System.Collections.Generic;
using MoonTools.ECS.Collections;

namespace MoonTools.ECS
{
	internal class FilterStorage
	{
		private EntityStorage EntityStorage;
		private TypeIndices ComponentTypeIndices;
		private Dictionary<FilterSignature, IndexableSet<Entity>> filterSignatureToEntityIDs = new Dictionary<FilterSignature, IndexableSet<Entity>>();
		private Dictionary<int, List<FilterSignature>> typeToFilterSignatures = new Dictionary<int, List<FilterSignature>>();

		private Dictionary<FilterSignature, Action<Entity>> addCallbacks = new Dictionary<FilterSignature, Action<Entity>>();
		private Dictionary<FilterSignature, Action<Entity>> removeCallbacks = new Dictionary<FilterSignature, Action<Entity>>();

		public FilterStorage(EntityStorage entityStorage, TypeIndices componentTypeIndices)
		{
			EntityStorage = entityStorage;
			ComponentTypeIndices = componentTypeIndices;
		}

		private void CopyTypeCache(Dictionary<int, List<FilterSignature>> typeCache)
		{
			foreach (var type in typeCache.Keys)
			{
				if (!typeToFilterSignatures.ContainsKey(type))
				{
					typeToFilterSignatures.Add(type, new List<FilterSignature>());

					foreach (var signature in typeCache[type])
					{
						typeToFilterSignatures[type].Add(signature);
					}
				}
			}
		}

		public void CreateMissingStorages(FilterStorage other)
		{
			foreach (var filterSignature in other.filterSignatureToEntityIDs.Keys)
			{
				if (!filterSignatureToEntityIDs.ContainsKey(filterSignature))
				{
					filterSignatureToEntityIDs.Add(filterSignature, new IndexableSet<Entity>());
				}
			}

			CopyTypeCache(other.typeToFilterSignatures);
		}

		public Filter CreateFilter(IndexableSet<int> included, IndexableSet<int> excluded)
		{
			var filterSignature = new FilterSignature(included, excluded);
			if (!filterSignatureToEntityIDs.ContainsKey(filterSignature))
			{
				filterSignatureToEntityIDs.Add(filterSignature, new IndexableSet<Entity>());

				foreach (var type in included)
				{
					if (!typeToFilterSignatures.ContainsKey(type))
					{
						typeToFilterSignatures.Add(type, new List<FilterSignature>());
					}

					typeToFilterSignatures[type].Add(filterSignature);
				}

				foreach (var type in excluded)
				{
					if (!typeToFilterSignatures.ContainsKey(type))
					{
						typeToFilterSignatures.Add(type, new List<FilterSignature>());
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
				RandomManager.LinearCongruentialSequence(FilterCount(filterSignature)));
		}

		public Entity FilterNthEntity(FilterSignature filterSignature, int index)
		{
			return new Entity(filterSignatureToEntityIDs[filterSignature][index]);
		}

		public Entity FilterRandomEntity(FilterSignature filterSignature)
		{
			var randomIndex = RandomManager.Next(FilterCount(filterSignature));
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

			if (filterSignatureToEntityIDs[filterSignature].Add(entityID))
			{
				if (addCallbacks.TryGetValue(filterSignature, out var addCallback))
				{
					addCallback(entityID);
				}
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

		// Used by TransferEntity
		public void AddEntity(FilterSignature signature, int entityID)
		{
			filterSignatureToEntityIDs[signature].Add(entityID);
		}

		public void TransferStorage(Dictionary<int, int> worldToTransferID, FilterStorage other)
		{
			foreach (var (filterSignature, entityIDs) in filterSignatureToEntityIDs)
			{
				foreach (var entity in entityIDs)
				{
					if (worldToTransferID.ContainsKey(entity))
					{
						var otherEntityID = worldToTransferID[entity];
						other.AddEntity(filterSignature, otherEntityID);

						if (other.addCallbacks.TryGetValue(filterSignature, out var addCallback))
						{
							addCallback(otherEntityID);
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
