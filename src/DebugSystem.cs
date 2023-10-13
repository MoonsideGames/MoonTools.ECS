// NOTE: these methods are very inefficient
// this class should only be used in debugging contexts!!

#if DEBUG
using System;
using System.Collections.Generic;

namespace MoonTools.ECS
{
	public abstract class DebugSystem : System
	{
		protected DebugSystem(World world) : base(world)
		{
		}

		protected ComponentEnumerator Debug_GetAllComponents(Entity entity)
		{
			return new ComponentEnumerator(ComponentDepot, entity, EntityStorage.ComponentTypeIndices(entity.ID));
		}

		protected IEnumerable<Entity> Debug_GetEntities(Type componentType)
		{
			foreach (var entityID in ComponentDepot.Debug_GetEntityIDs(ComponentTypeIndices.GetIndex(componentType)))
			{
				yield return new Entity(entityID);
			}
		}

		protected IEnumerable<Type> Debug_SearchComponentType(string typeString)
		{
			foreach (var type in ComponentTypeIndices.Types)
			{
				if (type.ToString().ToLower().Contains(typeString.ToLower()))
				{
					yield return type;
				}
			}
		}

		public ref struct ComponentEnumerator
		{
			private ComponentDepot ComponentDepot;
			private Entity Entity;
			private ReverseSpanEnumerator<int> ComponentTypeIndices;

			public ComponentEnumerator GetEnumerator() => this;

			internal ComponentEnumerator(
				ComponentDepot componentDepot,
				Entity entity,
				Collections.IndexableSet<int> componentTypeIndices
			) {
				ComponentDepot = componentDepot;
				Entity = entity;
				ComponentTypeIndices = componentTypeIndices.GetEnumerator();
			}

			public bool MoveNext() => ComponentTypeIndices.MoveNext();
			public object Current => ComponentDepot.Debug_Get(Entity.ID, ComponentTypeIndices.Current);
		}
	}
}
#endif
