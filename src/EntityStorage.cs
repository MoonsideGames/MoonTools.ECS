using System.Collections.Generic;

namespace MoonTools.ECS
{
	internal class EntityStorage
	{
		private int nextID = 0;
		private readonly Stack<int> availableIDs = new Stack<int>();
		private readonly HashSet<int> availableIDHash = new HashSet<int>();

		public Entity Create()
		{
			return new Entity(NextID());
		}

		public bool Exists(in Entity entity)
		{
			return Taken(entity.ID);
		}

		public void Destroy(in Entity entity)
		{
			Release(entity.ID);
		}

		public void Save(EntityStorageState state)
		{
			state.NextID = nextID;
			state.availableIDs.Clear();
			foreach (var id in availableIDs)
			{
				state.availableIDs.Add(id);
			}
		}

		public void Load(EntityStorageState state)
		{
			nextID = state.NextID;
			availableIDs.Clear();
			availableIDHash.Clear();
			foreach (var id in state.availableIDs)
			{
				availableIDs.Push(id);
				availableIDHash.Add(id);
			}
		}

		private int NextID()
		{
			if (availableIDs.Count > 0)
			{
				var id = availableIDs.Pop();
				availableIDHash.Remove(id);
				return id;
			}
			else
			{
				var id = nextID;
				nextID += 1;
				return id;
			}
		}

		private bool Taken(int id)
		{
			return !availableIDHash.Contains(id) && id < nextID;
		}

		private void Release(int id)
		{
			availableIDs.Push(id);
			availableIDHash.Add(id);
		}
	}
}
