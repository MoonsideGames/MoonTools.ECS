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

		protected IEnumerable<dynamic> Debug_GetAllComponents(Entity entity)
		{
			foreach (var typeIndex in EntityStorage.ComponentTypeIndices(entity.ID))
			{
				yield return ComponentDepot.Debug_Get(entity.ID, typeIndex);
			}
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
	}
}
#endif
