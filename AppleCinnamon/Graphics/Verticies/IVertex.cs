using SharpDX.Direct3D11;

namespace AppleCinnamon.Graphics.Verticies;

public interface IVertex
{
    int Size { get; }
    InputElement[] InputElements { get; }
}