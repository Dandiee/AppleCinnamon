using AppleCinnamon.Graphics;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;

namespace AppleCinnamon.Drawers
{
    public sealed class Crosshair
    {
        private readonly GraphicsContext _graphicsContext;
        private readonly Geometry _geometry;
        private readonly SolidColorBrush _brush;

        public Crosshair(GraphicsContext graphicsContext)
        {
            _graphicsContext = graphicsContext;
            var midX = graphicsContext.RenderForm.ClientSize.Width / 2f;
            var midY = graphicsContext.RenderForm.ClientSize.Height / 2f;
            var thickness = 3f;

            _geometry = new GeometryGroup(graphicsContext.D2dFactory, FillMode.Alternate,
                new Geometry[]
                {
                    new RectangleGeometry(graphicsContext.D2dFactory, new RawRectangleF(midX - 20, midY - thickness/2, midX + 20, midY + thickness/2)),
                    new RectangleGeometry(graphicsContext.D2dFactory, new RawRectangleF(midX - thickness/2, midY - 20, midX + thickness/2, midY + 20))
                });
            _brush = new SolidColorBrush(_graphicsContext.RenderTarget2D, Color.White);
        }


        public void Draw() => _graphicsContext.RenderTarget2D.FillGeometry(_geometry, _brush, null);
    }
}
