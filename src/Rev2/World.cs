using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MoonTools.ECS.Rev2
{
	public class World : IDisposable
	{
		// Get ComponentId from a Type
		internal static Dictionary<Type, ComponentId> TypeToComponentId = new Dictionary<Type, ComponentId>();
		// Get element size from a ComponentId
		internal static Dictionary<ComponentId, int> ElementSizes = new Dictionary<ComponentId, int>();

		// Lookup from ArchetypeSignature to Archetype
		internal Dictionary<ArchetypeSignature, Archetype> ArchetypeIndex = new Dictionary<ArchetypeSignature, Archetype>();

		// Going from EntityId to Archetype and storage row
		Dictionary<EntityId, Record> EntityIndex = new Dictionary<EntityId, Record>();

		// Going from ComponentId to Archetype list
		Dictionary<ComponentId, List<Archetype>> ComponentIndex = new Dictionary<ComponentId, List<Archetype>>();

		// ID Management
		IdAssigner<EntityId> EntityIdAssigner = new IdAssigner<EntityId>();
		IdAssigner<ComponentId> ComponentIdAssigner = new IdAssigner<ComponentId>();

		internal readonly Archetype EmptyArchetype;

		public FilterBuilder FilterBuilder => new FilterBuilder(this);

		private bool IsDisposed;

		public delegate void RefAction<T1, T2>(ref T1 arg1, ref T2 arg2);

		public World()
		{
			// Create the Empty Archetype
			EmptyArchetype = CreateArchetype(ArchetypeSignature.Empty);
		}

		internal Archetype CreateArchetype(ArchetypeSignature signature)
		{
			var archetype = new Archetype(this, signature)
			{
				ComponentColumns = new List<Column>(signature.Count)
			};

			ArchetypeIndex.Add(signature, archetype);

			for (int i = 0; i < signature.Count; i += 1)
			{
				var componentId = signature[i];
				ComponentIndex[componentId].Add(archetype);
				archetype.ComponentToColumnIndex.Add(componentId, archetype.ComponentColumns.Count);
				archetype.ComponentColumns.Add(new Column(ElementSizes[componentId]));
			}

			return archetype;
		}

		public EntityId CreateEntity()
		{
			var entityId = EntityIdAssigner.Assign();
			EntityIndex.Add(entityId, new Record(EmptyArchetype, EmptyArchetype.Count));
			EmptyArchetype.RowToEntity.Add(entityId);
			return entityId;
		}

		// used as a fast path by snapshot restore
		internal void CreateEntityOnArchetype(Archetype archetype)
		{
			var entityId = EntityIdAssigner.Assign();
			EntityIndex.Add(entityId, new Record(archetype, archetype.Count));
			archetype.RowToEntity.Add(entityId);
		}

		// used as a fast path by Archetype.ClearAll and snapshot restore
		internal void FreeEntity(EntityId entityId)
		{
			EntityIndex.Remove(entityId);
			EntityIdAssigner.Unassign(entityId);
		}

		// FIXME: would be much more efficient to do all this at load time somehow
		private void RegisterComponent<T>() where T : unmanaged
		{
			var componentId = ComponentIdAssigner.Assign();
			TypeToComponentId.Add(typeof(T), componentId);
			ComponentIndex.Add(componentId, new List<Archetype>());
			ElementSizes.Add(componentId, Unsafe.SizeOf<T>());
		}

		private void TryRegisterComponentId<T>() where T : unmanaged
		{
			if (!TypeToComponentId.ContainsKey(typeof(T)))
			{
				RegisterComponent<T>();
			}
		}

		// non-generic variant for use with Transfer
		internal void AddComponentIndexEntry(ComponentId componentId)
		{
			if (!ComponentIndex.ContainsKey(componentId))
			{
				ComponentIndex.Add(componentId, new List<Archetype>());
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private ComponentId GetComponentId<T>() where T : unmanaged
		{
			return TypeToComponentId[typeof(T)];
		}

		public bool Has<T>(EntityId entityId) where T : unmanaged
		{
			var componentId = GetComponentId<T>();
			var record = EntityIndex[entityId];
			return record.Archetype.ComponentToColumnIndex.ContainsKey(componentId);
		}

		// will throw if non-existent
		public unsafe ref T Get<T>(EntityId entityId) where T : unmanaged
		{
			var componentId = GetComponentId<T>();

			var record = EntityIndex[entityId];
			var columnIndex = record.Archetype.ComponentToColumnIndex[componentId];
			var column = record.Archetype.ComponentColumns[columnIndex];

			return ref ((T*) column.Elements)[record.Row];
		}

		public unsafe void Set<T>(in EntityId entityId, in T component) where T : unmanaged
		{
			TryRegisterComponentId<T>();
			var componentId = GetComponentId<T>();

			if (Has<T>(entityId))
			{
				var record = EntityIndex[entityId];
				var columnIndex = record.Archetype.ComponentToColumnIndex[componentId];
				var column = record.Archetype.ComponentColumns[columnIndex];

				((T*) column.Elements)[record.Row] = component;
			}
			else
			{
				Add(entityId, component);
			}
		}

		private void Add<T>(EntityId entityId, in T component) where T : unmanaged
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
			var columnIndex = nextArchetype.ComponentToColumnIndex[componentId];
			var column = nextArchetype.ComponentColumns[columnIndex];
			column.Append(component);
		}

		public void Remove<T>(EntityId entityId) where T : unmanaged
		{
			Archetype? nextArchetype;

			var componentId = GetComponentId<T>();

			var (archetype, row) = EntityIndex[entityId];

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

			MoveEntityToLowerArchetype(entityId, row, archetype, nextArchetype, componentId);
		}

		public void Destroy(EntityId entityId)
		{
			var record = EntityIndex[entityId];
			var archetype = record.Archetype;
			var row = record.Row;

			for (int i = 0; i < archetype.ComponentColumns.Count; i += 1)
			{
				archetype.ComponentColumns[i].Delete(row);
			}

			if (row != archetype.Count - 1)
			{
				// move last row entity to open spot
				var lastRowEntity = archetype.RowToEntity[archetype.Count - 1];
				archetype.RowToEntity[row] = lastRowEntity;
				EntityIndex[lastRowEntity] = new Record(archetype, row);
			}

			archetype.RowToEntity.RemoveAt(archetype.Count - 1);
			EntityIndex.Remove(entityId);
			EntityIdAssigner.Unassign(entityId);
		}

		private void MoveEntityToHigherArchetype(EntityId entityId, int row, Archetype from, Archetype to)
		{
			for (int i = 0; i < from.ComponentColumns.Count; i += 1)
			{
				var componentId = from.Signature[i];
				var destinationColumnIndex = to.ComponentToColumnIndex[componentId];

				// copy all components to higher archetype
				from.ComponentColumns[i].CopyElementToEnd(row, to.ComponentColumns[destinationColumnIndex]);

				// delete row on from archetype
				from.ComponentColumns[i].Delete(row);
			}

			if (row != from.Count - 1)
			{
				// move last row entity to open spot
				var lastRowEntity = from.RowToEntity[from.Count - 1];
				from.RowToEntity[row] = lastRowEntity;
				EntityIndex[lastRowEntity] = new Record(from, row);
			}

			from.RowToEntity.RemoveAt(from.Count - 1);

			// update row to entity lookup on to archetype
			EntityIndex[entityId] = new Record(to, to.Count);
			to.RowToEntity.Add(entityId);
		}

		private void MoveEntityToLowerArchetype(EntityId entityId, int row, Archetype from, Archetype to, ComponentId removed)
		{
			for (int i = 0; i < from.ComponentColumns.Count; i += 1)
			{
				var componentId = from.Signature[i];

				// delete the row
				from.ComponentColumns[i].Delete(row);

				// if this isn't the removed component, copy to the lower archetype
				if (componentId != removed)
				{
					var destinationColumnIndex = to.ComponentToColumnIndex[componentId];
					from.ComponentColumns[i].CopyElementToEnd(row, to.ComponentColumns[destinationColumnIndex]);
				}
			}

			if (row != from.Count - 1)
			{
				// update row to entity lookup on from archetype
				var lastRowEntity = from.RowToEntity[from.Count - 1];
				from.RowToEntity[row] = lastRowEntity;
				EntityIndex[lastRowEntity] = new Record(from, row);
			}

			from.RowToEntity.RemoveAt(from.Count - 1);

			// update row to entity lookup on to archetype
			EntityIndex[entityId] = new Record(to, to.Count);
			to.RowToEntity.Add(entityId);
		}

		public unsafe void ForEachEntity<T, T1, T2>(Filter filter,
			T rowForEachContainer) where T : IForEach<T1, T2> where T1 : unmanaged where T2 : unmanaged
		{
			foreach (var archetype in filter.Archetypes)
			{
				var componentIdOne = archetype.Signature[0];
				var columnIndexOne = archetype.ComponentToColumnIndex[componentIdOne];
				var columnOneElements = archetype.ComponentColumns[columnIndexOne].Elements;

				var componentIdTwo = archetype.Signature[1];
				var columnIndexTwo = archetype.ComponentToColumnIndex[componentIdTwo];
				var columnTwoElements = archetype.ComponentColumns[columnIndexTwo].Elements;

				for (int i = archetype.Count - 1; i >= 0; i -= 1)
				{
					rowForEachContainer.Update(ref ((T1*) columnOneElements)[i], ref ((T2*) columnTwoElements)[i]);
				}
			}
		}

		public unsafe void ForEachEntity<T1, T2>(Filter filter, RefAction<T1, T2> rowAction) where T1 : unmanaged where T2 : unmanaged
		{
			foreach (var archetype in filter.Archetypes)
			{
				var componentIdOne = archetype.Signature[0];
				var columnIndexOne = archetype.ComponentToColumnIndex[componentIdOne];
				var columnOneElements = archetype.ComponentColumns[columnIndexOne].Elements;

				var componentIdTwo = archetype.Signature[1];
				var columnIndexTwo = archetype.ComponentToColumnIndex[componentIdTwo];
				var columnTwoElements = archetype.ComponentColumns[columnIndexTwo].Elements;

				for (int i = archetype.Count - 1; i >= 0; i -= 1)
				{
					rowAction(ref ((T1*) columnOneElements)[i], ref ((T2*) columnTwoElements)[i]);
				}
			}
		}

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
							archetype.ComponentColumns[i].Dispose();
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
