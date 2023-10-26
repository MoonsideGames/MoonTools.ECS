using System.Collections.Generic;

namespace MoonTools.ECS.Rev2
{
	internal class IdAssigner
	{
		ulong Next;
		Queue<ulong> AvailableIds = new Queue<ulong>();

		public Id Assign()
		{
			if (!AvailableIds.TryDequeue(out var id))
			{
				id = Next;
				Next += 1;
			}

			return new Id(id);
		}

		public void Unassign(Id id)
		{
			AvailableIds.Enqueue(id.Value);
		}
	}
}
