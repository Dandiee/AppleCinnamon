using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace AppleCinnamon.Graphics.Verticies;

//[StructLayout(LayoutKind.Sequential)]
public struct VertexCloud : IVertex
{
    private const int _sizeInBytes = 24;

    private static readonly InputElement[] _inputElements =
    {
        new("POSITION", 0, Format.R32G32B32_Float, 0, 0),
        new("NORMAL", 0, Format.R32G32B32_Float, 12, 0),
    };

    public int Size => _sizeInBytes;
    public InputElement[] InputElements => _inputElements;

    public VertexCloud(Vector3 position, Vector3 normal)
    {
        Position = position;
        Normal = normal;
    }

    public Vector3 Position;
    public Vector3 Normal;

}