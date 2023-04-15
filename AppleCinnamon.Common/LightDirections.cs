using AppleCinnamon.Common;
using SharpDX;

namespace AppleCinnamon
{
    public readonly struct LightDirections
    {
        public readonly Face Direction;
        public readonly Int3 Step;

        public LightDirections(Face direction, Int3 step)
        {
            Direction = direction;
            Step = step;
        }

        public static readonly LightDirections[] All =
        {
            new(Face.Top, Int3.UnitY),
            new(Face.Bottom, -Int3.UnitY),
            new(Face.Left, -Int3.UnitX),
            new(Face.Right, Int3.UnitX),
            new(Face.Front, -Int3.UnitZ),
            new(Face.Back, Int3.UnitZ)
        };
    }
}