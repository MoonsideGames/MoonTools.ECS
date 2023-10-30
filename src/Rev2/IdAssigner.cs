using MoonTools.ECS.Collections;

namespace MoonTools.ECS.Rev2;

internal class IdAssigner
{
	ulong Next;
	NativeArray<ulong> AvailableIds = new NativeArray<ulong>();

	public Id Assign()
	{
		if (!AvailableIds.TryPop(out var id))
		{
			id = Next;
			Next += 1;
		}

		return new Id(id);
	}

	public void Unassign(Id id)
	{
		AvailableIds.Add(id.Value);
	}

	public void CopyTo(IdAssigner other)
	{
		AvailableIds.CopyTo(other.AvailableIds);
		other.Next = Next;
	}
}
