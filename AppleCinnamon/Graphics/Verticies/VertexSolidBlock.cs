using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace AppleCinnamon.Graphics.Verticies;

[StructLayout(LayoutKind.Sequential)]
public struct VertexSolidBlock : IVertex
{
    private const int _sizeInBytes = sizeof(float) * 4;
    private static readonly InputElement[] _inputElements = 
    {
        new("POSITION", 0, Format.R32G32B32_Float, 0, 0),
        new("VISIBILITY", 0, Format.R32_UInt, 12, 0)
    };

    public int Size => _sizeInBytes;
    public InputElement[] InputElements => _inputElements;

    public VertexSolidBlock(Vector3 position, int u, int v, byte baseSunlight, byte baseCustomLight,
        byte totalNeighborLights, int numberOfAmbientNeighbors, byte hueIndex, byte totalNeighborCustomLight)
    {
        Position = position; // 32
        MetaData = 0;

        var l = baseSunlight + totalNeighborLights;
        var a = numberOfAmbientNeighbors;
        var h = 0b1111 & hueIndex;
        var c = baseCustomLight + totalNeighborCustomLight;

        MetaData |= (uint)(u <<  0); // 5 bits  textureCoordinateU
        MetaData |= (uint)(v <<  5); // 5 bits  textureCoordinateV
        MetaData |= (uint)(l << 10); // 6 bits  sunlight
        MetaData |= (uint)(a << 16); // 4 bits  ambientNeighborCount
        MetaData |= (uint)(h << 20); // 4 bits  hueIndex
        MetaData |= (uint)(c << 26); // 6 bits  emittedLight
    }

    public Vector3 Position;
    public uint MetaData;
}