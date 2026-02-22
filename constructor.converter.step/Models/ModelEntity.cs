namespace constructor.converter.step.Models;

public class ModelEntity
{
    public string? Name { get; set; }
    public ElementTriangulation? Triangulation { get; set; }
    public List<ModelEntity> Childrens { get; set; } = [];
}