﻿using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;

namespace AppleCinnamon
{
    public sealed class Crosshair
    {
        private readonly Graphics _graphics;
        private readonly Geometry _geometry;

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
        }


        public void Draw()
        {
            _graphics.RenderTarget2D.FillGeometry(_geometry, new SolidColorBrush(_graphics.RenderTarget2D, Color.White), null);
        }
    }
}
