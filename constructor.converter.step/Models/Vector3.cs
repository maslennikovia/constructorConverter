namespace constructor.converter.step.Models;

public struct Vector3
{
    public double X, Y, Z;
    public Vector3(double x, double y, double z) => (X, Y, Z) = (x, y, z);
}