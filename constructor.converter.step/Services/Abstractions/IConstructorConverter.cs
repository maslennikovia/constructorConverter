using constructor.converter.step.Models;

namespace constructor.converter.step.Services.Abstractions;

public interface IConstructorConverter
{
    public void ConvertToIfc(
        string stepPath,
        string outputPath,
        IProgress<ConversionProgress> converterProgress,
        double linearDeflection = 0.01, 
        double angularDeflection = 0.5);
}