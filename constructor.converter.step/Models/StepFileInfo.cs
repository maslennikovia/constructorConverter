using OCCSharp;

namespace constructor.converter.step.Models;

public class StepFileInfo
{
    public string FileName { get; set; }
    public List<TopoDS_Shape> Shapes { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
    
    public BoundingBox BoundingBox { get; set; }
    
    public int SolidCount { get; set; }
    public int ShellCount { get; set; }
    public int FaceCount { get; set; }

    public TDF_Label RootLabel { get; set; }
        
    public StepFileInfo()
    {
        Shapes = new List<TopoDS_Shape>();
        Metadata = new Dictionary<string, string>();
    }
}