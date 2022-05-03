using System.Collections.Generic;

namespace MoonTools.ECS
{
    internal class IndexableSetState<T> where T : unmanaged
    {
		public int Count;
		public byte[] Array;

        public unsafe IndexableSetState(int count)
        {
			Count = count;
			Array = new byte[sizeof(T) * count];
		}
	}
}
