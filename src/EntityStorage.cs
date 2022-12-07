using System.Collections.Generic;

namespace MoonTools.ECS
{
	internal class EntityStorage
	{
		private int nextID = 0;
		// FIXME: why is this duplicated?
		private readonly Stack<int> availableIDs = new Stack<int>();
		// FIXME: this is only needed in debug mode
		private readonly HashSet<int> availableIDHash = new HashSet<int>();

		private Dictionary<int, HashSet<int>> EntityToComponentTypeIndices = new Dictionary<int, HashSet<int>>();
		private Dictionary<int, HashSet<int>> EntityToRelationTypeIndices = new Dictionary<int, HashSet<int>>();

		public int Count => nextID - availableIDs.Count;

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
		public bool SetComponent(int entityID, int componentTypeIndex)
		{
			return EntityToComponentTypeIndices[entityID].Add(componentTypeIndex);
		}

		public bool HasComponent(int entityID, int componentTypeIndex)
		{
			return EntityToComponentTypeIndices[entityID].Contains(componentTypeIndex);
		}

		// Returns true if the component existed.
		public bool RemoveComponent(int entityID, int componentTypeIndex)
		{
			return EntityToComponentTypeIndices[entityID].Remove(componentTypeIndex);
		}

		public void AddRelationKind(int entityID, int relationIndex)
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

		public void Clear()
		{
			nextID = 0;
			foreach (var componentSet in EntityToComponentTypeIndices.Values)
			{
				componentSet.Clear();
			}
			foreach (var relationSet in EntityToRelationTypeIndices.Values)
			{
				relationSet.Clear();
			}
			availableIDs.Clear();
			availableIDHash.Clear();
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
