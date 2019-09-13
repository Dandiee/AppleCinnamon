using AppleCinnamon.System;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;

namespace AppleCinnamon
{
    public sealed class DebugLayout
    {
        private readonly Graphics _graphics;
        private readonly TextFormat _textFormat;

        public DebugLayout(Graphics graphics)
        {
            _graphics = graphics;
            _textFormat = new TextFormat(_graphics.DirectWrite,
                "Consolas", FontWeight.Black, FontStyle.Normal,
                16);
        }


        private string BuildText(IChunkManager chunkManager, Camera camera)
        {
            return $"Finalized chunks {chunkManager.FinalizedChunks}\r\n" +
                   $"Rendered chunks {chunkManager.RenderedChunks}\r\n" +
                   $"Current position {camera.Position.ToVector3().ToNonRetardedString()}\r\n"+
                   $"Current target {camera.CurrentCursor?.AbsoluteVoxelIndex.ToString() ?? "No target"}";
        }

        public void Draw(
            IChunkManager chunkManager,
            Camera camera)
        {
            var text = BuildText(chunkManager, camera);

            var textLayout = new TextLayout(_graphics.DirectWrite, text, _textFormat, 600, 500);

            _graphics.RenderTarget2D.DrawTextLayout(new RawVector2(10, 10), textLayout,
                new SolidColorBrush(_graphics.RenderTarget2D, Color.White));
        }
    }
}
