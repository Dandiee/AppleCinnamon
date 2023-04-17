using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;

namespace AppleCinnamon.Drawers
{
    public sealed class Crosshair
    {
        private readonly Graphics _graphics;
        private readonly Geometry _geometry;
        private readonly SolidColorBrush _brush;

        public Crosshair(Graphics graphics)
        {
            _graphics = graphics;
            var midX = graphics.RenderForm.ClientSize.Width / 2f;
            var midY = graphics.RenderForm.ClientSize.Height / 2f;
            var thickness = 3f;

            _geometry = new GeometryGroup(graphics.D2dFactory, FillMode.Alternate,
                new Geometry[]
                {
                    new RectangleGeometry(graphics.D2dFactory, new RawRectangleF(midX - 20, midY - thickness/2, midX + 20, midY + thickness/2)),
                    new RectangleGeometry(graphics.D2dFactory, new RawRectangleF(midX - thickness/2, midY - 20, midX + thickness/2, midY + 20))
                });
            _brush = new SolidColorBrush(_graphics.RenderTarget2D, Color.White);
        }


        public void Draw() => _graphics.RenderTarget2D.FillGeometry(_geometry, _brush, null);
    }
}
