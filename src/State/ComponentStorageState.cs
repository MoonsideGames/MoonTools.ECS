using System.Collections.Generic;

namespace MoonTools.ECS
{
    internal class ComponentStorageState
    {
		public int Count;
		public byte[] EntityIDs;
		public byte[] Components;

        public unsafe static ComponentStorageState Create<TComponent>(int count) where TComponent : unmanaged
        {
			return new ComponentStorageState(
                count,
                count * sizeof(int),
                count * sizeof(TComponent)
            );
		}

        private ComponentStorageState(int count, int entityIDSize, int componentSize)
        {
			Count = count;
			EntityIDs = new byte[entityIDSize];
			Components = new byte[componentSize];
		}
	}
}
