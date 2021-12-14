using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace AppleCinnamon.Vertices
{
    public struct VertexBox : IVertex
    {
        private const int _size = 36;

        private static readonly InputElement[] _inputElements =
        {
            new("POSITION", 
                0, Format.R32G32B32_Float, 0, 0), //0
            new("POSITION", 
                1, Format.R32G32B32_Float, 12, 0), //0
            new("COLOR", 
                0, Format.R32G32B32_Float, 24, 0), //3+2
        };


        public int Size => _size;
        public InputElement[] InputElements => _inputElements;

        public VertexBox(Vector3 minimum, Vector3 maximum, Color3 color)
        {
            Minimum = minimum;
            Maximum = maximum;
            Color = color;
        }

        public VertexBox(ref BoundingBox bb, Color3 color)
        {
            Minimum = bb.Minimum;
            Maximum = bb.Maximum;
            Color = color;
        }

        public VertexBox(BoundingBox bb, Color3 color)
        {
            Minimum = bb.Minimum;
            Maximum = bb.Maximum;
            Color = color;
        }

        public Vector3 Minimum;
        public Vector3 Maximum;
        public Color3 Color;

    }
}