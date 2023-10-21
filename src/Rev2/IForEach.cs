namespace MoonTools.ECS.Rev2;

public interface IForEach<T1, T2> where T1 : unmanaged where T2 : unmanaged
{
	public void Update(ref T1 t1, ref T2 t2);
}
