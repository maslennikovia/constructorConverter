using OCCSharp;

namespace constructor.converter.step.Models;

public class TriangulationData
{
    public int HasCode { get; set; }
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

public class ElementTriangulation
{
    /// <summary>
    /// ссылки на триангуляцию плоскостей элемента и их Transform
    /// </summary>
    public List<(int, gp_Trsf)> Triangulations { get; set; } = new List<(int, gp_Trsf)>();
}

public record Transform
{
    public Vector3 Position { get; set; }
}