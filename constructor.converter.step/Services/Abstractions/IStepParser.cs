using constructor.converter.step.Models;

namespace constructor.converter.step.Services.Abstractions;

public interface IStepParser
{
    public StepFileInfo ReadStepFile(string filePath, double linearDeflection = 0.01, 
        double angularDeflection = 0.5);

    TriangulationData GetTriangulationFromStepFile
    (string filePath, double linearDeflection = 0.01,
        double angularDeflection = 0.5);
}