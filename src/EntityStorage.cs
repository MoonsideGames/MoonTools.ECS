using System.Collections.Generic;

namespace MoonTools.ECS
{
	internal class EntityStorage
	{
		private int nextID = 0;
		// FIXME: why is this duplicated?
		private readonly Stack<int> availableIDs = new Stack<int>();
		private readonly HashSet<int> availableIDHash = new HashSet<int>();

		private Dictionary<int, HashSet<int>> EntityToComponentTypeIndices = new Dictionary<int, HashSet<int>>();
		private Dictionary<int, HashSet<int>> EntityToRelationTypeIndices = new Dictionary<int, HashSet<int>>();

		public Entity Create()
		{
			var entity = new Entity(NextID());
			EntityToComponentTypeIndices.TryAdd(entity.ID, new HashSet<int>());
			EntityToRelationTypeIndices.TryAdd(entity.ID, new HashSet<int>());
			return entity;
		}

		public bool Exists(in Entity entity)
		{
			return Taken(entity.ID);
		}

		public void Destroy(in Entity entity)
		{
			EntityToComponentTypeIndices[entity.ID].Clear();
			EntityToRelationTypeIndices[entity.ID].Clear();
			Release(entity.ID);
		}

		// Returns true if the component is new.
		public bool SetComponent(int entityID, int storageIndex)
		{
			return EntityToComponentTypeIndices[entityID].Add(storageIndex);
		}

		public bool HasComponent(int entityID, int storageIndex)
		{
			return EntityToComponentTypeIndices[entityID].Contains(storageIndex);
		}

		// Returns true if the component existed.
		public bool RemoveComponent(int entityID, int storageIndex)
		{
			return EntityToComponentTypeIndices[entityID].Remove(storageIndex);
		}

		public void AddRelation(int entityID, int relationIndex)
		{
			EntityToRelationTypeIndices[entityID].Add(relationIndex);
		}

		public void RemoveRelation(int entityId, int relationIndex)
		{
			EntityToRelationTypeIndices[entityId].Remove(relationIndex);
		}

		// TODO: should these ints be ID types?
		public IEnumerable<int> ComponentTypeIndices(int entityID)
		{
			return EntityToComponentTypeIndices[entityID];
		}

		public IEnumerable<int> RelationTypeIndices(int entityID)
		{
			return EntityToRelationTypeIndices[entityID];
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
