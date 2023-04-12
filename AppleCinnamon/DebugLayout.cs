using System;
using System.Linq;
using System.Threading.Tasks.Dataflow;
using AppleCinnamon.Chunks;
using AppleCinnamon.Helper;
using AppleCinnamon.Settings;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectInput;
using SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;
using TextAlignment = SharpDX.DirectWrite.TextAlignment;

namespace AppleCinnamon
{
    public sealed class DebugLayout
    {
        public const string FontFamilyName = "Consolas";

        private readonly Graphics _graphics;
        private readonly TextFormat _leftAlignedTextFormat;
        private readonly TextFormat _rightAlignedTextFormat;
        private readonly TextFormat _bottomCenterAlignedTextFormat;
        private readonly Keyboard _keyboard;



        private readonly SolidColorBrush _brush;


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

            _bottomCenterAlignedTextFormat = new TextFormat(_graphics.DirectWrite,
                FontFamilyName, FontWeight.Black, FontStyle.Normal, 20)
            {
                TextAlignment = TextAlignment.Center
            };

            _keyboard = new Keyboard(new DirectInput());
            _keyboard.Properties.BufferSize = 128;
            _keyboard.Acquire();

            _brush = new SolidColorBrush(_graphics.RenderTarget2D, Color.White);
        }


        private string BuildLeftText(ChunkManager chunkManager, Camera camera, Game game)
        {
            var targetInfo = camera.CurrentCursor == null
                ? "No target"
                : $"{camera.CurrentCursor.AbsoluteVoxelIndex} (BlockType: {camera.CurrentCursor.Voxel.BlockType}, Light: {camera.CurrentCursor.Voxel.CompositeLight})";

            var targetTargetInfo = "No target target";

            if (camera.CurrentCursor != null)
            {
                if (chunkManager.TryGetVoxel(camera.CurrentCursor.AbsoluteVoxelIndex + camera.CurrentCursor.Direction, out var targetTarget))
                {
                    if (chunkManager.TryGetVoxelAddress(
                        camera.CurrentCursor.AbsoluteVoxelIndex + camera.CurrentCursor.Direction, out var address))
                    {
                        targetTargetInfo =
                            $"BlockType: {targetTarget.BlockType}, Sun: {targetTarget.Sunlight}, Light: {targetTarget.EmittedLight}" +
                            $"Chunk: {address.Chunk.ChunkIndex.X}, {address.Chunk.ChunkIndex.Y}, " +
                            $"Voxel: {address.RelativeVoxelIndex.X}, {address.RelativeVoxelIndex.Y}, {address.RelativeVoxelIndex.Z}";
                    }
                    else throw new Exception("that should not happen i guess");
                }
            }

            return //$"Finalized chunks {chunkManager.FinalizedChunks:N0}\r\n" +
                   //$"Rendered chunks {chunkManager.RenderedChunks:N0}\r\n" +
                   //$"Queued chunks {chunkManager.QueuedChunks:N0}\r\n" +
                   //$"Total visible faces {chunkManager.TotalVisibleFaces:N0}\r\n" +
                   //$"Total visible voxels {chunkManager.TotalVisibleVoxels:N0}\r\n" +
                   $"Time: {game.World.Time:N2}\r\n" +
                   $"Current position {camera.Position.ToNonRetardedString()}\r\n" +
                   $"Orientation {camera.LookAt.ToNonRetardedString()}\r\n" +
                   $"Current target {targetInfo}\r\n" +
                   $"Target target: {targetTargetInfo}\r\n" +
                   $"Back-face culling [F1]: {(Game.IsBackFaceCullingEnabled ? "On" : "Off")}\r\n" +
                   $"View frustum culling [F2]: {(Game.IsViewFrustumCullingEnabled ? "On" : "Off")}\r\n" +
                   $"Show chunk boxes [F3]: {(Game.ShowChunkBoundingBoxes ? "On" : "Off")}\r\n";
        }

        private string GetPipelineMetrics(ChunkManager chunkManager)
        {
            var pipe = chunkManager.Pipeline;

            var stages = string.Join("", pipe.Stages.Select(stage => PipelineStageSummary(stage.Name, stage.Transform, stage.TimeSpentInTransform, stage)));
            return $"Pipeline ({pipe.State}) \r\n" +
                   stages +
                   PipelineStageSummary("Dispatcher", pipe.Dispatcher, pipe.TimeSpentInTransform);
        }

        private string PipelineStageSummary(string name, TransformBlock<Chunk, Chunk> transform, TimeSpan elapsedTime, PipelineStage stage = null)
            =>
                $"{name} {elapsedTime.TotalMilliseconds:N0}ms\r\n";

        private string BuildRightText(ChunkManager chunkManager, Game game)
        {
            return
                $"Chunk size {WorldSettings.ChunkSize}, View distance: {Game.ViewDistance}, Slice: {Chunk.SliceHeight}\r\n" +
                //string.Join("\r\n", chunkManager.PipelinePerformance.Select(s => $"{s.Key}: {s.Value:N0} ms")) + "\r\n" + 
                GetPipelineMetrics(chunkManager) + "\r\n" +
                //$"Total pipeline time: {chunkManager.PipelinePerformance.Values.Sum():N0} ms\r\n" + 
                //$"Boot time: {chunkManager.BootTime.TotalMilliseconds:N0} ms\r\n" + 
                $"Average render time: {game.AverageRenderTime:F2}\r\n" +
                $"Peek render time: {game.PeekRenderTime:F2}\r\n" +
                $"Average FPS: {game.AverageFps:F2}\r\n" +
                $"SUN: {Hofman.SunDirection:F2}\r\n" +
                $"INTENSITY: {Hofman.SunlightFactor:F2}\r\n" +
                $"In Proc Chunks: {ChunkManager.InProcessChunks}\r\n" +
                $"Death queue: {ChunkManager.BagOfDeath.Count}\r\n" +
                $"Chunks created: {ChunkManager.CreatedChunkInstances}\r\n" +
                $"Chunks resurrected: {ChunkManager.ChunksResurrected}\r\n" +
                $"Chunks: {ChunkManager.Chunks.Count}\r\n" +
                //$"Suspended: {!ChunkManager.SuspendPipeline.WaitOne(0)}\r\n" +
                $"Cleanups: {ChunkManager.Cleanups}\r\n" +
                $"Changes: {ChunkManager.NumberOfChanges}\r\n";
            //$"AnotherChunkCounter: {ChunkManager.AnotherChunkCounter}\r\n" +
            //$"Sinked: {ChunkManager.Sinked}\r\n";
            //$"{SharpDX.Diagnostics.ObjectTracker.ReportActiveObjects()}\r\n";
        }

        public void Draw(
            ChunkManager chunkManager,
            Camera camera,
            Game game)
        {
            var leftText = BuildLeftText(chunkManager, camera, game);
            var rightText = BuildRightText(chunkManager, game);

            //if (_keyboard.GetCurrentState().IsPressed(Key.C) && _keyboard.GetCurrentState().IsPressed(Key.LeftControl))
            //{
            //    Clipboard.SetText(rightText);
            //}

            using (var leftTextLayout = new TextLayout(_graphics.DirectWrite, leftText, _leftAlignedTextFormat ,_graphics.RenderForm.Width - 20, _graphics.RenderForm.Height))
            {
                _graphics.RenderTarget2D.DrawTextLayout(new RawVector2(10, 10), leftTextLayout, _brush);
            }

            using (var rightTextLayout = new TextLayout(_graphics.DirectWrite, rightText, _rightAlignedTextFormat, _graphics.RenderForm.Width - 30, _graphics.RenderForm.Height))
            {
                _graphics.RenderTarget2D.DrawTextLayout(new RawVector2(0, 10), rightTextLayout, _brush);
            }

            using (var bottomCenterTextLayout = new TextLayout(_graphics.DirectWrite, $"{camera.VoxelInHand.Name}: [{camera.VoxelInHand.Type}]", _bottomCenterAlignedTextFormat, _graphics.RenderForm.Width - 30, _graphics.RenderForm.Height))
            {
                _graphics.RenderTarget2D.DrawTextLayout(new RawVector2(0, _graphics.RenderForm.Height - 100), bottomCenterTextLayout, _brush);
            }
        }
    }
}
