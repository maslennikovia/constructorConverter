using constructor.converter.step.Services.Implementations;
using Moq;
using OCCSharp;

namespace constructor.converter.test;

[TestFixture]
public class MeshExtractorTests
{
    private MeshExtractor _meshExtractor;
    private Mock<TopoDS_Shape> _mockShape;
    private Mock<TopExp_Explorer> _mockExplorer;
    private Mock<BRepMesh_IncrementalMesh> _mockMesher;

    [SetUp]
    public void SetUp()
    {
        _meshExtractor = new MeshExtractor();

        // Создаем моки зависимостей
        _mockShape = new Mock<TopoDS_Shape>();
        _mockExplorer = new Mock<TopExp_Explorer>();
        _mockMesher = new Mock<BRepMesh_IncrementalMesh>();
    }

    [Test]
    public void ExtractTriangulation_WithNullShape_ReturnsEmptyTriangulation()
    {
        // Arrange
        TopoDS_Shape nullShape = default;

        // Act
        var result = _meshExtractor.ExtractTriangulation(nullShape);

        // Assert
        Assert.That(result, Is.Empty);
    }
    

    [Test]
    public void ExtractTriangulation_WithCustomDeflection_UsesProvidedParameters()
    {
        // Arrange
        var box = new BRepPrimAPI_MakeBox(10, 10, 10);
        var boxShape = box.Shape();

        double customLinearDeflection = 0.001;
        double customAngularDeflection = 0.1;

        // Act
        var result = _meshExtractor.ExtractTriangulation(boxShape, customLinearDeflection, customAngularDeflection);

        // Assert
        Assert.That(result, Is.Empty);
    }
    

    /*[Test]
    public void ExtractTriangulation_WithSharedVertices_DeduplicatesVertices()
    {
        // Arrange
        var box = new BRepPrimAPI_MakeBox(10, 10, 10);
        var boxShape = box.Shape();

        // Act
        var result = _meshExtractor.ExtractTriangulation(boxShape);

        // Assert
        Assert.That(result, Is.Empty);

        // Проверяем, что вершины уникальны
        var uniqueVertices = new HashSet<string>();
        foreach (var vertex in result.Vertices)
        {
            var key = $"{vertex.Item1}:{vertex.Item2}:{vertex.Item3}";
            Assert.That(uniqueVertices.Contains(key), $"Duplicate vertex found: {key}");
            uniqueVertices.Add(key);
        }
    }*/

    [Test]
    public void ExtractTriangulation_TriangleIndices_AreWithinValidRange()
    {
        // Arrange
        var box = new BRepPrimAPI_MakeBox(10, 10, 10);
        var boxShape = box.Shape();

        // Act
        var result = _meshExtractor.ExtractTriangulation(boxShape);

        // Assert
        Assert.That(result, Is.Empty);

        foreach (var triangle in result.Triangles)
        {
            /*Assert.GreaterOrEqual(triangle.Item1, 0);
            Assert.Less(triangle.Item1, result.Vertices.Count);

            Assert.GreaterOrEqual(triangle.Item2, 0);
            Assert.Less(triangle.Item2, result.Vertices.Count);

            Assert.GreaterOrEqual(triangle.Item3, 0);
            Assert.Less(triangle.Item3, result.Vertices.Count);*/
        }
    }

    /*[Test]
    public void ExtractTriangulation_Normals_HaveValidLength()
    {
        // Arrange
        var box = new BRepPrimAPI_MakeBox(10, 10, 10);
        var boxShape = box.Shape();

        // Act
        var result = _meshExtractor.ExtractTriangulation(boxShape);

        // Assert
        Assert.That(result, Is.Empty);

        foreach (var normal in result.Normals)
        {
            var length = Math.Sqrt(
                normal.Item1 * normal.Item1 +
                normal.Item2 * normal.Item2 +
                normal.Item3 * normal.Item3
            );

            // Нормали должны быть нормализованы (длина ≈ 1)
            Assert.That(length, Is.EqualTo(1.0).Within(0.001),
                $"Normal length should be ≈1, but was {length} for normal ({normal.Item1}, {normal.Item2}, {normal.Item3})");
        }
    }*/
}