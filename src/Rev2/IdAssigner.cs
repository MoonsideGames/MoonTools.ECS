using MoonTools.ECS.Collections;

namespace MoonTools.ECS.Rev2;

internal class IdAssigner
{
	uint Next;
	NativeArray<uint> AvailableIds = new NativeArray<uint>();

	public uint Assign()
	{
		if (!AvailableIds.TryPop(out var id))
		{
			id = Next;
			Next += 1;
		}

		return id;
	}

	public void Unassign(uint id)
	{
		AvailableIds.Add(id);
	}

	public void CopyTo(IdAssigner other)
	{
		AvailableIds.CopyTo(other.AvailableIds);
		other.Next = Next;
	}
}
