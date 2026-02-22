using constructor.converter.step.Models;

namespace constructor.converter.step.Services.Abstractions;

public interface IStepParser
{
    /*public StepFileInfo ReadStepFile(string filePath,
        IProgress<ConversionProgress> converterProgress,
        double linearDeflection = 0.01, 
        double angularDeflection = 0.5,
        int stageCount = 1);*/

    ModelData GetTriangulationFromStepFile(
        string filePath,
        IProgress<ConversionProgress> converterProgress,
        string[] stages,
        int stage,
        double linearDeflection = 0.01,
        double angularDeflection = 0.5);
}