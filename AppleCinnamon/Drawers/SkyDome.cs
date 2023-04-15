using AppleCinnamon.Chunks;
using AppleCinnamon.Extensions;
using AppleCinnamon.Vertices;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;

namespace AppleCinnamon.Drawers
{
    public class SkyDome
    {
        private readonly Device _device;
        public const int Resolution = 64;
        public const float Radius = 100;

        private readonly BufferDefinition<VertexSkyBox> _skyBuffer;
        private readonly EffectDefinition<VertexSkyBox> _skyEffectDefinition;

        public SkyDome(Device device)
        {
            _device = device;
            _skyEffectDefinition = new(_device, "Content/Effect/RayleightScatter.fx", PrimitiveTopology.TriangleList);
            _skyBuffer = GenerateSkyDome(device);
        }

        public void Draw()
        {

            _skyEffectDefinition.Use(_device);
            _skyBuffer.Draw(_device);

        }

        public void Update(Camera camera, World world)
        {
            Hofman.UpdateEffect(_skyEffectDefinition, camera, world);
        }

        public static BufferDefinition<VertexSkyBox> GenerateSkyDome(Device device)
        {
            var undercut = Resolution / 4;

            var startVector = Vector3.UnitZ * Radius;
            var step = -MathUtil.Pi / (Resolution * 2);

            var facesCount = (Resolution + undercut) * Resolution * 4;
            var indexes = new uint[facesCount * 6];
            var vertices = new VertexSkyBox[facesCount * 4];

            var faceCounter = 0;

            for (var i = 0; i < Resolution * 4; i++)
            {
                for (var j = -undercut; j < Resolution; j++)
                {
                    var v1 = startVector.Rotate(Vector3.UnitX, (j + 0) * step).Rotate(Vector3.UnitY, (i + 0) * step);
                    var v2 = startVector.Rotate(Vector3.UnitX, (j + 1) * step).Rotate(Vector3.UnitY, (i + 0) * step);
                    var v3 = startVector.Rotate(Vector3.UnitX, (j + 1) * step).Rotate(Vector3.UnitY, (i + 1) * step);
                    var v4 = startVector.Rotate(Vector3.UnitX, (j + 0) * step).Rotate(Vector3.UnitY, (i + 1) * step);

                    var normal = Vector3.Normalize(Vector3.Zero - (v1 + v2 + v3 + v4) / 4f);

                    var currentFaceIndex = faceCounter;
                    var vertexIndexOffset = currentFaceIndex * 4;
                    var indexIndexOffset = currentFaceIndex * 6;

                    vertices[vertexIndexOffset + 0] = new VertexSkyBox(v1 * Radius, normal, new Vector2(0, 0));
                    vertices[vertexIndexOffset + 1] = new VertexSkyBox(v2 * Radius, normal, new Vector2(0, 1));
                    vertices[vertexIndexOffset + 2] = new VertexSkyBox(v3 * Radius, normal, new Vector2(1, 1));
                    vertices[vertexIndexOffset + 3] = new VertexSkyBox(v4 * Radius, normal, new Vector2(1, 0));

                    indexes[indexIndexOffset + 0] = (uint)(vertexIndexOffset + 0);
                    indexes[indexIndexOffset + 1] = (uint)(vertexIndexOffset + 1);
                    indexes[indexIndexOffset + 2] = (uint)(vertexIndexOffset + 2);
                    indexes[indexIndexOffset + 3] = (uint)(vertexIndexOffset + 0);
                    indexes[indexIndexOffset + 4] = (uint)(vertexIndexOffset + 2);
                    indexes[indexIndexOffset + 5] = (uint)(vertexIndexOffset + 3);

                    faceCounter++;
                }
            }

            return new BufferDefinition<VertexSkyBox>(device, ref vertices, ref indexes);
        }
    }
}
