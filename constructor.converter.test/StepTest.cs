using constructor.converter.step.Services.Implementations;

namespace constructor.converter.test;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void ReadStepFile_WithTestData_ReadSuccess()
    {
        try
        {
            
            string path = Path.Combine(AppContext.BaseDirectory, "Assets", "rect.stp");
            StepParser parser = new StepParser();
            parser.ReadStepFile(path);
        }
        catch(Exception e)
        {
            Assert.Fail(e.Message);
        }
        
        Assert.Pass();
    }
    
    [Test]
    public void CreateIfcFile_WithTestData_Success()
    {
        try
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Assets", "rect.stp");
            string output = Path.Combine(AppContext.BaseDirectory, "Assets", "building_example.ifc");
            StepParser parser = new StepParser();
            var parsingResult = parser.ReadStepFile(path);
            IfcService ifcService = new IfcService();
            ifcService.CreateIfcModel(parsingResult, output);
        }
        catch(Exception e)
        {
            Assert.Fail(e.Message);
        }
    }
    
    [Test]
    public void CreateBoxyIfcFile_WithTestData_Success()
    {
        try
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Assets", "komponovka_k-140_izm2.stp");
            string output = Path.Combine(AppContext.BaseDirectory, "Assets", "building_example2.ifc");
            var triangulationData = new StepParser().GetTriangulationFromStepFile(path, 1);
            new IfcService().CreateIfcModelFromTriangulation(triangulationData, output);
        }
        catch(Exception e)
        {
            Assert.Fail(e.Message);
        }
    }
    
    [Test]
    public void CreateIoIfcFile_WithTestData_Success()
    {
        try
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Assets", "io1-ca-214.stp");
            string output = Path.Combine(AppContext.BaseDirectory, "Assets", "building_example3.ifc");
            var triangulationData = new StepParser().GetTriangulationFromStepFile(path, 1);
            new IfcService().CreateIfcModelFromTriangulation(triangulationData, output);
        }
        catch(Exception e)
        {
            Assert.Fail(e.Message);
        }
    }
    
    [Test]
    public void CreateFilterIfcFile_WithTestData_Success()
    {
        try
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Assets", "filler.stp");
            string output = Path.Combine(AppContext.BaseDirectory, "Assets", "building_example4.ifc");
            var triangulationData = new StepParser().GetTriangulationFromStepFile(path, 1);
            new IfcService().CreateIfcModelFromTriangulation(triangulationData, output);
        }
        catch(Exception e)
        {
            Assert.Fail(e.Message);
        }
    }
    
    [Test]
    public void CreateAS1IfcFile_WithTestData_Success()
    {
        try
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Assets", "Skrubber_azotnyy_AllCATPart.stp");
            string output = Path.Combine(AppContext.BaseDirectory, "Assets", "building_example5.ifc");
            var triangulationData = new StepParser().GetTriangulationFromStepFile(path, 1);
            new IfcService().CreateIfcModelFromTriangulation(triangulationData, output);
        }
        catch(Exception e)
        {
            Assert.Fail(e.Message);
        }
    }
}