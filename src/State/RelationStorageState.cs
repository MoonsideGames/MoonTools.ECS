using System.Collections.Generic;

namespace MoonTools.ECS
{
	internal class RelationStorageState
	{
		public int Count;
		public byte[] Relations;
		public byte[] RelationDatas;

		public unsafe static RelationStorageState Create<TRelation>(int count) where TRelation : unmanaged
		{
			return new RelationStorageState(
				count,
				count * sizeof(Relation),
				count * sizeof(TRelation)
			);
		}

		private RelationStorageState(int count, int relationSize, int relationDataSize)
		{
			Count = count;
			Relations = new byte[relationSize];
			RelationDatas = new byte[relationDataSize];
		}
	}
}
