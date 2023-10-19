using System;
using System.Collections.Generic;

namespace MoonTools.ECS.Rev2
{
	public class World
	{
		// Lookup from ArchetypeSignature to Archetype
		Dictionary<ArchetypeSignature, Archetype> ArchetypeIndex = new Dictionary<ArchetypeSignature, Archetype>();

		// Going from EntityId to Archetype and storage row
		Dictionary<EntityId, Record> EntityIndex = new Dictionary<EntityId, Record>();

		// Going from ComponentId to an Archetype storage column
		Dictionary<ComponentId, Dictionary<ArchetypeId, ArchetypeRecord>> ComponentIndex = new Dictionary<ComponentId, Dictionary<ArchetypeId, ArchetypeRecord>>();

		// Get ComponentId from a Type
		Dictionary<Type, ComponentId> TypeToComponentId = new Dictionary<Type, ComponentId>();

		// ID Management
		IdAssigner<ArchetypeId> ArchetypeIdAssigner = new IdAssigner<ArchetypeId>();
		IdAssigner<EntityId> EntityIdAssigner = new IdAssigner<EntityId>();
		IdAssigner<ComponentId> ComponentIdAssigner = new IdAssigner<ComponentId>();

		public World()
		{
			// Create the Empty Archetype
			CreateArchetype(ArchetypeSignature.Empty);
		}

		private Archetype CreateArchetype(ArchetypeSignature signature)
		{
			var archetypeId = ArchetypeIdAssigner.Assign();
			var archetype = new Archetype(archetypeId, signature);
			ArchetypeIndex.Add(signature, archetype);

			for (int i = 0; i < signature.Count; i += 1)
			{
				var componentId = signature[i];
				ComponentIndex[componentId].Add(archetypeId, new ArchetypeRecord(i));
			}

			return archetype;
		}

		public EntityId CreateEntity()
		{
			var entityId = EntityIdAssigner.Assign();
			var emptyArchetype = ArchetypeIndex[ArchetypeSignature.Empty];
			EntityIndex.Add(entityId, new Record(emptyArchetype, 0));
			return entityId;
		}

		// FIXME: would be much more efficient to do all this at load time somehow
		private void RegisterComponentId(ComponentId componentId)
		{
			ComponentIndex.Add(componentId, new Dictionary<ArchetypeId, ArchetypeRecord>());
		}

		public ComponentId GetComponentId<T>() where T : unmanaged
		{
			if (!TypeToComponentId.TryGetValue(typeof(T), out var componentId))
			{
				componentId = ComponentIdAssigner.Assign();
			}

			return componentId;
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

		public void AddComponent<T>(EntityId entityId, T component) where T : unmanaged
		{
			Archetype? nextArchetype;

			var componentId = GetComponentId<T>();

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

				var newEdge = new ArchetypeEdge(archetype, nextArchetype);
				archetype.Edges.Add(componentId, newEdge);
				nextArchetype.Edges.Add(componentId, newEdge);
			}

			MoveEntity(entityId, record.Row, archetype, nextArchetype);
		}

		private void MoveEntity(EntityId entityId, int row, Archetype from, Archetype to)
		{
			for (int i = 0; i < from.Components.Count; i += 1)
			{
				var componentId = from.Signature[i];
				var destinationColumnIndex = ComponentIndex[componentId][to.Id].ColumnIndex;

				from.Components[i].CopyToEnd(row, to.Components[destinationColumnIndex]);
				from.Components[i].Delete(row);
			}

			EntityIndex[entityId] = new Record(to, to.Count);

			to.Count += 1;
			from.Count -= 1;
		}
	}
}
