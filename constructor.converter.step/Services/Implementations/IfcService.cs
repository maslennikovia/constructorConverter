using constructor.converter.step.Extensions;
using constructor.converter.step.Models;
using constructor.converter.step.Services.Abstractions;
using OCCSharp;
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
using Xbim.Common.Collections;

namespace constructor.converter.step.Services.Implementations;

public class IfcService : IIfcService
{
    private Dictionary<string, IfcCartesianPointList3D> _sharedPointLists = new();

    public void CreateIfcModelFromTriangulation(ModelData rootEntity,
        string outputPath)
    {
        IfcBuildingStorey buildingStorey = null!;
        IfcOwnerHistory ownerHistory = null!;
        IfcGeometricRepresentationContext context = null!;
        var faceSetCache = new Dictionary<int, IfcRepresentationMap>();
        // Создание модели с использованием MemoryModel
        using var model = IfcStore.Create(XbimSchemaVersion.Ifc4, XbimStoreType.EsentDatabase);
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
            buildingStorey = model.Instances.New<IfcBuildingStorey>();
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

            context = model.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
            if (context == null)
            {
                context = model.Instances.New<IfcGeometricRepresentationContext>();
                context.ContextType = "Model";
                context.ContextIdentifier = "Body";
                context.CoordinateSpaceDimension = 3;
                context.Precision = 1e-5;
            }

            ownerHistory = project.OwnerHistory;

            transaction.Commit();
        }
        
        CreateAllElements(
            model,
            rootEntity.Root,
            rootEntity.Triangulations,
            buildingStorey,
            faceSetCache,
            ownerHistory,
            context);

        // Сохранение модели
        model.SaveAs(outputPath, StorageType.Ifc);
        Console.WriteLine("Модель успешно сохранена!");
    }

    private void CreateAllElements(
        IfcStore model,
        ModelEntity rootEntity,
        Dictionary<int, TriangulationData> triangulations,
        IfcBuildingStorey buildingStorey,
        Dictionary<int, IfcRepresentationMap> faceSetCache,
        IfcOwnerHistory ownerHistory,
        IfcGeometricRepresentationContext context)
    {
        foreach (var children in rootEntity.Childrens)
        {
            if ((children.Triangulation == null || !children.Triangulation.Triangulations.Any()) &&
                children.Childrens.Count <= 0) continue;
            if (children.Childrens.Count > 0)
            {
                CreateAllElements(model, children, triangulations, buildingStorey, faceSetCache, ownerHistory, context);
            }
            else
            {
                using var txn = model.BeginTransaction($"Create elements batch {children.Name}");
                var element = CreateTriangulatedElement(
                    children.Triangulation!,
                    triangulations,
                    model,
                    context,
                    ownerHistory,
                    children.Name ?? "Unnamed",
                    faceSetCache);
                buildingStorey.AddElement(element);
                txn.Commit();
            }
        }
    }

    private static void CollectNodesWithGeometry(ModelEntity node, List<ModelEntity> result)
    {
        if (node.Triangulation != null && node.Triangulation.Triangulations.Any())
            result.Add(node);

        foreach (var child in node.Childrens)
            CollectNodesWithGeometry(child, result);
    }

    private static IfcBuildingElementProxy CreateTriangulatedElement(
        ElementTriangulation triangulation,
        Dictionary<int, TriangulationData> triangulations,
        IModel model,
        IfcGeometricRepresentationContext context,
        IfcOwnerHistory ownerHistory,
        string name,
        Dictionary<int, IfcRepresentationMap> mapCache)
    {
        var shape = model.Instances.New<IfcProductDefinitionShape>();
        var representation = model.Instances.New<IfcShapeRepresentation>();
        representation.ContextOfItems = context;
        representation.RepresentationType = "MappedRepresentation";
        representation.RepresentationIdentifier = "Body";
        
        foreach (var face in triangulation.Triangulations)
        {
            if (!triangulations.TryGetValue(face.Item1, out var data))
                continue;
            
            if (!mapCache.TryGetValue(face.Item1, out var map))
            {
                map = CreateMapForGeometry(data, model, context);
                mapCache[face.Item1] = map;
            }
            var mappedItem = model.Instances.New<IfcMappedItem>();
            mappedItem.MappingSource = map; // ссылка на карту

            // 3. Задаём трансформацию из матрицы face.Item2
            var transform = CreateTransformationFromMatrix(face.Item2, model);
            mappedItem.MappingTarget = transform;
            representation.Items.Add(mappedItem);
        }
        
        shape.Representations.Add(representation);

        // 5. Создаём элемент (IfcBuildingElementProxy)
        var element = model.Instances.New<IfcBuildingElementProxy>();
        element.Name = name;
        element.OwnerHistory = ownerHistory;
        element.Representation = shape;

        // 6. Задаём локальное размещение (предполагаем глобальные координаты)
        var placement = model.Instances.New<IfcLocalPlacement>();
        var axis2Placement = model.Instances.New<IfcAxis2Placement3D>();
        var origin = model.Instances.New<IfcCartesianPoint>();
        origin.X = 0;
        origin.Y = 0;
        origin.Z = 0;
        axis2Placement.Location = origin;
        placement.RelativePlacement = axis2Placement;
        element.ObjectPlacement = placement;

        return element;
    }
    
    private static IfcCartesianTransformationOperator3D CreateTransformationFromMatrix(
        gp_Trsf trsf,
        IModel model)
    {
        // Извлечение смещения (translation)
        gp_XYZ translation = trsf.TranslationPart();

        // Масштабный коэффициент (равномерный)
        double scale = trsf.ScaleFactor();

        // Создание оператора трансформации
        var op = model.Instances.New<IfcCartesianTransformationOperator3D>();

        // Установка точки смещения
        var origin = model.Instances.New<IfcCartesianPoint>();
        origin.X = translation.X();
        origin.Y = translation.Y();
        origin.Z = translation.Z();
        op.LocalOrigin = origin;

        // Если масштаб не равен 1, сохраняем его
        if (Math.Abs(scale - 1.0) > 1e-9)
            op.Scale = scale;

        // Получение элементов матрицы поворота (3x3) из gp_Trsf
        double m11 = trsf.Value(1, 1);
        double m12 = trsf.Value(1, 2);
        double m13 = trsf.Value(1, 3);
        double m21 = trsf.Value(2, 1);
        double m22 = trsf.Value(2, 2);
        double m23 = trsf.Value(2, 3);
        double m31 = trsf.Value(3, 1);
        double m32 = trsf.Value(3, 2);
        double m33 = trsf.Value(3, 3);

        // Нормализация столбцов для получения направлений (без учёта масштаба)
        double xDirX = m11 / scale;
        double xDirY = m21 / scale;
        double xDirZ = m31 / scale;

        double yDirX = m12 / scale;
        double yDirY = m22 / scale;
        double yDirZ = m32 / scale;

        double zDirX = m13 / scale;
        double zDirY = m23 / scale;
        double zDirZ = m33 / scale;

        // Задание осей направления
        var axis1 = model.Instances.New<IfcDirection>();
        axis1.DirectionRatios.AddRange([new IfcReal(xDirX), new IfcReal(xDirY), new IfcReal(xDirZ)]);
        op.Axis1 = axis1;

        var axis2 = model.Instances.New<IfcDirection>();
        axis2.DirectionRatios.AddRange([new IfcReal(yDirX), new IfcReal(yDirY), new IfcReal(yDirZ)]);
        op.Axis2 = axis2;

        var axis3 = model.Instances.New<IfcDirection>();
        axis3.DirectionRatios.AddRange([new IfcReal(zDirX), new IfcReal(zDirY), new IfcReal(zDirZ)]);
        op.Axis3 = axis3;

        return op;
    }
    
    private static IfcRepresentationMap CreateMapForGeometry(
        TriangulationData data,
        IModel model,
        IfcGeometricRepresentationContext context)
    {
        // Создаём представление внутри карты
        var mapRepresentation = model.Instances.New<IfcShapeRepresentation>();
        mapRepresentation.ContextOfItems = context;
        mapRepresentation.RepresentationType = "Tessellation";
        mapRepresentation.RepresentationIdentifier = "MappedGeometry";

        // Создаём FaceSet без трансформации
        var faceSet = model.Instances.New<IfcTriangulatedFaceSet>();
        faceSet.Closed = true;

        // Список точек (локальные координаты)
        var pointList = model.Instances.New<IfcCartesianPointList3D>();
        for (int i = 0; i < data.Vertices.Count; i++)
        {
            pointList.CoordList.GetAt(i).AddRange(new IfcLengthMeasure[]
            {
                data.Vertices[i].X,
                data.Vertices[i].Y,
                data.Vertices[i].Z
            });
        }
        faceSet.Coordinates = pointList;

        // Индексы (1-based)
        for (int i = 0; i < data.Triangles.Count; i++)
        {
            faceSet.CoordIndex.GetAt(i).AddRange(new IfcPositiveInteger[]
            {
                data.Triangles[i].Item1 + 1,
                data.Triangles[i].Item2 + 1,
                data.Triangles[i].Item3 + 1
            });
        }

        mapRepresentation.Items.Add(faceSet);

        // Создаём карту
        var map = model.Instances.New<IfcRepresentationMap>();
        map.MappedRepresentation = mapRepresentation;
        // Базисная точка карты обычно (0,0,0) – можно оставить по умолчанию
        var origin = model.Instances.New<IfcAxis2Placement3D>();
        origin.Location = model.Instances.New<IfcCartesianPoint>(p => { p.X = 0; p.Y = 0; p.Z = 0; });
        map.MappingOrigin = origin;

        return map;
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
    private TriangulationData ExtractTriangulationPart(TriangulationData source)
    {
        var part = new TriangulationData();

        // Берем срез треугольников
        var triangleSlice = source.Triangles;

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