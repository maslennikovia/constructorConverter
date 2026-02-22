namespace constructor.converter.step.Models;

public class ModelData
{
    public ModelEntity Root { get; set; }
    public Dictionary<int, TriangulationData> Triangulations { get; set; }
}