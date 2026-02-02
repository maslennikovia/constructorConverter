using constructor.converter.step.Models;
using OCCSharp;

namespace constructor.converter.step.Services.Implementations;

public class MeshExtractor
{
    public TriangulationData ExtractTriangulation(TopoDS_Shape shape, 
        IProgress<ConversionProgress> converterProgress,
        string[] stages,
        int stage,
        double linearDeflection = 0.01, 
        double angularDeflection = 0.5)
    {
        var result = new TriangulationData();
        var currentProgress = new ConversionProgress
        {
            Percentage = (0 * 100.0) / stages.Length,
            Stage = stages[stage],
            CurrentOperation = "Создание триангуляции"
        };
        converterProgress?.Report(currentProgress);
        // Создаем мешер с заданными параметрами
        var mesher = new BRepMesh_IncrementalMesh(shape, linearDeflection, 
            false, angularDeflection, true);
        mesher.Perform(new Message_ProgressRange());
        
        // Словарь для отслеживания уже добавленных вершин
        var vertexMap = new Dictionary<(double, double, double), int>(
            new DoubleTupleComparer());
        int nextIndex = 0;
        
        // Обходим все грани в модели
        var faceExplorer = new TopExp_Explorer(shape, TopAbs_ShapeEnum.TopAbs_FACE, TopAbs_ShapeEnum.TopAbs_WIRE);
        while (faceExplorer.More())
        {
            var face = TopoDS.Face(faceExplorer.Current());
            var location = new TopLoc_Location();
            
            // Получаем триангуляцию для текущей грани
            var triangulation = BRep_Tool.Triangulation(face, location, 0);
            if (triangulation != null && triangulation.NbNodes() > 0)
            {
                ProcessFaceTriangulationOptimized(triangulation, location, face, 
                    result, vertexMap, ref nextIndex);
            }
            
            faceExplorer.Next();
        }
        return result;
    }
    

    private void ProcessFaceTriangulationOptimized(
        Poly_Triangulation triangulation,
        TopLoc_Location location,
        TopoDS_Face face,
        TriangulationData result,
        Dictionary<(double, double, double), int> vertexMap,
        ref int nextIndex)
    {
        var nodesCount = triangulation.NbNodes();
        var trianglesCount = triangulation.NbTriangles();
        var hasNormals = triangulation.HasNormals();
        
        // 7. Используем массивы вместо списков для временных данных
        var faceVertexIndices = new int[nodesCount];
        var transform = location.Transformation();
        
        // 8. Подготавливаем массивы для кэширования
        var uvNodesExist = triangulation.HasUVNodes();
        var uvArray = uvNodesExist ? new gp_Pnt2d[nodesCount] : null;
        
        if (uvNodesExist)
        {
            for (int i = 0; i < nodesCount; i++)
            {
                uvArray[i] = triangulation.UVNode(i + 1); // OpenCASCADE индексирует с 1
            }
        }
        
        // 9. Обрабатываем вершины
        for (int i = 0; i < nodesCount; i++)
        {
            var vertex = triangulation.Node(i + 1).Transformed(transform);

            if (!vertexMap.TryGetValue((vertex.X(), vertex.Y(), vertex.Z()), out int vertexIndex))
            {
                // Новая вершина
                vertexIndex = nextIndex++;
                vertexMap[(vertex.X(), vertex.Y(), vertex.Z())] = vertexIndex;
                
                result.Vertices.Add(new Vector3(vertex.X(), vertex.Y(), vertex.Z()));
                
                // Вычисляем нормаль
                if (hasNormals)
                {
                    var occNormal = triangulation.Normal(i + 1);
                    result.Normals.Add(new Vector3(occNormal.X(), occNormal.Y(), occNormal.Z()));
                }
                else if (uvNodesExist)
                {
                    var normal = ComputeFaceNormalAtUV(face, uvArray[i]);
                    result.Normals.Add(normal);
                }
                else
                {
                    result.Normals.Add(new Vector3(0, 0, 1));
                }
            }
            
            faceVertexIndices[i] = vertexIndex;
        }
        
        // 10. Обрабатываем треугольники
        for (int i = 0; i < trianglesCount; i++)
        {
            var triangle = triangulation.Triangle(i + 1);
            int n1 = 0; int n2 = 0;int n3 = 0;
            triangle.Get(ref n1, ref n2, ref n3);
            
            // Индексы в OpenCASCADE начинаются с 1
            n1--; n2--; n3--;
            
            result.Triangles.Add((
                faceVertexIndices[n1],
                faceVertexIndices[n2],
                faceVertexIndices[n3]
            ));
        }
    }
    
     private Vector3 ComputeFaceNormalAtUV(TopoDS_Face face,gp_Pnt2d uv)
    {
        try
        {
            var surface = BRep_Tool.Surface(face);
            gp_Pnt point = surface.Value(uv.X(), uv.Y());
            gp_Vec d1u = new gp_Vec();
            gp_Vec d1v = new gp_Vec();
            
            surface.D1(uv.X(), uv.Y(), point, d1u, d1v);
            
            // Вычисляем нормаль как векторное произведение
            var normal = d1u.Crossed(d1v);
            
            if (normal.Magnitude() > 1e-12)
            {
                normal.Normalize();
                
                // Убеждаемся, что нормаль направлена наружу
                if (face.Orientation() == TopAbs_Orientation.TopAbs_REVERSED)
                {
                    normal.Reverse();
                }
            }
            
            return new Vector3(normal.X(), normal.Y(), normal.Z());
        }
        catch
        {
            // Возвращаем нормаль по умолчанию в случае ошибки
            return new Vector3(0, 0, 1);
        }
    }
     
    private class DoubleTupleComparer : IEqualityComparer<(double, double, double)>
    {
        private const double Epsilon = 1e-6;
    
        public bool Equals((double, double, double) a, (double, double, double) b)
        {
            return Math.Abs(a.Item1 - b.Item1) < Epsilon &&
                   Math.Abs(a.Item2 - b.Item2) < Epsilon &&
                   Math.Abs(a.Item3 - b.Item3) < Epsilon;
        }
    
        public int GetHashCode((double, double, double) obj)
        {
            // Округляем до определенной точности для хэширования
            var x = Math.Round(obj.Item1 / Epsilon) * Epsilon;
            var y = Math.Round(obj.Item2 / Epsilon) * Epsilon;
            var z = Math.Round(obj.Item3 / Epsilon) * Epsilon;
        
            return HashCode.Combine(x, y, z);
        }
    }
}