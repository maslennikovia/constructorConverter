namespace constructor.converter.step.Models;

public class TriangulationData
{
    public List<Vector3> Vertices { get; } = new List<Vector3>();
    public List<Vector3> Normals { get; } = new List<Vector3>();
    public List<(int, int, int)> Triangles { get; } = new List<(int, int, int)>();

    public TriangulationData()
    {
        Vertices = new List<Vector3>();
        Normals = new List<Vector3>();
        Triangles =  new List<(int, int, int)>();
    }
}