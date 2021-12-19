using System.Linq;
using AppleCinnamon.Extensions;
using AppleCinnamon.Helper;
using AppleCinnamon.Pipeline;
using AppleCinnamon.Pipeline.Context;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;

namespace AppleCinnamon
{
    public sealed class PipelineVisualizer
    {
        private readonly Graphics _graphics;
        private readonly Bitmap1 _textures;

        public PipelineVisualizer(Graphics graphics)
        {
            _graphics = graphics;
            _textures = _graphics.D2DeviceContext.CreateD2DBitmap("Content/Texture/terrain3.png");
        }
        
        public void Draw(Camera camera, ChunkManager chunkManager)
        {
            const int size = sizeof(float) * 4;

            var chunks = NeighborAssigner.Chunks.Values.ToList();
            var destinationRects = new RawRectangleF[chunks.Count];
            var sourceRects = new RawRectangle[chunks.Count];
            var colors = new RawColor4[chunks.Count];

            for (var i = 0; i < chunks.Count; i++)
            {
                var result = chunks[i].Rect(camera.CurrentChunkIndex);
                destinationRects[i] = result.Dest;
                sourceRects[i] = result.Source;
                colors[i] = result.Color;
            }
            _graphics.D2DeviceContext.AntialiasMode = AntialiasMode.Aliased;
            _graphics.D2DeviceContext.BeginDraw();
            _graphics.SpriteBatch.Clear();
            _graphics.SpriteBatch.AddSprites(chunks.Count, destinationRects, sourceRects, colors, null, size, size, size, 0);
            _graphics.D2DeviceContext.DrawSpriteBatch(_graphics.SpriteBatch, 0, chunks.Count, _textures, BitmapInterpolationMode.Linear, SpriteOptions.ClampToSourceRectangle);
            _graphics.D2DeviceContext.EndDraw();
        }
    }

    public static class D2dExtensions
    {
        public static readonly Vector2 Offset = new(500, 500);
        public static readonly Vector2 Size = new(8, 8);
        public static readonly Vector2 HalfSize = Size / 2;

        public static readonly RawColor4 RenderedColor = new(0, 1, 0, 1);
        public static readonly RawRectangle SourceRect = new(0, 0, 16, 16);

        //public static readonly RawColor4[] ColorsByStep = PipelineBlock.Blocks.Select(s =>
        //{
        //    if (s is ChunkPoolPipelineBlock)
        //    {
        //        return s.DebugColor;
        //    }
        //    else
        //    {
        //        return new RawColor4((float) s.PipelineStepIndex / (PipelineBlock.Blocks.Count), 0, 0, 1);
        //    }
        //}).ToArray();

        public static readonly RawColor4[] ColorsBySteps = new[]
        {
            Color.Orange.ToRawColor4(), // terraing gen
            Color.Blue.ToRawColor4(), // neighbor assigner
            Color.GreenYellow.ToRawColor4(), // artifact gen
            Color.Red.ToRawColor4(), // pool
            Color.Green.ToRawColor4(), // localizer
            Color.Black.ToRawColor4(), // pool
            Color.Pink.ToRawColor4(), // globalizer
            Color.Gray.ToRawColor4(), // pool
            Color.LightBlue.ToRawColor4(), // dispatcher
            Color.Bisque.ToRawColor4(), // finalized
            //Color.Black.ToRawColor4(),
            //Color.Wheat.ToRawColor4(),
            //Color.Wheat.ToRawColor4(),
            //Color.Wheat.ToRawColor4(),
            //Color.Wheat.ToRawColor4(),
            //Color.Wheat.ToRawColor4(),
            //Color.Wheat.ToRawColor4(),
            //Color.Wheat.ToRawColor4(),
            //Color.Wheat.ToRawColor4(),
        };

        public static ChunkSprite Rect(this Chunk chunk, Int2 currentChunkIndex)
        {
            var center = chunk.Center2d / 2f + Offset;
            var color =  RenderedColor;
            if (!chunk.IsRendered)
            {
                color = ColorsBySteps[chunk.PipelineStep];
            }

            if (chunk.IsMarkedForDelete)
            {
                color = new RawColor4(1, 1, 1, 1);
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
