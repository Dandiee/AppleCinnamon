using System.Linq;
using AppleCinnamon.System;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;

namespace AppleCinnamon
{
    public sealed class DebugLayout
    {
        public const string FontFamilyName = "Consolas";

        private readonly Graphics _graphics;
        private readonly TextFormat _leftAlignedTextFormat;
        private readonly TextFormat _rightAlignedTextFormat;

        public DebugLayout(Graphics graphics)
        {
            _graphics = graphics;
            _leftAlignedTextFormat = new TextFormat(_graphics.DirectWrite,
                FontFamilyName, FontWeight.Black, FontStyle.Normal, 16);

            _rightAlignedTextFormat = new TextFormat(_graphics.DirectWrite,
                FontFamilyName, FontWeight.Black, FontStyle.Normal, 16)
            {
                TextAlignment = TextAlignment.Trailing
            };
        }


        private string BuildText(IChunkManager chunkManager, Camera camera)
        {
            return $"Finalized chunks {chunkManager.FinalizedChunks}\r\n" +
                   $"Rendered chunks {chunkManager.RenderedChunks}\r\n" +
                   $"Current position {camera.Position.ToVector3().ToNonRetardedString()}\r\n"+
                   $"Current target {camera.CurrentCursor?.AbsoluteVoxelIndex.ToString() ?? "No target"}";
        }

        private string BuildPipelinePerformanceText(IChunkManager chunkManager) =>
            string.Join("\r\n", chunkManager.PipelinePerformance.Select(s => $"{s.Key}: {s.Value}ms"));

        public void Draw(
            IChunkManager chunkManager,
            Camera camera)
        {
            var leftText = BuildText(chunkManager, camera);
            var rightText = BuildPipelinePerformanceText(chunkManager);

            using (var leftTextLayout = new TextLayout(_graphics.DirectWrite, leftText, _leftAlignedTextFormat,
                _graphics.RenderForm.Width - 20, _graphics.RenderForm.Height))
            {
                _graphics.RenderTarget2D.DrawTextLayout(new RawVector2(10, 10), leftTextLayout,
                    new SolidColorBrush(_graphics.RenderTarget2D, Color.White));
            }

            using (var rightTextLayout = new TextLayout(_graphics.DirectWrite, rightText, _rightAlignedTextFormat,
                _graphics.RenderForm.Width - 20, _graphics.RenderForm.Height))
            {

                _graphics.RenderTarget2D.DrawTextLayout(new RawVector2(0, 10), rightTextLayout,
                    new SolidColorBrush(_graphics.RenderTarget2D, Color.White));
            }
        }
    }
}
