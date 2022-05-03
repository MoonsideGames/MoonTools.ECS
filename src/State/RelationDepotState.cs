using System;
using System.Collections.Generic;

namespace MoonTools.ECS
{
    internal class RelationDepotState
    {
		public Dictionary<Type, RelationStorageState> StorageStates = new Dictionary<Type, RelationStorageState>();
	}
}
