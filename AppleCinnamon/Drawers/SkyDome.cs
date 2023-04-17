using AppleCinnamon.Extensions;
using AppleCinnamon.Vertices;
using SharpDX;
using Device = SharpDX.Direct3D11.Device;

namespace AppleCinnamon.Drawers
{
    public class SkyDome
    {
        private readonly Device _device;
        private readonly SkyDomeEffect _skyDomeEffectEffect;

        private BufferDefinition<VertexSkyBox> _skyBuffer;

        public SkyDome(Device device)
        {
            _device = device;
            _skyDomeEffectEffect = new SkyDomeEffect(device);
            UpdateSkyDome();
        }

        public void Draw()
        {
            _skyDomeEffectEffect.EffectDefinition.Use(_device);
            _skyBuffer.Draw(_device);
        }

        public void Update(Camera camera)
        {
            _skyDomeEffectEffect.Update(camera);
        }

        public void UpdateEffect()
        {
            _skyDomeEffectEffect.UpdateDetails();
        }

        public void UpdateSkyDome()
        {
            _skyBuffer?.Dispose();
            _skyBuffer = null;

            var undercut = SkyDomeOptions.Resolution / 4;

            var startVector = Vector3.UnitZ * SkyDomeOptions.Radius;
            var step = -MathUtil.Pi / (SkyDomeOptions.Resolution * 2);

            var facesCount = (SkyDomeOptions.Resolution + undercut) * SkyDomeOptions.Resolution * 4;
            var indexes = new uint[facesCount * 6];
            var vertices = new VertexSkyBox[facesCount * 4];

            var faceCounter = 0;

            for (var i = 0; i < SkyDomeOptions.Resolution * 4; i++)
            {
                for (var j = -undercut; j < SkyDomeOptions.Resolution; j++)
                {
                    var v1 = startVector.Rotate(Vector3.UnitX, (j + 0) * step).Rotate(Vector3.UnitY, (i + 0) * step);
                    var v2 = startVector.Rotate(Vector3.UnitX, (j + 1) * step).Rotate(Vector3.UnitY, (i + 0) * step);
                    var v3 = startVector.Rotate(Vector3.UnitX, (j + 1) * step).Rotate(Vector3.UnitY, (i + 1) * step);
                    var v4 = startVector.Rotate(Vector3.UnitX, (j + 0) * step).Rotate(Vector3.UnitY, (i + 1) * step);

                    var normal = Vector3.Normalize(Vector3.Zero - (v1 + v2 + v3 + v4) / 4f);

                    var currentFaceIndex = faceCounter;
                    var vertexIndexOffset = currentFaceIndex * 4;
                    var indexIndexOffset = currentFaceIndex * 6;

                    vertices[vertexIndexOffset + 0] = new VertexSkyBox(v1 * SkyDomeOptions.Radius, normal, new Vector2(0, 0));
                    vertices[vertexIndexOffset + 1] = new VertexSkyBox(v2 * SkyDomeOptions.Radius, normal, new Vector2(0, 1));
                    vertices[vertexIndexOffset + 2] = new VertexSkyBox(v3 * SkyDomeOptions.Radius, normal, new Vector2(1, 1));
                    vertices[vertexIndexOffset + 3] = new VertexSkyBox(v4 * SkyDomeOptions.Radius, normal, new Vector2(1, 0));

                    indexes[indexIndexOffset + 0] = (uint)(vertexIndexOffset + 0);
                    indexes[indexIndexOffset + 1] = (uint)(vertexIndexOffset + 1);
                    indexes[indexIndexOffset + 2] = (uint)(vertexIndexOffset + 2);
                    indexes[indexIndexOffset + 3] = (uint)(vertexIndexOffset + 0);
                    indexes[indexIndexOffset + 4] = (uint)(vertexIndexOffset + 2);
                    indexes[indexIndexOffset + 5] = (uint)(vertexIndexOffset + 3);

                    faceCounter++;
                }
            }

            _skyBuffer = new BufferDefinition<VertexSkyBox>(_device, ref vertices, ref indexes);
        }
    }
}
