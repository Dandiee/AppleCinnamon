using System;
using System.Collections.Generic;
using AppleCinnamon.Common;
using AppleCinnamon.Helper;
using SharpDX;

namespace AppleCinnamon.ChunkGenerators
{
    public static class ShapeGenerator
    {
        public static IEnumerable<Int3> RectangleWithBevel(Int3 minCorner, Int3 size)
        {
            var maxCorner = minCorner + size - Int3.One;
            for (var i = minCorner.X; i <= maxCorner.X; i++)
            {
                for (var j = minCorner.Y; j <= maxCorner.Y; j++)
                {
                    for (var k = minCorner.Z; k <= maxCorner.Z; k++)
                    {
                        var edges = 0;
                        if (i == minCorner.X || i == maxCorner.X) edges++;
                        if (j == minCorner.Y || j == maxCorner.Y) edges++;
                        if (k == minCorner.Z || k == maxCorner.Z) edges++;

                        if (edges >= 2) continue;

                        yield return new Int3(i, j, k);

                    }
                }
            }
        }

        public static IEnumerable<Int3> Rectangle(Int3 baseOrigo, int radius, int height, bool withBevel = false)
        {
            var min = new Int2(baseOrigo.X - radius, baseOrigo.Z - radius);
            var max = new Int2(baseOrigo.X + radius, baseOrigo.Z + radius);

            for (var step = 0; step < height; step++)
            {
                for (var x = min.X; x <= max.X; x++)
                {
                    for (var z = min.Y; z <= max.Y; z++)
                    {
                        if (withBevel && (x == min.X || x == max.X) && (z == min.Y || z == max.Y)) continue;

                        yield return new Int3(x, step + baseOrigo.Y, z);
                    }
                }
            }
        }


        public static IEnumerable<Int3> Sphere(Int3 origo, int radius, float threshold = .5f)
        {
            var origoVector = new Vector3(origo.X, origo.Y, origo.Z);

            var min = origo - new Int3(radius);
            var max = origo + new Int3(radius);

            for (var x = min.X; x <= max.X; x++)
            {
                for (var y = min.Y; y <= max.Y; y++)
                {
                    for (var z = min.Z; z <= max.Z; z++)
                    {
                        var pos = new Vector3(x, y, z);
                        Vector3.Distance(ref pos, ref origoVector, out var distance);

                        if (Math.Abs(distance - radius) > threshold) continue;
                        yield return new Int3(x, y, z);
                    }
                }
            }
        }

        public static IEnumerable<Int3> Pyramid(Int3 baseOrigo, int baseRadius, int numberOfSteps)
        {
            for (var step = 0; step < numberOfSteps; step++)
            {
                var radius = baseRadius - step;
                var min = new Int2(baseOrigo.X - radius, baseOrigo.Z - radius);
                var max = new Int2(baseOrigo.X + radius, baseOrigo.Z + radius);

                for (var x = min.X; x <= max.X; x++)
                {
                    for (var z = min.Y; z <= max.Y; z++)
                    {
                        yield return new Int3(x, step + baseOrigo.Y, z);
                    }
                }
            }
        }



    }
}
