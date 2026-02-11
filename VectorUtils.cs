using System.Numerics;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace JailBreak;

public static class VectorUtils
{
    public static int GetVectorDistance(Vector first, Vector second, int zOffset = 0)
    {
        return (int)Math.Sqrt(Math.Pow(second.X - first.X, 2) + Math.Pow(second.Y - first.Y, 2) +
                              Math.Pow(second.Z - first.Z + zOffset, 2));
    }

    public static Vector Normalize(Vector vector)
    {
        var length = (float)Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y + vector.Z * vector.Z);
        var normalizedVector = new Vector(vector.X / length, vector.Y / length, vector.Z / length);

        return normalizedVector;
    }

    public static Vector Cross(Vector vector1, Vector vector2)
    {
        return new Vector(
            (vector1.Y * vector2.Z) - (vector1.Z * vector2.Y),
            (vector1.Z * vector2.X) - (vector1.X * vector2.Z),
            (vector1.X * vector2.Y) - (vector1.Y * vector2.X)
        );
    }
    
}