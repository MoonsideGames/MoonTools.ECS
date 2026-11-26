using System;
using System.Collections.Generic;

namespace MoonTools.ECS;

internal class IdAssigner
{
	uint Next;
	SortedSet<uint> AvailableIds = [];

	public uint Assign()
	{
		if (AvailableIds.Count > 0)
		{
			var min = AvailableIds.Min;
			AvailableIds.Remove(min);
			return min;
		}

		var id = Next;
		Next += 1;
		return id;
	}

	public void Unassign(uint id)
	{
		AvailableIds.Add(id);
	}

	public void CopyTo(IdAssigner other)
	{
		// TODO: is this slow?
		other.AvailableIds.Clear();
		other.AvailableIds.UnionWith(AvailableIds);
		other.Next = Next;
	}
}
