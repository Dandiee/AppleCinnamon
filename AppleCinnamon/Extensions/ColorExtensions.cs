using SharpDX.Mathematics.Interop;

namespace AppleCinnamon.Extensions
{
    public static class ColorExtensions
    {

        public static RawColor4 ToRawColor4(this SharpDX.Color color)
        {
            var c = color.ToColor3();

            return new RawColor4(c.Red, c.Green, c.Blue, 1);
        }
    }
}