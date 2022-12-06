// NOTE: these methods are very inefficient
// this class should only be used in debugging contexts!!

#if DEBUG
using System;
using System.Collections.Generic;

namespace MoonTools.ECS
{
	public abstract class DebugSystem : System
	{
		private Dictionary<Type, Filter> singleComponentFilters = new Dictionary<Type, Filter>();

		protected DebugSystem(World world) : base(world)
		{
		}

		protected IEnumerable<object> Debug_GetAllComponents(Entity entity)
		{
			foreach (var typeIndex in EntityStorage.ComponentTypeIndices(entity.ID))
			{
				yield return ComponentDepot.UntypedGet(entity.ID, typeIndex);
			}
		}

		protected IEnumerable<Entity> Debug_GetEntities(Type componentType)
		{
			if (!singleComponentFilters.ContainsKey(componentType))
			{
				singleComponentFilters.Add(componentType, new Filter(FilterStorage, new HashSet<int>(ComponentTypeIndices.GetIndex(componentType)), new HashSet<int>()));
			}
			return singleComponentFilters[componentType].Entities;
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
	}
}
#endif
