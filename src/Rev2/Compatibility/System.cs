using System;

namespace MoonTools.ECS.Rev2.Compatibility;

public class System : Manipulator
{
	public System(World world) : base(world) { }

	protected ReadOnlySpan<T> ReadMessages<T>() where T : unmanaged => World.ReadMessages<T>();
	protected T ReadMessage<T>() where T : unmanaged => World.ReadMessage<T>();
	protected bool SomeMessage<T>() where T : unmanaged => World.SomeMessage<T>();
	protected void Send<T>(T message) where T : unmanaged => World.Send(message);
}
