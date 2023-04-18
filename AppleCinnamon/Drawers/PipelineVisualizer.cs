using AppleCinnamon.Common;
using AppleCinnamon.Extensions;
using AppleCinnamon.Graphics;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;

namespace AppleCinnamon.Drawers
{
    public sealed class PipelineVisualizer
    {
        private readonly GraphicsContext _graphicsContext;
        private readonly Bitmap1 _textures;

        public PipelineVisualizer(GraphicsContext graphicsContext)
        {
            _graphicsContext = graphicsContext;
            _textures = _graphicsContext.D2DeviceContext.CreateD2DBitmap("Content/Texture/terrain3.png");
        }

        public void Draw(Camera camera, ChunkManager chunkManager)
        {
            const int size = sizeof(float) * 4;

            var destinationRects = new RawRectangleF[chunkManager.Chunks.Count];
            var sourceRects = new RawRectangle[chunkManager.Chunks.Count];
            var colors = new RawColor4[chunkManager.Chunks.Count];

            var i = 0;

            foreach (var chunk in chunkManager.Chunks.Values)
            {
                var result = chunk.Rect(camera.CurrentChunkIndex);
                destinationRects[i] = result.Dest;
                sourceRects[i] = result.Source;
                colors[i] = result.Color;

                i++;
            }

            _graphicsContext.D2DeviceContext.AntialiasMode = AntialiasMode.Aliased;
            _graphicsContext.D2DeviceContext.BeginDraw();
            _graphicsContext.SpriteBatch.Clear();
            _graphicsContext.SpriteBatch.AddSprites(chunkManager.Chunks.Count, destinationRects, sourceRects, colors, null, size, size, size, 0);
            _graphicsContext.D2DeviceContext.DrawSpriteBatch(_graphicsContext.SpriteBatch, 0, chunkManager.Chunks.Count, _textures, BitmapInterpolationMode.Linear, SpriteOptions.ClampToSourceRectangle);
            _graphicsContext.D2DeviceContext.EndDraw();
        }
    }

    public static class D2dExtensions
    {
        public static readonly Vector2 Offset = new(500, 500);
        public static readonly Vector2 Size = new(8, 8);
        public static readonly Vector2 HalfSize = Size / 2;

        public static readonly RawColor4 RenderedColor = new(0, 1, 0, 1);
        public static readonly RawRectangle SourceRect = new(0, 0, 16, 16);

        public static readonly RawColor4[] ColorsBySteps = new[]
        {
            Color.White.ToRawColor4(),      // Terrain
            Color.LightGray.ToRawColor4(),  // Artifact
            Color.Gray.ToRawColor4(),       // Local
            Color.Black.ToRawColor4(),      // Global
            Color.Blue.ToRawColor4(),       // Dispatcher
        };

        public static ChunkSprite Rect(this Chunk chunk, Int2 currentChunkIndex)
        {
            var center = chunk.Center2d / 2f + Offset;
            var color = RenderedColor;

            if (!chunk.IsRendered)
            {
                color = chunk.State == ChunkState.Finished 
                    ? Color.Purple
                    : ColorsBySteps[chunk.Stage];
            }

            if (chunk.Deletion == ChunkDeletionState.MarkedForDeletion)
            {
                color = Color.DarkRed.ToRawColor4();
            }

            if (chunk.ChunkIndex == currentChunkIndex)
            {
                color = new RawColor4(1, 0, 1, 1);
            }

            return new ChunkSprite(color,
                new RawRectangleF(center.X - HalfSize.X, center.Y - HalfSize.Y, center.X + HalfSize.X,
                    center.Y + HalfSize.Y), SourceRect);
        }
    }

    public readonly struct ChunkSprite
    {
        public readonly RawColor4 Color;
        public readonly RawRectangleF Dest;
        public readonly RawRectangle Source;

        public ChunkSprite(RawColor4 color, RawRectangleF dest, RawRectangle source)
        {
            Color = color;
            Dest = dest;
            Source = source;
        }
    }
}
