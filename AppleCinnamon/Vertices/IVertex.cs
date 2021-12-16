using SharpDX.Direct3D11;

namespace AppleCinnamon.Vertices
{
    public interface IVertex
    {
        int Size { get; }
        InputElement[] InputElements { get; }
    }
}