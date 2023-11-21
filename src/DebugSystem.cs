// NOTE: these methods are very inefficient
// this class should only be used in debugging contexts!!
#if DEBUG
using System;
using System.Collections.Generic;

namespace MoonTools.ECS;

public abstract class DebugSystem : System
{
	protected DebugSystem(World world) : base(world) { }

	protected World.ComponentTypeEnumerator Debug_GetAllComponentTypes(Entity entity) => World.Debug_GetAllComponentTypes(entity);
	protected IEnumerable<Entity> Debug_GetEntities(Type componentType) => World.Debug_GetEntities(componentType);
	protected IEnumerable<Type> Debug_SearchComponentType(string typeString) => World.Debug_SearchComponentType(typeString);
}
#endif
