﻿using System;

namespace MoonTools.ECS.Rev2.Compatibility;

public abstract class System : Manipulator
{
	public FilterBuilder FilterBuilder => World.FilterBuilder;

	protected System(World world) : base(world) { }

	public abstract void Update();

	protected ReadOnlySpan<T> ReadMessages<T>() where T : unmanaged => World.ReadMessages<T>();
	protected T ReadMessage<T>() where T : unmanaged => World.ReadMessage<T>();
	protected bool SomeMessage<T>() where T : unmanaged => World.SomeMessage<T>();
	protected void Send<T>(T message) where T : unmanaged => World.Send(message);
}
