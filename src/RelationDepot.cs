using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace MoonTools.ECS
{
	internal class RelationDepot
	{
		private EntityStorage EntityStorage;
		private TypeIndices RelationTypeIndices;
		private RelationStorage[] storages = new RelationStorage[256];

		public RelationDepot(EntityStorage entityStorage, TypeIndices relationTypeIndices)
		{
			EntityStorage = entityStorage;
			RelationTypeIndices = relationTypeIndices;
		}

		private void Register<TRelationKind>(int index) where TRelationKind : unmanaged
		{
			if (index >= storages.Length)
			{
				Array.Resize(ref storages, storages.Length * 2);
			}

			storages[index] = new RelationStorage<TRelationKind>();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private RelationStorage<TRelationKind> Lookup<TRelationKind>() where TRelationKind : unmanaged
		{
			var storageIndex = RelationTypeIndices.GetIndex<TRelationKind>();
			// TODO: is there some way to avoid this null check?
			if (storages[storageIndex] == null)
			{
				Register<TRelationKind>(storageIndex);
			}
			return (RelationStorage<TRelationKind>) storages[storageIndex];
		}

		public void Set<TRelationKind>(in Entity entityA, in Entity entityB, TRelationKind relationData) where TRelationKind : unmanaged
		{
			Lookup<TRelationKind>().Set(entityA, entityB, relationData);
		}

		public TRelationKind Get<TRelationKind>(in Entity entityA, in Entity entityB) where TRelationKind : unmanaged
		{
			return Lookup<TRelationKind>().Get(entityA, entityB);
		}

		public (bool, bool) Remove<TRelationKind>(in Entity entityA, in Entity entityB) where TRelationKind : unmanaged
		{
			return Lookup<TRelationKind>().Remove(entityA, entityB);
		}

		public void UnrelateAll<TRelationKind>(int entityID) where TRelationKind : unmanaged
		{
			Lookup<TRelationKind>().UnrelateAll(entityID);
		}

		public ReverseSpanEnumerator<(Entity, Entity)> Relations<TRelationKind>() where TRelationKind : unmanaged
		{
			return Lookup<TRelationKind>().All();
		}

		public bool Related<TRelationKind>(int idA, int idB) where TRelationKind : unmanaged
		{
			return Lookup<TRelationKind>().Has((idA, idB));
		}

		public ReverseSpanEnumerator<Entity> OutRelations<TRelationKind>(int entityID) where TRelationKind : unmanaged
		{
			return Lookup<TRelationKind>().OutRelations(entityID);
		}

		public Entity OutRelationSingleton<TRelationKind>(int entityID) where TRelationKind : unmanaged
		{
			return Lookup<TRelationKind>().OutFirst(entityID);
		}

		public Entity NthOutRelation<TRelationKind>(int entityID, int n) where TRelationKind : unmanaged
		{
			return Lookup<TRelationKind>().OutNth(entityID, n);
		}

		public int OutRelationCount<TRelationKind>(int entityID) where TRelationKind : unmanaged
		{
			return Lookup<TRelationKind>().OutRelationCount(entityID);
		}

		public bool HasOutRelation<TRelationKind>(int entityID) where TRelationKind : unmanaged
		{
			return Lookup<TRelationKind>().HasOutRelation(entityID);
		}

		public ReverseSpanEnumerator<Entity> InRelations<TRelationKind>(int entityID) where TRelationKind : unmanaged
		{
			return Lookup<TRelationKind>().InRelations(entityID);
		}

		public Entity NthInRelation<TRelationKind>(int entityID, int n) where TRelationKind : unmanaged
		{
			return Lookup<TRelationKind>().InNth(entityID, n);
		}

		public Entity InRelationSingleton<TRelationKind>(int entityID) where TRelationKind : unmanaged
		{
			return Lookup<TRelationKind>().InFirst(entityID);
		}

		public bool HasInRelation<TRelationKind>(int entityID) where TRelationKind : unmanaged
		{
			return Lookup<TRelationKind>().HasInRelation(entityID);
		}

		public int InRelationCount<TRelationKind>(int entityID) where TRelationKind : unmanaged
		{
			return Lookup<TRelationKind>().InRelationCount(entityID);
		}

		// untyped methods used for destroying and snapshots

		public unsafe void Set(int entityA, int entityB, int relationTypeIndex, void* relationData)
		{
			storages[relationTypeIndex].Set(entityA, entityB, relationData);
		}

		public void UnrelateAll(int entityID, int relationTypeIndex)
		{
			storages[relationTypeIndex].UnrelateAll(entityID);
		}

		public void Clear()
		{
			for (var i = 0; i < storages.Length; i += 1)
			{
				if (storages[i] != null)
				{
					storages[i].Clear();
				}
			}
		}

		public void CreateMissingStorages(RelationDepot other)
		{
			while (other.RelationTypeIndices.Count >= storages.Length)
			{
				Array.Resize(ref storages, storages.Length * 2);
			}

			while (other.RelationTypeIndices.Count >= other.storages.Length)
			{
				Array.Resize(ref other.storages, other.storages.Length * 2);
			}

			for (var i = 0; i < other.RelationTypeIndices.Count; i += 1)
			{
				if (storages[i] == null && other.storages[i] != null)
				{
					storages[i] = other.storages[i].CreateStorage();
				}
			}
		}

		public unsafe void TransferStorage(Dictionary<int, int> worldToTransferID, RelationDepot other)
		{
			for (var i = 0; i < storages.Length; i += 1)
			{
				if (storages[i] != null)
				{
					foreach (var (a, b) in storages[i].All())
					{
						if (worldToTransferID.TryGetValue(a, out var otherA))
						{
							if (worldToTransferID.TryGetValue(b, out var otherB))
							{
								var storageIndex = storages[i].GetStorageIndex(a, b);
								var relationData = storages[i].Get(storageIndex);
								other.Set(otherA, otherB, i, relationData);
								other.EntityStorage.AddRelationKind(otherA, i);
								other.EntityStorage.AddRelationKind(otherB, i);
							}
							else
							{
								throw new InvalidOperationException($"Missing transfer entity! {EntityStorage.Tag(a.ID)} related to {EntityStorage.Tag(b.ID)}");
							}
						}
					}
				}
			}
		}
	}
}
