using constructor.converter.step.Models;
using constructor.converter.step.Services.Abstractions;
using OCCSharp;

namespace constructor.converter.step.Services.Implementations;

public class StepParser : IStepParser
{
    public static bool _use_triangulation = false;

    public TriangulationData GetTriangulationFromStepFile(
        string filePath,
        IProgress<ConversionProgress> converterProgress,
        string[] stages,
        int stage,
        double linearDeflection = 0.01,
        double angularDeflection = 0.5)
    {
        OcctConfiguration.Configure();

        // Создание STEP reader
        STEPControl_Reader reader = new STEPControl_Reader();

        // Чтение файла
        IFSelect_ReturnStatus status = reader.ReadFile(filePath);
        if (status != IFSelect_ReturnStatus.IFSelect_RetDone)
        {
            throw new Exception($"Не удалось прочитать файл: {status}");
        }

        var currentProgress = new ConversionProgress
        {
            Percentage = (0 * 100.0) / stages.Length,
            Stage = stages[stage],
            CurrentOperation = "Выполняется чтение файла"
        };
        converterProgress?.Report(currentProgress);
        // Перевод всех корневых entities в shapes
        var progress = new Message_ProgressRange();
        reader.TransferRoots(progress);
        
        // Получение всех shapes
        TopoDS_Shape shape = reader.OneShape();
        if (!shape.IsNull())
        {
            var mesher = new MeshExtractor();
            return mesher.ExtractTriangulation(
                shape, converterProgress, stages, stage++, linearDeflection, angularDeflection);
        }

        return new TriangulationData();
    }

    public StepFileInfo ReadStepFile(
        string filePath,
        IProgress<ConversionProgress> converterProgress,
        double linearDeflection = 0.01,
        double angularDeflection = 0.5,
        int stageCount = 1)
    {
        var result = new StepFileInfo();

        OcctConfiguration.Configure();

        // Создание STEP reader
        STEPControl_Reader reader = new STEPControl_Reader();

        // Чтение файла
        IFSelect_ReturnStatus status = reader.ReadFile(filePath);
        if (status != IFSelect_ReturnStatus.IFSelect_RetDone)
        {
            throw new Exception($"Не удалось прочитать файл: {status}");
        }

        var currentProgress = new ConversionProgress
        {
            Percentage = (0 * 100.0) / stageCount,
            Stage = "Чтение файла STEP",
            CurrentOperation = "Выполняется чтение файла"
        };
        converterProgress?.Report(currentProgress);
        // Перевод всех корневых entities в shapes
        var progress = new Message_ProgressRange();
        reader.TransferRoots(progress);


        // Получение всех shapes
        TopoDS_Shape shape = reader.OneShape();
        if (!shape.IsNull())
        {
            // Если это compound, разбираем на отдельные shapes
            /*if (shape.ShapeType() == TopAbs_ShapeEnum.TopAbs_COMPOUND)
            {
                var explorer = new TopExp_Explorer(shape, TopAbs_ShapeEnum.TopAbs_SOLID,
                    TopAbs_ShapeEnum.TopAbs_COMPSOLID);
                while (explorer.More())
                {
                    var current = explorer.Current();
                    var loc = current.Location();
                    result.Shapes.Add(explorer.Current());
                    explorer.Next();
                }

                // Если нет solids, ищем shells
                if (result.Shapes.Count == 0)
                {
                    explorer = new TopExp_Explorer(shape, TopAbs_ShapeEnum.TopAbs_SHELL, TopAbs_ShapeEnum.TopAbs_SOLID);
                    while (explorer.More())
                    {
                        result.Shapes.Add(explorer.Current());
                        explorer.Next();
                    }
                }
            }
            else
            {
                result.Shapes.Add(shape);
            }*/
            BRepMesh_IncrementalMesh mesher = new BRepMesh_IncrementalMesh(
                shape,
                theLinDeflection: linearDeflection, // Отклонение (меньше = точнее, но больше треугольников)
                isRelative: true, // Относительное отклонение
                theAngDeflection: angularDeflection, // Угловой параметр (в радианах) 
                isInParallel: true // Параллельная обработка
            );
            mesher.Perform(progress);
            result.Shapes.Add(shape);
        }

        // Извлечение метаданных
        //ExtractMetadata(reader, result);
        var finalProgress = new ConversionProgress
        {
            Percentage = (100.0) / stageCount,
            Stage = "Чтение файла STEP",
            CurrentOperation = "Чтение файла выполнено"
        };
        converterProgress?.Report(finalProgress);
        return result;
    }

    /*static void ExtractMetadata(STEPControl_Reader reader, StepFileInfo result)
    {
        try
        {
            // Получение рабочей сессии и STEP модели
            var workSession = reader.WS();
            var stepModel = workSession.Model();

            if (stepModel == null)
                return;

            // Обход всех entities в модели
            for (int i = 1; i <= stepModel.NbEntities(); i++)
            {
                var entity = stepModel.Entities()[i];
                if (entity.IsNull())
                    continue;

                // Извлечение информации о продукте
                if (entity is StepBasic_Product product)
                {
                    string productId = product.Id();
                    string name = product.Name();
                    string description = product.Description();

                    if (!string.IsNullOrEmpty(name))
                    {
                        result.Metadata[$"Product_{productId}_Name"] = name;
                    }
                    if (!string.IsNullOrEmpty(description))
                    {
                        result.Metadata[$"Product_{productId}_Description"] = description;
                    }
                }

                // Извлечение информации о представлении продукта
                if (entity is StepRepr_ProductDefinitionShape productDefShape)
                {
                    string defId = productDefShape.Id();
                    string defName = productDefShape.Name();

                    if (!string.IsNullOrEmpty(defName))
                    {
                        result.Metadata[$"ProductDefinition_{defId}_Name"] = defName;
                    }
                }

                // Извлечение информации о контексте
                if (entity is StepBasic_ProductContext productContext)
                {
                    string contextName = productContext.Name();
                    if (!string.IsNullOrEmpty(contextName))
                    {
                        result.Metadata["ProductContext"] = contextName;
                    }
                }

                // Извлечение информации о единицах измерения
                if (entity is StepRepr_RepresentationContext representationContext)
                {
                    string contextType = representationContext.ContextType();
                    if (!string.IsNullOrEmpty(contextType))
                    {
                        result.Metadata["RepresentationContext"] = contextType;
                    }
                }
            }

            // Получение информации о единицах измерения
            /*var units = workSession.Units();
            if (!units.IsNull())
            {
                result.Metadata["LengthUnit"] = units.LengthUnit().ToString();
                result.Metadata["AngleUnit"] = units.AngleUnit().ToString();
            }#1#
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при извлечении метаданных: {ex.Message}");
        }
    }*/

    static void ProcessShape(TopoDS_Shape shape)
    {
        Console.WriteLine($"\nОбработка формы типа: {shape.ShapeType()}");

        // Анализ bounding box
        Bnd_Box bbox = new Bnd_Box();
        BRepBndLib.Add(shape, bbox, _use_triangulation);
        double xMin = 0, yMin = 0, zMin = 0, xMax = 0, yMax = 0, zMax = 0;
        bbox.Get(ref xMin, ref yMin, ref zMin, ref xMax, ref yMax, ref zMax);

        Console.WriteLine($"Bounding Box:");
        Console.WriteLine($"  Min: ({xMin:F2}, {yMin:F2}, {zMin:F2})");
        Console.WriteLine($"  Max: ({xMax:F2}, {yMax:F2}, {zMax:F2})");
        Console.WriteLine($"  Размеры: {xMax - xMin:F2} x {yMax - yMin:F2} x {zMax - zMin:F2}");

        // Подсчет компонентов
        var solidCount = CountShapes(shape, TopAbs_ShapeEnum.TopAbs_SOLID);
        var faceCount = CountShapes(shape, TopAbs_ShapeEnum.TopAbs_FACE);
        var edgeCount = CountShapes(shape, TopAbs_ShapeEnum.TopAbs_EDGE);
        var vertexCount = CountShapes(shape, TopAbs_ShapeEnum.TopAbs_VERTEX);

        Console.WriteLine($"Компоненты:");
        Console.WriteLine($"  Solids: {solidCount}");
        Console.WriteLine($"  Faces: {faceCount}");
        Console.WriteLine($"  Edges: {edgeCount}");
        Console.WriteLine($"  Vertices: {vertexCount}");
    }

    static int CountShapes(TopoDS_Shape shape, TopAbs_ShapeEnum shapeType)
    {
        int count = 0;
        var explorer = new TopExp_Explorer(shape, shapeType, TopAbs_ShapeEnum.TopAbs_COMPOUND);
        while (explorer.More())
        {
            count++;
            explorer.Next();
        }

        return count;
    }
}