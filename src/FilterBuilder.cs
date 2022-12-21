using System;
using System.Collections.Generic;

namespace MoonTools.ECS
{
	public struct FilterBuilder
	{
		private TypeIndices ComponentTypeIndices;
		private TypeIndices RelationTypeIndices;
		private FilterStorage FilterStorage;
		private HashSet<int> Included;
		private HashSet<int> Excluded;
		private HashSet<int> InRelations;
		private HashSet<int> OutRelations;

		internal FilterBuilder(
			FilterStorage filterStorage,
			TypeIndices componentTypeIndices,
			TypeIndices relationTypeIndices
		) {
			FilterStorage = filterStorage;
			ComponentTypeIndices = componentTypeIndices;
			RelationTypeIndices = relationTypeIndices;
			Included = new HashSet<int>();
			Excluded = new HashSet<int>();
			InRelations = new HashSet<int>();
			OutRelations = new HashSet<int>();
		}

		private FilterBuilder(
			FilterStorage filterStorage,
			TypeIndices componentTypeIndices,
			TypeIndices relationTypeIndices,
			HashSet<int> included,
			HashSet<int> excluded,
			HashSet<int> inRelations,
			HashSet<int> outRelations
		) {
			FilterStorage = filterStorage;
			ComponentTypeIndices = componentTypeIndices;
			RelationTypeIndices = relationTypeIndices;
			Included = included;
			Excluded = excluded;
			InRelations = inRelations;
			OutRelations = outRelations;
		}

		public FilterBuilder Include<TComponent>() where TComponent : unmanaged
		{
			Included.Add(ComponentTypeIndices.GetIndex<TComponent>());
			return this;
		}

		public FilterBuilder Exclude<TComponent>() where TComponent : unmanaged
		{
			Excluded.Add(ComponentTypeIndices.GetIndex<TComponent>());
			return this;
		}

		public FilterBuilder WithInRelation<TRelation>() where TRelation : unmanaged
		{
			InRelations.Add(RelationTypeIndices.GetIndex<TRelation>());
			return this;
		}

		public FilterBuilder WithOutRelation<TRelation>() where TRelation : unmanaged
		{
			OutRelations.Add(RelationTypeIndices.GetIndex<TRelation>());
			return this;
		}

		public Filter Build()
		{
			return FilterStorage.CreateFilter(Included, Excluded, InRelations, OutRelations);
		}
	}
}
