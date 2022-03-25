// NOTE: these methods are very inefficient
// this class should only be used in debugging contexts!!
namespace MoonTools.ECS
{
	public abstract class DebugSystem : System
	{
		protected DebugSystem(World world) : base(world)
		{
		}

		protected IEnumerable<object> Debug_GetAllComponents(Entity entity)
		{
			return ComponentDepot.Debug_GetAllComponents(entity.ID);
		}

		protected ReadOnlySpan<Entity> Debug_GetEntities(Type componentType)
		{
			return ComponentDepot.Debug_GetEntities(componentType);
		}

		protected IEnumerable<Type> Debug_SearchComponentType(string typeString)
		{
			return ComponentDepot.Debug_SearchComponentType(typeString);
		}
	}
}
