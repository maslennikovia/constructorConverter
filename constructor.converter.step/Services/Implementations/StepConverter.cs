using constructor.converter.step.Models;
using constructor.converter.step.Services.Abstractions;

namespace constructor.converter.step.Services.Implementations;

public class StepConverter : IConstructorConverter
{
    public static readonly string[] Stages =
    [
        "Чтение STEP файла",
        "Парсинг геометрии",
        "Оптимизация сетки",
        "Создание IFC структуры",
        "Сохранение в файл"
    ];
    public void ConvertToIfc(string stepPath, string outputPath, IProgress<ConversionProgress> converterProgress, double linearDeflection,
        double angularDeflection)
    {
        
        var triangulationData = new StepParser().GetTriangulationFromStepFile(stepPath, converterProgress, Stages, 0, linearDeflection, angularDeflection);
        new IfcService().CreateIfcModelFromTriangulation(triangulationData, outputPath);
    }
}