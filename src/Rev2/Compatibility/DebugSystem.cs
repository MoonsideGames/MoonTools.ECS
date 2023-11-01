// NOTE: these methods are very inefficient
// this class should only be used in debugging contexts!!
#if DEBUG
using System;
using System.Collections.Generic;

namespace MoonTools.ECS.Rev2.Compatibility;

public abstract class DebugSystem : System
{
	protected DebugSystem(World world) : base(world) { }

	protected World.ComponentEnumerator Debug_GetAllComponents(EntityId entity) => World.Debug_GetAllComponents(entity);
	protected Filter.EntityEnumerator Debug_GetEntities(Type componentType) => World.Debug_GetEntities(componentType);
	protected IEnumerable<Type> Debug_SearchComponentType(string typeString) => World.Debug_SearchComponentType(typeString);
}
#endif
