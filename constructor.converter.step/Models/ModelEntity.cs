using OCCSharp;

namespace constructor.converter.step.Models;

public class ModelEntity
{
    public string? Name { get; set; }
    public ElementTriangulation? Triangulation { get; set; }

    public gp_Trsf Location { get; set; } =  new gp_Trsf();
    public List<ModelEntity> Childrens { get; set; } = [];
}