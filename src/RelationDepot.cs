using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace MoonTools.ECS
{
	internal class RelationDepot
	{
		private TypeIndices RelationTypeIndices;
		private RelationStorage[] storages = new RelationStorage[256];

		public RelationDepot(TypeIndices relationTypeIndices)
		{
			RelationTypeIndices = relationTypeIndices;
		}

		private void Register<TRelationKind>(int index) where TRelationKind : unmanaged
		{
			if (index >= storages.Length)
			{
				Array.Resize(ref storages, storages.Length * 2);
			}

			storages[index] = new RelationStorage<TRelationKind>();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private RelationStorage<TRelationKind> Lookup<TRelationKind>() where TRelationKind : unmanaged
		{
			var storageIndex = RelationTypeIndices.GetIndex<TRelationKind>();
			// TODO: is there some way to avoid this null check?
			if (storages[storageIndex] == null)
			{
				Register<TRelationKind>(storageIndex);
			}
			return (RelationStorage<TRelationKind>) storages[storageIndex];
		}

		public void Set<TRelationKind>(Relation relation, TRelationKind relationData) where TRelationKind : unmanaged
		{
			Lookup<TRelationKind>().Set(relation, relationData);
		}

		public (bool, bool) Remove<TRelationKind>(Relation relation) where TRelationKind : unmanaged
		{
			return Lookup<TRelationKind>().Remove(relation);
		}

		public void UnrelateAll<TRelationKind>(int entityID) where TRelationKind : unmanaged
		{
			Lookup<TRelationKind>().UnrelateAll(entityID);
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

		// untyped methods used for destroying and snapshots

		public void Set(int entityA, int entityB, int relationTypeIndex, object relationData)
		{
			storages[relationTypeIndex].Set(entityA, entityB, relationData);
		}

		public void UnrelateAll(int entityID, int relationTypeIndex)
		{
			storages[relationTypeIndex].UnrelateAll(entityID);
		}

		public IEnumerable<(int, object)> InRelations(int entityID, int relationTypeIndex)
		{
			return storages[relationTypeIndex].UntypedInRelations(entityID);
		}

		public IEnumerable<(int, object)> OutRelations(int entityID, int relationTypeIndex)
		{
			return storages[relationTypeIndex].UntypedOutRelations(entityID);
		}

		public void Clear()
		{
			for (var i = 0; i < RelationTypeIndices.Count; i += 1)
			{
				if (storages[i] != null)
				{
					storages[i].Clear();
				}
			}
		}

		public void CreateMissingStorages(RelationDepot other)
		{
			for (var i = 0; i < RelationTypeIndices.Count; i += 1)
			{
				if (storages[i] == null && other.storages[i] != null)
				{
					storages[i] = other.storages[i].CreateStorage();
				}
			}
		}
	}
}
