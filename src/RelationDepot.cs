using System;
using System.Collections.Generic;

namespace MoonTools.ECS
{
	internal class RelationDepot
	{
		private Dictionary<Type, RelationStorage> storages = new Dictionary<Type, RelationStorage>();

		private void Register<TRelationKind>() where TRelationKind : struct
		{
			if (!storages.ContainsKey(typeof(TRelationKind)))
			{
				storages.Add(typeof(TRelationKind), new RelationStorage<TRelationKind>());
			}
		}

		private RelationStorage<TRelationKind> Lookup<TRelationKind>() where TRelationKind : struct
		{
			Register<TRelationKind>();
			return (RelationStorage<TRelationKind>) storages[typeof(TRelationKind)];
		}

		public void Add<TRelationKind>(Relation relation, TRelationKind relationData) where TRelationKind : struct
		{
			Lookup<TRelationKind>().Add(relation, relationData);
		}

		public void Remove<TRelationKind>(Relation relation) where TRelationKind : struct
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

		public IEnumerable<(Entity, Entity, TRelationKind)> Relations<TRelationKind>() where TRelationKind : struct
		{
			return Lookup<TRelationKind>().All();
		}

		public bool Related<TRelationKind>(int idA, int idB) where TRelationKind : struct
		{
			return Lookup<TRelationKind>().Has(new Relation(idA, idB));
		}

		public IEnumerable<(Entity, TRelationKind)> RelatedToA<TRelationKind>(int entityID) where TRelationKind : struct
		{
			return Lookup<TRelationKind>().RelatedToA(entityID);
		}

		public IEnumerable<(Entity, TRelationKind)> RelatedToB<TRelationKind>(int entityID) where TRelationKind : struct
		{
			return Lookup<TRelationKind>().RelatedToB(entityID);
		}
	}
}
