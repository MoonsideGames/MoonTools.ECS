using System;
using System.Collections.Generic;

namespace MoonTools.ECS
{
	internal class ComponentDepotState
	{
		public Dictionary<Type, ComponentStorageState> StorageStates = new Dictionary<Type, ComponentStorageState>();
		public Dictionary<FilterSignature, IndexableSetState<int>> FilterStates = new Dictionary<FilterSignature, IndexableSetState<int>>();
	}
}
