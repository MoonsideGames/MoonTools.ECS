namespace MoonTools.ECS
{
	internal class RelationDepot
	{
		private Dictionary<Type, RelationStorage> storages = new Dictionary<Type, RelationStorage>();

		private RelationStorage Lookup<TRelationKind>()
		{
			return storages[typeof(TRelationKind)];
		}

		public void Register<TRelationKind>()
		{
			storages[typeof(TRelationKind)] = new RelationStorage();
		}

		public void Add<TRelationKind>(Relation relation)
		{
			Lookup<TRelationKind>().Add(relation);
		}

		public void Remove<TRelationKind>(Relation relation)
		{
			Lookup<TRelationKind>().Remove(relation);
		}

		public void OnEntityDestroy(int entityID)
		{
			foreach (var storage in storages.Values)
			{
				storage.OnEntityDestroy(entityID);
			}
		}

		public IEnumerable<Relation> Relations<TRelationKind>()
		{
			return Lookup<TRelationKind>().All();
		}

		public bool Related<TRelationKind>(int idA, int idB)
		{
			return Lookup<TRelationKind>().Has(new Relation(idA, idB));
		}

		public IEnumerable<Entity> RelatedToA<TRelationKind>(int entityID)
		{
			return Lookup<TRelationKind>().RelatedToA(entityID);
		}

		public IEnumerable<Entity> RelatedToB<TRelationKind>(int entityID)
		{
			return Lookup<TRelationKind>().RelatedToB(entityID);
		}
	}
}
