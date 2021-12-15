using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppleCinnamon.Extensions;
using AppleCinnamon.Helper;
using AppleCinnamon.Vertices;
using SharpDX;
using SharpDX.Direct2D1;

namespace AppleCinnamon.Chunks
{
    public static class SkyDome
    {
        public const int Resolution = 64;
        public const float Radius = 100;

        public static readonly IReadOnlyDictionary<Face, Vector3> NormalMapping = new Dictionary<Face, Vector3>()
        {
            [Face.Top] = -Vector3.UnitY,
            [Face.Bottom] = Vector3.UnitY,

            [Face.Left] = Vector3.UnitX,
            [Face.Right] = -Vector3.UnitX,

            [Face.Front] = Vector3.UnitZ,
            [Face.Back] = -Vector3.UnitZ,

        };

        public static readonly Int2[] UvOffsetIndexes = { new(0, 0), new(1, 0), new(1, 1), new(0, 1) };

        public static IEnumerable<VertexSkyBox> GenerateSkyDome1()
        {
            var indicies = new int[] {3, 2, 0, 2, 1, 0};
            //var indicies = new int[] {0, 2, 3, 0, 1, 2};
            var scaler = new Vector3(20000);
            var offset = new Vector3(0, 0, 0);
            for (var i = 0; i < 6; i++)
            {
                var vertices = FaceBuildInfo.FaceVertices.Faces[i];
                var normal = NormalMapping[(Face) i];
                for (var j = 0; j < 6; j++)
                {
                    var index = indicies[j];
                    var vertex = vertices[index] * scaler + offset;
                    var uv = UvOffsetIndexes[index];
                    yield return new VertexSkyBox(vertex, normal, new Vector2(uv.X, uv.Y));
                }
            }
        }

        public static IEnumerable<VertexSkyBox> GenerateSkyDome()
        {
            var startVector = Vector3.UnitZ * Radius;

            var step = -MathUtil.Pi / (Resolution * 2);
            var horizontalSteps = Resolution * 4;
            var offset = new Vector3(1000);
            float scaler = 100;

            for (var i = 0; i < horizontalSteps; i++)
            {
                for (var j = 0; j < Resolution; j++)
                {
                    var v1 = startVector.Rotate(Vector3.UnitX, (j + 0) * step).Rotate(Vector3.UnitY, (i + 0) * step);
                    var v2 = startVector.Rotate(Vector3.UnitX, (j + 1) * step).Rotate(Vector3.UnitY, (i + 0) * step);
                    var v3 = startVector.Rotate(Vector3.UnitX, (j + 1) * step).Rotate(Vector3.UnitY, (i + 1) * step);
                    var v4 = startVector.Rotate(Vector3.UnitX, (j + 0) * step).Rotate(Vector3.UnitY, (i + 1) * step);

                    var middle = (v1 + v2 + v3 + v4) / 4f;
                    var direction = Vector3.Zero - middle;
                    direction.Normalize();


                    yield return new VertexSkyBox(v1 * scaler, direction, new Vector2(0, 0));
                    yield return new VertexSkyBox(v2 * scaler, direction, new Vector2(0, 1));
                    yield return new VertexSkyBox(v3 * scaler, direction, new Vector2(1, 1));
                    yield return new VertexSkyBox(v1 * scaler, direction, new Vector2(0, 1));
                    yield return new VertexSkyBox(v3 * scaler, direction, new Vector2(1, 1));
                    yield return new VertexSkyBox(v4 * scaler, direction, new Vector2(1, 0));


                    //yield return new VertexSkyBox(v3, direction, new Vector2(1, 1));
                }
            }
        }
    }
}
