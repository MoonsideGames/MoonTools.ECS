using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace MoonTools.ECS
{
	internal class ComponentDepot
	{
		private Dictionary<Type, ComponentStorage> storages = new Dictionary<Type, ComponentStorage>();

		private Dictionary<FilterSignature, IndexableSet<int>> filterSignatureToEntityIDs = new Dictionary<FilterSignature, IndexableSet<int>>();

		private Dictionary<Type, HashSet<FilterSignature>> typeToFilterSignatures = new Dictionary<Type, HashSet<FilterSignature>>();

#if DEBUG
		private Dictionary<Type, Filter> singleComponentFilters = new Dictionary<Type, Filter>();
#endif

		private HashSet<Type> TypesWithDisabledSerialization = new HashSet<Type>();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void Register<TComponent>() where TComponent : unmanaged
		{
			if (!storages.ContainsKey(typeof(TComponent)))
			{
				storages.Add(typeof(TComponent), new ComponentStorage<TComponent>());
#if DEBUG
				singleComponentFilters.Add(typeof(TComponent), CreateFilter(new HashSet<Type>() { typeof(TComponent) }, new HashSet<Type>()));
#endif
			}
		}

		private ComponentStorage Lookup(Type type)
		{
			return storages[type];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private ComponentStorage<TComponent> Lookup<TComponent>() where TComponent : unmanaged
		{
			// TODO: is it possible to optimize this?
			Register<TComponent>();
			return (ComponentStorage<TComponent>) storages[typeof(TComponent)];
		}

		public bool Some<TComponent>() where TComponent : unmanaged
		{
			return Lookup<TComponent>().Any();
		}

		public bool Has<TComponent>(int entityID) where TComponent : unmanaged
		{
			return Lookup<TComponent>().Has(entityID);
		}

		private bool Has(Type type, int entityID)
		{
			return Lookup(type).Has(entityID);
		}

		public ref readonly TComponent Get<TComponent>(int entityID) where TComponent : unmanaged
		{
			return ref Lookup<TComponent>().Get(entityID);
		}

		public ref readonly TComponent Get<TComponent>() where TComponent : unmanaged
		{
			return ref Lookup<TComponent>().Get();
		}

		public void Set<TComponent>(int entityID, in TComponent component) where TComponent : unmanaged
		{
			var existed = Lookup<TComponent>().Set(entityID, component);

			// update filters
			if (!existed)
			{
				if (typeToFilterSignatures.TryGetValue(typeof(TComponent), out var filterSignatures))
				{
					foreach (var filterSignature in filterSignatures)
					{
						CheckFilter(filterSignature, entityID);
					}
				}
			}
		}

		public Entity GetSingletonEntity<TComponent>() where TComponent : unmanaged
		{
			return Lookup<TComponent>().FirstEntity();
		}

		public ReadOnlySpan<TComponent> ReadComponents<TComponent>() where TComponent : unmanaged
		{
			return Lookup<TComponent>().AllComponents();
		}

		private void Remove(Type type, int entityID)
		{
			var existed = Lookup(type).Remove(entityID);

			// update filters
			if (existed)
			{
				if (typeToFilterSignatures.TryGetValue(type, out var filterSignatures))
				{
					foreach (var filterSignature in filterSignatures)
					{
						CheckFilter(filterSignature, entityID);
					}
				}
			}
		}

		public void Remove<TComponent>(int entityID) where TComponent : unmanaged
		{
			var existed = Lookup<TComponent>().Remove(entityID);

			// update filters
			if (existed)
			{
				if (typeToFilterSignatures.TryGetValue(typeof(TComponent), out var filterSignatures))
				{
					foreach (var filterSignature in filterSignatures)
					{
						CheckFilter(filterSignature, entityID);
					}
				}
			}
		}

		// TODO: is there some way to optimize this without complicating serialization?
		public void OnEntityDestroy(int entityID)
		{
			foreach (var type in storages.Keys)
			{
				Remove(type, entityID);
			}
		}

		public Filter CreateFilter(HashSet<Type> included, HashSet<Type> excluded)
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

		// FIXME: this dictionary should probably just store entities
		public IEnumerable<Entity> FilterEntities(Filter filter)
		{
			foreach (var id in filterSignatureToEntityIDs[filter.Signature])
			{
				yield return new Entity(id);
			}
		}

		public IEnumerable<Entity> FilterEntitiesRandom(Filter filter)
		{
			foreach (var index in RandomGenerator.LinearCongruentialGenerator(FilterCount(filter)))
			{
				yield return new Entity(filterSignatureToEntityIDs[filter.Signature][index]);
			}
		}

		public Entity FilterNthEntity(Filter filter, int index)
		{
			return new Entity(filterSignatureToEntityIDs[filter.Signature][index]);
		}

		public Entity FilterRandomEntity(Filter filter)
		{
			var randomIndex = RandomGenerator.Next(FilterCount(filter));
			return new Entity(filterSignatureToEntityIDs[filter.Signature][randomIndex]);
		}

		public int FilterCount(Filter filter)
		{
			return filterSignatureToEntityIDs[filter.Signature].Count;
		}

		private void CheckFilter(FilterSignature filterSignature, int entityID)
		{
			foreach (var type in filterSignature.Included)
			{
				if (!Has(type, entityID))
				{
					filterSignatureToEntityIDs[filterSignature].Remove(entityID);
					return;
				}
			}

			foreach (var type in filterSignature.Excluded)
			{
				if (Has(type, entityID))
				{
					filterSignatureToEntityIDs[filterSignature].Remove(entityID);
					return;
				}
			}

			filterSignatureToEntityIDs[filterSignature].Add(entityID);
		}

		public void DisableSerialization<TComponent>() where TComponent : unmanaged
		{
			TypesWithDisabledSerialization.Add(typeof(TComponent));
		}

		public void Save(ComponentDepotState state)
		{
			foreach (var (type, storage) in storages)
			{
				if (!TypesWithDisabledSerialization.Contains(type))
				{
					if (!state.StorageStates.ContainsKey(type))
					{
						state.StorageStates.Add(type, storage.CreateState());
					}

					storage.Save(state.StorageStates[type]);
				}
			}

			foreach (var (signature, set) in filterSignatureToEntityIDs)
			{
				// FIXME: we could cache this
				if (!signature.Included.Overlaps(TypesWithDisabledSerialization) && !signature.Excluded.Overlaps(TypesWithDisabledSerialization))
				{
					if (!state.FilterStates.ContainsKey(signature))
					{
						state.FilterStates[signature] = new IndexableSetState<int>(set.Count);
					}
					set.Save(state.FilterStates[signature]);
				}
			}
		}

		public void Load(ComponentDepotState state)
		{
			foreach (var (type, storageState) in state.StorageStates)
			{
				storages[type].Load(storageState);
			}

			foreach (var (signature, setState) in state.FilterStates)
			{
				filterSignatureToEntityIDs[signature].Load(setState);
			}
		}

#if DEBUG
		public IEnumerable<object> Debug_GetAllComponents(int entityID)
		{
			foreach (var (type, storage) in storages)
			{
				if (storage.Has(entityID))
				{
					yield return storage.Debug_Get(entityID);
				}
			}
		}

		public IEnumerable<Entity> Debug_GetEntities(Type componentType)
		{
			return singleComponentFilters[componentType].Entities;
		}

		public IEnumerable<Type> Debug_SearchComponentType(string typeString)
		{
			foreach (var type in storages.Keys)
			{
				if (type.ToString().ToLower().Contains(typeString.ToLower()))
				{
					yield return type;
				}
			}
		}
#endif
	}
}
