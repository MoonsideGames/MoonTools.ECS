using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
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

		public void UnrelateAll<TRelationKind>(int entityID) where TRelationKind : unmanaged
		{
			Lookup<TRelationKind>().UnrelateAll(entityID);
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

		public IEnumerable<(Entity, TRelationKind)> OutRelations<TRelationKind>(int entityID) where TRelationKind : unmanaged
		{
			return Lookup<TRelationKind>().OutRelations(entityID);
		}

		public (Entity, TRelationKind) OutRelationSingleton<TRelationKind>(int entityID) where TRelationKind : unmanaged
		{
			return Lookup<TRelationKind>().OutFirst(entityID);
		}

		public int OutRelationCount<TRelationKind>(int entityID) where TRelationKind : unmanaged
		{
			return Lookup<TRelationKind>().OutRelationCount(entityID);
		}

		public bool HasOutRelation<TRelationKind>(int entityID) where TRelationKind : unmanaged
		{
			return Lookup<TRelationKind>().HasOutRelation(entityID);
		}

		public IEnumerable<(Entity, TRelationKind)> InRelations<TRelationKind>(int entityID) where TRelationKind : unmanaged
		{
			return Lookup<TRelationKind>().InRelations(entityID);
		}

		public (Entity, TRelationKind) InRelationSingleton<TRelationKind>(int entityID) where TRelationKind : unmanaged
		{
			return Lookup<TRelationKind>().InFirst(entityID);
		}

		public bool HasInRelation<TRelationKind>(int entityID) where TRelationKind : unmanaged
		{
			return Lookup<TRelationKind>().HasInRelation(entityID);
		}

		public int InRelationCount<TRelationKind>(int entityID) where TRelationKind : unmanaged
		{
			return Lookup<TRelationKind>().InRelationCount(entityID);
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
