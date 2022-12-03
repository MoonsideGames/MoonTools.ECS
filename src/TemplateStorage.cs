namespace MoonTools.ECS
{
	public class TemplateStorage
	{
		private int nextID = 0;

		public Template Create()
		{
			return new Template(NextID());
		}

		private int NextID()
		{
			var id = nextID;
			nextID += 1;
			return id;
		}
	}
}
