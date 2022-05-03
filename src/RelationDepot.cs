using System;
using System.Collections.Generic;

namespace MoonTools.ECS
{
	internal class RelationDepot
	{
		private Dictionary<Type, RelationStorage> storages = new Dictionary<Type, RelationStorage>();

		private void Register<TRelationKind>() where TRelationKind : unmanaged
		{
			if (!storages.ContainsKey(typeof(TRelationKind)))
			{
				storages.Add(typeof(TRelationKind), new RelationStorage<TRelationKind>());
			}
		}

		private RelationStorage<TRelationKind> Lookup<TRelationKind>() where TRelationKind : unmanaged
		{
			Register<TRelationKind>();
			return (RelationStorage<TRelationKind>) storages[typeof(TRelationKind)];
		}

		public void Set<TRelationKind>(Relation relation, TRelationKind relationData) where TRelationKind : unmanaged
		{
			Lookup<TRelationKind>().Set(relation, relationData);
		}

		public void Remove<TRelationKind>(Relation relation) where TRelationKind : unmanaged
		{
			Lookup<TRelationKind>().Remove(relation);
		}

		// FIXME: optimize this
		public void OnEntityDestroy(int entityID)
		{
			foreach (var storage in storages.Values)
			{
				storage.OnEntityDestroy(entityID);
			}
		}

		public IEnumerable<(Entity, Entity, TRelationKind)> Relations<TRelationKind>() where TRelationKind : unmanaged
		{
			return Lookup<TRelationKind>().All();
		}

		public bool Related<TRelationKind>(int idA, int idB) where TRelationKind : unmanaged
		{
			return Lookup<TRelationKind>().Has(new Relation(idA, idB));
		}

		public IEnumerable<(Entity, TRelationKind)> RelatedToA<TRelationKind>(int entityID) where TRelationKind : unmanaged
		{
			return Lookup<TRelationKind>().RelatedToA(entityID);
		}

		public IEnumerable<(Entity, TRelationKind)> RelatedToB<TRelationKind>(int entityID) where TRelationKind : unmanaged
		{
			return Lookup<TRelationKind>().RelatedToB(entityID);
		}

		public void Save(RelationDepotState state)
		{
			foreach (var (type, storage) in storages)
			{
				if (!state.StorageStates.ContainsKey(type))
				{
					state.StorageStates.Add(type, storage.CreateState());
				}

				storage.Save(state.StorageStates[type]);
			}
		}

		public void Load(RelationDepotState state)
		{
			foreach (var (type, storageState) in state.StorageStates)
			{
				storages[type].Load(storageState);
			}
		}
	}
}
