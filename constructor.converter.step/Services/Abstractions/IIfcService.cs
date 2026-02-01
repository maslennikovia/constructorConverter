using constructor.converter.step.Models;

namespace constructor.converter.step.Services.Abstractions;

public interface IIfcService
{
    public void CreateIfcModel(
        StepFileInfo stepData,
        string outputPath,
        double linearDeflection = 0.01, 
        double angularDeflection = 0.5);
}