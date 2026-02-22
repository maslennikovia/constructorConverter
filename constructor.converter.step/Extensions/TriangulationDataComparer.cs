using constructor.converter.step.Models;

namespace constructor.converter.step.Extensions;

public static class TriangulationDataComparer
{
    public static bool Equals(TriangulationData x, TriangulationData y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;

        // Сравнение списков поэлементно с учётом порядка
        return x.Vertices.SequenceEqual(y.Vertices)
               && x.Normals.SequenceEqual(y.Normals)
               && x.Triangles.SequenceEqual(y.Triangles);
    }

    public static int GetHashCode(TriangulationData obj)
    {
        if (obj == null) return 0;

        unchecked
        {
            int hash = 17;

            foreach (var v in obj.Vertices)
                hash = hash * 31 + v.GetHashCode();

            foreach (var n in obj.Normals)
                hash = hash * 31 + n.GetHashCode();

            foreach (var t in obj.Triangles)
                hash = hash * 31 + t.GetHashCode();

            return hash;
        }
    }
}