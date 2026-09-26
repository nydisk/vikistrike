using System.Numerics;

namespace vikistrike.math;

// ReSharper disable once InconsistentNaming
public readonly record struct Vec3i(int X, int Y, int Z) {
    public static Vec3i operator +(Vec3i a, Vec3i b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static Vec3i operator -(Vec3i a, Vec3i b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static Vec3i operator *(Vec3i a, int s) => new(a.X * s, a.Y * s, a.Z * s);
    public static explicit operator Vector3(Vec3i v) => new(v.X, v.Y, v.Z);
}