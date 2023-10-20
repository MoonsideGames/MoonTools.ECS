using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace MoonTools.ECS.Rev2
{
	public class World : IDisposable
	{
		// Lookup from ArchetypeSignature to Archetype
		Dictionary<ArchetypeSignature, Archetype> ArchetypeIndex = new Dictionary<ArchetypeSignature, Archetype>();

		// Going from EntityId to Archetype and storage row
		Dictionary<EntityId, Record> EntityIndex = new Dictionary<EntityId, Record>();

		// Going from ComponentId to an Archetype storage column index
		Dictionary<ComponentId, Dictionary<ArchetypeId, ArchetypeRecord>> ComponentIndex = new Dictionary<ComponentId, Dictionary<ArchetypeId, ArchetypeRecord>>();

		// Get ComponentId from a Type
		Dictionary<Type, ComponentId> TypeToComponentId = new Dictionary<Type, ComponentId>();

		// Get element size from a ComponentId
		Dictionary<ComponentId, int> ElementSizes = new Dictionary<ComponentId, int>();

		// ID Management
		IdAssigner<ArchetypeId> ArchetypeIdAssigner = new IdAssigner<ArchetypeId>();
		IdAssigner<EntityId> EntityIdAssigner = new IdAssigner<EntityId>();
		IdAssigner<ComponentId> ComponentIdAssigner = new IdAssigner<ComponentId>();

		private bool IsDisposed;

		public delegate void RefAction<T1, T2>(ref T1 arg1, ref T2 arg2);

		public World()
		{
			// Create the Empty Archetype
			CreateArchetype(ArchetypeSignature.Empty);
		}

		private Archetype CreateArchetype(ArchetypeSignature signature)
		{
			var archetypeId = ArchetypeIdAssigner.Assign();
			var archetype = new Archetype(archetypeId, signature)
			{
				Components = new List<Column>(signature.Count)
			};

			ArchetypeIndex.Add(signature, archetype);

			for (int i = 0; i < signature.Count; i += 1)
			{
				var componentId = signature[i];
				ComponentIndex[componentId].Add(archetypeId, new ArchetypeRecord(i));
				archetype.Components.Add(new Column(ElementSizes[componentId]));
			}

			return archetype;
		}

		public EntityId CreateEntity()
		{
			var entityId = EntityIdAssigner.Assign();
			var emptyArchetype = ArchetypeIndex[ArchetypeSignature.Empty];
			EntityIndex.Add(entityId, new Record(emptyArchetype, 0));
			emptyArchetype.RowToEntity.Add(entityId);
			return entityId;
		}

		// FIXME: would be much more efficient to do all this at load time somehow
		private void RegisterComponent<T>() where T : unmanaged
		{
			var componentId = ComponentIdAssigner.Assign();
			TypeToComponentId.Add(typeof(T), componentId);
			ComponentIndex.Add(componentId, new Dictionary<ArchetypeId, ArchetypeRecord>());
			ElementSizes.Add(componentId, Unsafe.SizeOf<T>());
		}

		private void TryRegisterComponentId<T>() where T : unmanaged
		{
			if (!TypeToComponentId.TryGetValue(typeof(T), out var componentId))
			{
				RegisterComponent<T>();
			}
		}

		private ComponentId GetComponentId<T>() where T : unmanaged
		{
			return TypeToComponentId[typeof(T)];
		}

		internal ArchetypeRecord GetArchetypeRecord<T>(Archetype archetype) where T : unmanaged
		{
			var componentId = GetComponentId<T>();
			return ComponentIndex[componentId][archetype.Id];
		}

		public bool HasComponent<T>(EntityId entityId) where T : unmanaged
		{
			var componentId = GetComponentId<T>();

			var record = EntityIndex[entityId];
			var archetypes = ComponentIndex[componentId];
			return archetypes.ContainsKey(record.Archetype.Id);
		}

		public unsafe T GetComponent<T>(EntityId entityId) where T : unmanaged
		{
			var componentId = GetComponentId<T>();

			var record = EntityIndex[entityId];
			var archetype = record.Archetype;

			var archetypes = ComponentIndex[componentId];
			if (!archetypes.ContainsKey(archetype.Id))
			{
				return default; // FIXME: maybe throw in debug mode?
			}

			var archetypeRecord = archetypes[archetype.Id];
			var column = archetype.Components[archetypeRecord.ColumnIndex];

			return ((T*) column.Elements)[record.Row];
		}

		public unsafe void SetComponent<T>(EntityId entityId, T component) where T : unmanaged
		{
			TryRegisterComponentId<T>();
			var componentId = GetComponentId<T>();

			if (HasComponent<T>(entityId))
			{
				var record = EntityIndex[entityId];
				var archetype = record.Archetype;
				var archetypes = ComponentIndex[componentId];
				var archetypeRecord = archetypes[archetype.Id];
				var column = archetype.Components[archetypeRecord.ColumnIndex];

				((T*) column.Elements)[record.Row] = component;
			}
			else
			{
				AddComponent(entityId, component);
			}
		}

		private void AddComponent<T>(EntityId entityId, T component) where T : unmanaged
		{
			Archetype? nextArchetype;

			var componentId = GetComponentId<T>();

			// move the entity to the new archetype
			var record = EntityIndex[entityId];
			var archetype = record.Archetype;

			if (archetype.Edges.TryGetValue(componentId, out var edge))
			{
				nextArchetype = edge.Add;
			}
			else
			{
				// FIXME: pool the signatures
				var nextSignature = new ArchetypeSignature(archetype.Signature.Count + 1);
				archetype.Signature.CopyTo(nextSignature);
				nextSignature.Insert(componentId);

				if (!ArchetypeIndex.TryGetValue(nextSignature, out nextArchetype))
				{
					nextArchetype = CreateArchetype(nextSignature);
				}

				var newEdge = new ArchetypeEdge(nextArchetype, archetype);
				archetype.Edges.Add(componentId, newEdge);
				nextArchetype.Edges.Add(componentId, newEdge);
			}

			MoveEntityToHigherArchetype(entityId, record.Row, archetype, nextArchetype);

			// add the new component to the new archetype
			var archetypes = ComponentIndex[componentId];
			var archetypeRecord = archetypes[nextArchetype.Id];
			var column = nextArchetype.Components[archetypeRecord.ColumnIndex];
			column.Append(component);
		}

		public void RemoveComponent<T>(EntityId entityId) where T : unmanaged
		{
			Archetype? nextArchetype;

			var componentId = GetComponentId<T>();

			var record = EntityIndex[entityId];
			var archetype = record.Archetype;

			if (archetype.Edges.TryGetValue(componentId, out var edge))
			{
				nextArchetype = edge.Remove;
			}
			else
			{
				// FIXME: pool the signatures
				var nextSignature = new ArchetypeSignature(archetype.Signature.Count + 1);
				archetype.Signature.CopyTo(nextSignature);
				nextSignature.Remove(componentId);

				if (!ArchetypeIndex.TryGetValue(nextSignature, out nextArchetype))
				{
					nextArchetype = CreateArchetype(nextSignature);
				}

				var newEdge = new ArchetypeEdge(nextArchetype, archetype);
				archetype.Edges.Add(componentId, newEdge);
				nextArchetype.Edges.Add(componentId, newEdge);
			}

			MoveEntityToLowerArchetype(entityId, record.Row, archetype, nextArchetype, componentId);
		}

		private void MoveEntityToHigherArchetype(EntityId entityId, int row, Archetype from, Archetype to)
		{
			for (int i = 0; i < from.Components.Count; i += 1)
			{
				var componentId = from.Signature[i];
				var destinationColumnIndex = ComponentIndex[componentId][to.Id].ColumnIndex;

				// copy all components to higher archetype
				from.Components[i].CopyToEnd(row, to.Components[destinationColumnIndex]);

				// delete row on from archetype
				from.Components[i].Delete(row);

				if (from.Count > 1)
				{
					// update row to entity lookup on from archetype
					from.RowToEntity[row] = from.RowToEntity[from.Count - 1];
					from.RowToEntity.RemoveAt(from.Count - 1);
					EntityIndex[from.RowToEntity[row]] = new Record(from, row);
				}
			}

			// update row to entity lookup on to archetype
			EntityIndex[entityId] = new Record(to, to.Count);
			to.RowToEntity.Add(entityId);

			to.Count += 1;
			from.Count -= 1;
		}

		private void MoveEntityToLowerArchetype(EntityId entityId, int row, Archetype from, Archetype to, ComponentId removed)
		{
			for (int i = 0; i < from.Components.Count; i += 1)
			{
				var componentId = from.Signature[i];

				// delete the row
				from.Components[i].Delete(row);

				// if this isn't the removed component, copy to the lower archetype
				if (componentId != removed)
				{
					var destinationColumnIndex = ComponentIndex[componentId][to.Id].ColumnIndex;
					from.Components[i].CopyToEnd(row, to.Components[destinationColumnIndex]);

					if (from.Count > 0)
					{
						// update row to entity lookup on from archetype
						from.RowToEntity[row] = from.RowToEntity[from.Count - 1];
						from.RowToEntity.RemoveAt(from.Count - 1);
						EntityIndex[from.RowToEntity[row]] = new Record(from, row);
					}
				}
			}

			// update row to entity lookup on to archetype
			EntityIndex[entityId] = new Record(to, to.Count);
			to.RowToEntity.Add(entityId);

			to.Count += 1;
			from.Count -= 1;
		}

		public unsafe void ForEachEntity<T1, T2>(ArchetypeSignature signature, RefAction<T1, T2> rowAction) where T1 : unmanaged where T2 : unmanaged
		{
			var archetype = ArchetypeIndex[signature];

			var componentIdOne = signature[0];
			var columnIndexOne = ComponentIndex[componentIdOne][archetype.Id].ColumnIndex;
			var columnOneElements = archetype.Components[columnIndexOne].Elements;

			var componentIdTwo = signature[1];
			var columnIndexTwo = ComponentIndex[componentIdTwo][archetype.Id].ColumnIndex;
			var columnTwoElements = archetype.Components[columnIndexTwo].Elements;

			for (int i = 0; i < archetype.Count; i += 1)
			{
				rowAction(ref ((T1*) columnOneElements)[i], ref ((T2*) columnTwoElements)[i]);
			}

			foreach (var edge in archetype.Edges.Values)
			{
				if (edge.Add != archetype)
				{
					ForEachEntity(edge.Add.Signature, rowAction);
				}
			}
		}

		/*
		public void ForEachEntity(ArchetypeSignature signature, Action<Entity> rowAction)
		{
			var archetype = ArchetypeIndex[signature];

			for (int i = 0; i < archetype.Count; i += 1)
			{
				var entity = new Entity(this, archetype, i, archetype.RowToEntity[i]);
				rowAction(entity);
			}

			// recursion might get too hairy here
			foreach (var edge in archetype.Edges.Values)
			{
				if (edge.Add != archetype)
				{
					ForEachEntity(edge.Add.Signature, rowAction);
				}
			}
		}
		*/

		protected virtual void Dispose(bool disposing)
		{
			if (!IsDisposed)
			{
				if (disposing)
				{
					// dispose managed state (managed objects)
					foreach (var archetype in ArchetypeIndex.Values)
					{
						for (var i = 0; i < archetype.Signature.Count; i += 1)
						{
							archetype.Components[i].Dispose();
						}
					}
				}

				// TODO: free unmanaged resources (unmanaged objects) and override finalizer
				// TODO: set large fields to null
				IsDisposed = true;
			}
		}

		// TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
		// ~World()
		// {
		//     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		//     Dispose(disposing: false);
		// }

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}
