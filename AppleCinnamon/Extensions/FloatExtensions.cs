using System;

namespace AppleCinnamon.Extensions;

public static class FloatExtensions
{
    public static bool IsEpsilon(this float number) => Math.Abs(number) < 0.001f;
    public static float Distance(this float lhs, float rhs) => Math.Abs(Math.Abs(lhs) - Math.Abs(rhs));
}