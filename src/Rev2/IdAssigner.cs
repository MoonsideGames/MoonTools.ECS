using System.Collections.Generic;

namespace MoonTools.ECS.Rev2
{
	public class IdAssigner<T> where T : struct, IHasId
	{
		int Next;
		Queue<int> AvailableIds = new Queue<int>();

		public T Assign()
		{
			if (!AvailableIds.TryDequeue(out var id))
			{
				id = Next;
				Next += 1;
			}

			return new T { Id = id };
		}

		public void Unassign(T idHaver)
		{
			AvailableIds.Enqueue(idHaver.Id);
		}
	}
}
