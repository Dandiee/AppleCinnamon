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
                FontFamilyName, FontWeight.Black, FontStyle.Normal, 20);

            _rightAlignedTextFormat = new TextFormat(_graphics.DirectWrite,
                FontFamilyName, FontWeight.Black, FontStyle.Normal, 20)
            {
                TextAlignment = TextAlignment.Trailing
            };
        }


        private string BuildLeftText(IChunkManager chunkManager, Camera camera)
        {
            var targetInfo = camera.CurrentCursor == null
                ? "No target"
                : $"{camera.CurrentCursor.AbsoluteVoxelIndex} (Block: {camera.CurrentCursor.Voxel.Block}, Light: {camera.CurrentCursor.Voxel.Lightness})";

            var targetTargetInfo = "No target target";

            if (camera.CurrentCursor != null)
            {
                var targetTarget =
                    chunkManager.GetVoxel(camera.CurrentCursor.AbsoluteVoxelIndex + camera.CurrentCursor.Direction);

                if (targetTarget != null)
                {
                    var address = Chunk.GetVoxelAddress(camera.CurrentCursor.AbsoluteVoxelIndex + camera.CurrentCursor.Direction);
                    targetTargetInfo = $"Block: {targetTarget.Value.Block}, Light: {targetTarget.Value.Lightness}, " +
                                       $"Chunk: {address.Value.ChunkIndex.X}, {address.Value.ChunkIndex.Y}, " +
                                       $"Voxel: {address.Value.RelativeVoxelIndex.X}, {address.Value.RelativeVoxelIndex.Y}, {address.Value.RelativeVoxelIndex.Z}";
                }
            }

            return $"Finalized chunks {chunkManager.FinalizedChunks:N0}\r\n" +
                   $"Rendered chunks {chunkManager.RenderedChunks:N0}\r\n" +
                   $"Queued chunks {chunkManager.QueuedChunks:N0}\r\n" +
                   $"Total visible faces {chunkManager.TotalVisibleFaces:N0}\r\n" + 
                   $"Total visible voxels {chunkManager.TotalVisibleVoxels:N0}\r\n" + 
                   $"Current position {camera.Position.ToVector3().ToNonRetardedString()}\r\n"+
                   $"Orientation {camera.LookAt.ToVector3().ToNonRetardedString()}\r\n"+
                   $"Current target {targetInfo}\r\n" +
                   $"Target target: {targetTargetInfo}\r\n" +
                   $"Back-face culling [F1]: {(Game.IsBackFaceCullingEnabled ? "On" : "Off")}\r\n" +
                   $"View frustum culling [F2]: {(Game.IsViewFrustumCullingEnabled ? "On" : "Off")}\r\n" +
                   $"Show chunk boxes [F3]: {(Game.ShowChunkBoundingBoxes ? "On" : "Off")}\r\n";
        }

        private string BuildRightText(IChunkManager chunkManager, Game game)
        {
            return string.Join("\r\n", chunkManager.PipelinePerformance.Select(s => $"{s.Key}: {s.Value:N0} ms")) + "\r\n" + 
                   $"Total pipeline time: {chunkManager.PipelinePerformance.Values.Sum():N0} ms\r\n" + 
                   $"Average render time: {game.AverageRenderTime:F2}\r\n" +
                   $"Peek render time: {game.PeekRenderTime:F2}\r\n" +
                   $"Average FPS: {game.AverageFps:F2}\r\n";
        }

        public void Draw(
            IChunkManager chunkManager,
            Camera camera,
            Game game)
        {
            var leftText = BuildLeftText(chunkManager, camera);
            var rightText = BuildRightText(chunkManager, game);

            using (var leftTextLayout = new TextLayout(_graphics.DirectWrite, leftText, _leftAlignedTextFormat,
                _graphics.RenderForm.Width - 20, _graphics.RenderForm.Height))
            {
                _graphics.RenderTarget2D.DrawTextLayout(new RawVector2(10, 10), leftTextLayout,
                    new SolidColorBrush(_graphics.RenderTarget2D, Color.White));
            }

            using (var rightTextLayout = new TextLayout(_graphics.DirectWrite, rightText, _rightAlignedTextFormat,
                _graphics.RenderForm.Width - 30, _graphics.RenderForm.Height))
            {
                _graphics.RenderTarget2D.DrawTextLayout(new RawVector2(0, 10), rightTextLayout,
                    new SolidColorBrush(_graphics.RenderTarget2D, Color.White));
            }
        }
    }
}
