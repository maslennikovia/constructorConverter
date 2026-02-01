using constructor.converter.step.Extensions;
using constructor.converter.step.Models;
using constructor.converter.step.Services.Abstractions;
using Xbim.Common;
using Xbim.Common.Step21;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.GeometryResource;
using Xbim.Ifc4.GeometricModelResource;
using Xbim.Ifc4.ProductExtension;
using Xbim.Ifc4.SharedBldgElements;
using Xbim.Ifc4.GeometricConstraintResource;
using Xbim.Ifc4.ProfileResource;
using Xbim.Ifc4.RepresentationResource;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.PresentationDefinitionResource;
using Xbim.Ifc4.PropertyResource;
using Xbim.IO;
using Xbim.IO.Memory;
using Xbim.Ifc4.TopologyResource;
using Xbim.Ifc4.UtilityResource;

namespace constructor.converter.step.Services.Implementations;

public class IfcService : IIfcService
{
    private Dictionary<string, IfcCartesianPointList3D> _sharedPointLists = new();
    
    public void CreateIfcModelFromTriangulation(TriangulationData triangulation,
        string outputPath)
    {
        // Создание модели с использованием MemoryModel
        using (var model = IfcStore.Create(XbimSchemaVersion.Ifc4, XbimStoreType.EsentDatabase))
        {
            using (var transaction = model.BeginTransaction("Create building"))
            {
                // Создание проекта
                var project = model.Instances.New<IfcProject>();
                project.Name = "Step building";
                project.Initialize(ProjectUnits.SIUnitsUK);

                // Создание сайта
                var site = model.Instances.New<IfcSite>();
                site.Name = "Plot";
                site.GlobalId = XbimExtension.GenerateIfcGlobalId();
                site.OwnerHistory = project.OwnerHistory;
                // Создание здания
                var building = model.Instances.New<IfcBuilding>();
                building.GlobalId = XbimExtension.GenerateIfcGlobalId();
                building.Name = "Main building";
                building.OwnerHistory = project.OwnerHistory;
                
                // Создание этажа
                var buildingStorey = model.Instances.New<IfcBuildingStorey>();
                buildingStorey.Name = "First floor";
                buildingStorey.Elevation = 0.0;
                buildingStorey.GlobalId = XbimExtension.GenerateIfcGlobalId();
                buildingStorey.OwnerHistory = project.OwnerHistory;
                
                var projectAggregates = model.Instances.New<IfcRelAggregates>();
                projectAggregates.GlobalId = XbimExtension.GenerateIfcGlobalId();
                projectAggregates.OwnerHistory = project.OwnerHistory;
                projectAggregates.RelatingObject = project;
                projectAggregates.RelatedObjects.Add(site);
    
                // Site → Building
                var siteAggregates = model.Instances.New<IfcRelAggregates>();
                siteAggregates.GlobalId = XbimExtension.GenerateIfcGlobalId();
                siteAggregates.OwnerHistory = project.OwnerHistory;
                siteAggregates.RelatingObject = site;
                siteAggregates.RelatedObjects.Add(building);
    
                // Building → Storey
                var buildingAggregates = model.Instances.New<IfcRelAggregates>();
                buildingAggregates.GlobalId = XbimExtension.GenerateIfcGlobalId();
                buildingAggregates.OwnerHistory = project.OwnerHistory;
                buildingAggregates.RelatingObject = building;
                buildingAggregates.RelatedObjects.Add(buildingStorey);

                // Иерархия объектов
                project.AddSite(site);
                site.AddBuilding(building);
                
                transaction.Commit();
            }
            
            int trianglesCount = triangulation.Triangles.Count;
            int elementCounter = 0;
            int batchSize = 100000;

            
            for (int i = 0; i < trianglesCount; i += batchSize)
            {
                using (var batchTransaction = model.BeginTransaction($"Create element batch {elementCounter + 1}"))
                {
                    var project = model.Instances.OfType<IfcProject>().First();
                    var buildingStorey = model.Instances.OfType<IfcBuildingStorey>().First();
                    var context = model.Instances.OfType<IfcGeometricRepresentationContext>().First();

                    // Создаем часть триангуляции
                    var partData = ExtractTriangulationPart(triangulation, i, Math.Min(batchSize, trianglesCount - i));
                    var element = CreateTriangulatedElement(
                        partData, 
                        model, 
                        context,
                        project.OwnerHistory);
                
                    // Добавляем элемент в этаж
                    buildingStorey.AddElement(element);
                
                    batchTransaction.Commit();
                    elementCounter++;
                }
            }
            // Сохранение модели
            model.SaveAs(outputPath, StorageType.IfcZip);
            Console.WriteLine("Модель успешно сохранена!");
        }
    }
    public void CreateIfcModel(StepFileInfo stepData,
        string outputPath,
        double linearDeflection = 0.01, 
        double angularDeflection = 0.5)
    {
        // Создание модели с использованием MemoryModel
        using (var model = IfcStore.Create(XbimSchemaVersion.Ifc4, XbimStoreType.EsentDatabase))
        {
            using (var transaction = model.BeginTransaction("Create building"))
            {
                // Создание проекта
                var project = model.Instances.New<IfcProject>();
                project.Name = "Step building";
                project.Initialize(ProjectUnits.SIUnitsUK);

                // Создание сайта
                var site = model.Instances.New<IfcSite>();
                site.Name = "Plot";
                site.GlobalId = XbimExtension.GenerateIfcGlobalId();
                site.OwnerHistory = project.OwnerHistory;
                // Создание здания
                var building = model.Instances.New<IfcBuilding>();
                building.GlobalId = XbimExtension.GenerateIfcGlobalId();
                building.Name = "Main building";
                building.OwnerHistory = project.OwnerHistory;
                
                // Создание этажа
                var buildingStorey = model.Instances.New<IfcBuildingStorey>();
                buildingStorey.Name = "First floor";
                buildingStorey.Elevation = 0.0;
                buildingStorey.GlobalId = XbimExtension.GenerateIfcGlobalId();
                buildingStorey.OwnerHistory = project.OwnerHistory;
                
                var projectAggregates = model.Instances.New<IfcRelAggregates>();
                projectAggregates.GlobalId = XbimExtension.GenerateIfcGlobalId();
                projectAggregates.OwnerHistory = project.OwnerHistory;
                projectAggregates.RelatingObject = project;
                projectAggregates.RelatedObjects.Add(site);
    
                // Site → Building
                var siteAggregates = model.Instances.New<IfcRelAggregates>();
                siteAggregates.GlobalId = XbimExtension.GenerateIfcGlobalId();
                siteAggregates.OwnerHistory = project.OwnerHistory;
                siteAggregates.RelatingObject = site;
                siteAggregates.RelatedObjects.Add(building);
    
                // Building → Storey
                var buildingAggregates = model.Instances.New<IfcRelAggregates>();
                buildingAggregates.GlobalId = XbimExtension.GenerateIfcGlobalId();
                buildingAggregates.OwnerHistory = project.OwnerHistory;
                buildingAggregates.RelatingObject = building;
                buildingAggregates.RelatedObjects.Add(buildingStorey);

                // Иерархия объектов
                project.AddSite(site);
                site.AddBuilding(building);
                
                transaction.Commit();
            }
            
            MeshExtractor extractor = new MeshExtractor();
            var shape = stepData.Shapes[0];
            var triangulation = extractor.ExtractTriangulation(shape, linearDeflection, angularDeflection);
            int trianglesCount = triangulation.Triangles.Count;
            int elementCounter = 0;
            int batchSize = 100000;

            
            for (int i = 0; i < trianglesCount; i += batchSize)
            {
                using (var batchTransaction = model.BeginTransaction($"Create element batch {elementCounter + 1}"))
                {
                    var project = model.Instances.OfType<IfcProject>().First();
                    var buildingStorey = model.Instances.OfType<IfcBuildingStorey>().First();
                    var context = model.Instances.OfType<IfcGeometricRepresentationContext>().First();

                    // Создаем часть триангуляции
                    var partData = ExtractTriangulationPart(triangulation, i, Math.Min(batchSize, trianglesCount - i));
                    var element = CreateTriangulatedElement(
                        partData, 
                        model, 
                        context,
                        project.OwnerHistory);
                
                    // Добавляем элемент в этаж
                    buildingStorey.AddElement(element);
                
                    batchTransaction.Commit();
                    elementCounter++;
                }
            }
            // Сохранение модели
            model.SaveAs(outputPath, StorageType.IfcZip);
            Console.WriteLine("Модель успешно сохранена!");
        }
    }
    
    private IfcBuildingElement CreateTriangulatedElement(
        TriangulationData triangulation,
        IfcStore model,
        IfcGeometricRepresentationContext context,
        IfcOwnerHistory ownerHistory)
    {
        // 1. Создаем элемент (продукт)
        var element = model.Instances.New<IfcBuildingElementProxy>();
        element.GlobalId = XbimExtension.GenerateIfcGlobalId();
        element.OwnerHistory = ownerHistory;
        element.Name = "Triangulated Element";
        element.ObjectType = "Custom Geometry";
    
        // 2. Создаем геометрическое представление
        var shapeRep = CreateTriangulatedFaceSetXbim(triangulation, model, context);
    
        // 3. Создаем определение формы для элемента
        var shapeDefinition = model.Instances.New<IfcProductDefinitionShape>();
        shapeDefinition.Representations.Add(shapeRep);
    
        // 4. Присваиваем форму элементу
        element.Representation = shapeDefinition;
    
        return element;
    }
    private IfcShapeRepresentation CreateTriangulatedFaceSetXbim(
        TriangulationData triangulation,
        IfcStore model,
        IfcGeometricRepresentationContext context)
    {
        var faceSet = model.Instances.New<IfcTriangulatedFaceSet>(tfs => {
            tfs.Closed = true;
            // определяем координаты вершин
            tfs.Coordinates = model.Instances.New<IfcCartesianPointList3D>(pl => {
                
                for (int i = 0; i < triangulation.Vertices.Count; i++)
                {
                    pl.CoordList.GetAt(i).AddRange(new IfcLengthMeasure[]
                        { triangulation.Vertices[i].X,
                            triangulation.Vertices[i].Y,
                            triangulation.Vertices[i].Z
                            
                        });
                }
            });
            // Indices are 1 based in IFC !!!
            for (int i = 0; i < triangulation.Triangles.Count; i++)
            {
                tfs.CoordIndex.GetAt(i).AddRange(new IfcPositiveInteger[]
                    { triangulation.Triangles[i].Item1 + 1,
                        triangulation.Triangles[i].Item2 + 1,
                        triangulation.Triangles[i].Item3 + 1
                    });
            }
        });
        
        var shapeRep = model.Instances.New<IfcShapeRepresentation>();
        shapeRep.ContextOfItems = context;
        shapeRep.RepresentationIdentifier = "Body";
        shapeRep.RepresentationType = "Tessellation";
        shapeRep.Items.Add(faceSet);
        
        return shapeRep;
    }
    
    private IfcCartesianPointList3D GetOrCreateSharedPointList(
        List<(double, double, double)> vertices, 
        IfcStore model,
        string hash)
    {
        if (!_sharedPointLists.TryGetValue(hash, out var pointList))
        {
            pointList = model.Instances.New<IfcCartesianPointList3D>();
            // Добавьте точки
            _sharedPointLists[hash] = pointList;
        }
        return pointList;
    }
    

    private (List<Tuple<double, double, double>>, Dictionary<int, int>) RemoveDuplicateVertices(
        List<Tuple<double, double, double>> vertices, double tolerance)
    {
        var uniqueVertices = new List<Tuple<double, double, double>>();
        var vertexMap = new Dictionary<int, int>();
        
        // Используем пространственное хеширование для быстрого поиска дубликатов
        var cellSize = tolerance * 10;
        var spatialGrid = new Dictionary<string, int>();
        
        for (int i = 0; i < vertices.Count; i++)
        {
            var vertex = vertices[i];
            
            // Округляем координаты для группировки
            var roundedX = Math.Round(vertex.Item1 / tolerance) * tolerance;
            var roundedY = Math.Round(vertex.Item2 / tolerance) * tolerance;
            var roundedZ = Math.Round(vertex.Item3 / tolerance) * tolerance;
            
            // Ключ для пространственной ячейки
            var cellKey = $"{(int)(roundedX / cellSize)}_{(int)(roundedY / cellSize)}_{(int)(roundedZ / cellSize)}";
            
            // Ищем похожие вершины в этой ячейке
            bool foundDuplicate = false;
            if (spatialGrid.ContainsKey(cellKey))
            {
                // Проверяем все вершины в ячейке
                var candidates = spatialGrid.Where(kvp => kvp.Key == cellKey);
                foreach (var candidate in candidates)
                {
                    var existingVertex = uniqueVertices[candidate.Value];
                    if (Math.Abs(existingVertex.Item1 - roundedX) < tolerance &&
                        Math.Abs(existingVertex.Item2 - roundedY) < tolerance &&
                        Math.Abs(existingVertex.Item3 - roundedZ) < tolerance)
                    {
                        vertexMap[i] = candidate.Value;
                        foundDuplicate = true;
                        break;
                    }
                }
            }
            
            if (!foundDuplicate)
            {
                vertexMap[i] = uniqueVertices.Count;
                uniqueVertices.Add(new Tuple<double, double, double>(roundedX, roundedY, roundedZ));
                spatialGrid[cellKey] = uniqueVertices.Count - 1;
            }
        }
        
        return (uniqueVertices, vertexMap);
    }


    /// <summary>
    /// Извлекает часть триангуляции из основного набора данных
    /// </summary>
    private TriangulationData ExtractTriangulationPart(TriangulationData source, int startIndex, int count)
    {
        var part = new TriangulationData();
        
        // Берем срез треугольников
        var triangleSlice = source.Triangles.Skip(startIndex).Take(count).ToList();
        
        // Собираем все уникальные вершины, используемые в этом срезе
        var vertexIndices = new HashSet<int>();
        var normalIndices = new HashSet<int>();
        
        foreach (var triangle in triangleSlice)
        {
            vertexIndices.Add(triangle.Item1);
            vertexIndices.Add(triangle.Item2);
            vertexIndices.Add(triangle.Item3);
            
            // Если есть нормали, собираем их индексы
            if (source.Normals != null && source.Normals.Count > 0)
            {
                // Предполагаем, что индексы нормалей совпадают с индексами вершин
                normalIndices.Add(triangle.Item1);
                normalIndices.Add(triangle.Item2);
                normalIndices.Add(triangle.Item3);
            }
        }
        
        // Создаем словари для переиндексации
        var vertexMap = new Dictionary<int, int>();
        var normalMap = new Dictionary<int, int>();
        
        // Копируем вершины с новыми индексами
        int newIndex = 0;
        foreach (var oldIndex in vertexIndices.OrderBy(x => x))
        {
            part.Vertices.Add(source.Vertices[oldIndex]);
            vertexMap[oldIndex] = newIndex;
            newIndex++;
        }
        
        // Копируем нормали, если они есть
        if (source.Normals != null && source.Normals.Count > 0)
        {
            newIndex = 0;
            foreach (var oldIndex in normalIndices.OrderBy(x => x))
            {
                part.Normals.Add(source.Normals[oldIndex]);
                normalMap[oldIndex] = newIndex;
                newIndex++;
            }
        }
        
        // Переиндексируем треугольники
        foreach (var triangle in triangleSlice)
        {
            var newTriangle = new Tuple<int, int, int>(
                vertexMap[triangle.Item1],
                vertexMap[triangle.Item2],
                vertexMap[triangle.Item3]
            );
            part.Triangles.Add(new ValueTuple<int, int, int>(newTriangle.Item1, newTriangle.Item2, newTriangle.Item3));
        }
        
        return part;
    }

    private IfcCartesianPoint CreateCartesianPoint(IfcStore model, Tuple<double, double, double> coordinates)
    {
        var point = model.Instances.New<IfcCartesianPoint>();
        point.SetXYZ(coordinates.Item1, coordinates.Item2, coordinates.Item3);
        return point;
    }

        
        

    private void AddMetadata(MemoryModel model, StepFileInfo stepData, IIfcObjectDefinition product)
    {
        var pset = model.Instances.New<IfcPropertySet>();
        pset.Name = "STEP_Metadata";
        pset.GlobalId = XbimExtension.GenerateIfcGlobalId();

        foreach (var metadata in stepData.Metadata)
        {
            AddProperty(pset, metadata.Key, metadata.Value);
        }

        AddProperty(pset, "Conversion Date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        AddProperty(pset, "Converter Tool", "Xbim STEP to IFC Converter");

        var relDefines = model.Instances.New<IfcRelDefinesByProperties>();
        relDefines.RelatingPropertyDefinition = pset;
    }

    private void AddProperty(IfcPropertySet pset, string name, string value)
    {
        var property = pset.Model.Instances.New<IfcPropertySingleValue>();
        property.Name = name;
        property.NominalValue = new IfcText(value);
        pset.HasProperties.Add(property);
    }
}