﻿using AppleCinnamon.Common;
using AppleCinnamon.Extensions;
using AppleCinnamon.Helper;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;

namespace AppleCinnamon.Drawers
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

            var destinationRects = new RawRectangleF[ChunkManager.Chunks.Count];
            var sourceRects = new RawRectangle[ChunkManager.Chunks.Count];
            var colors = new RawColor4[ChunkManager.Chunks.Count];

            var i = 0;

            foreach (var chunk in ChunkManager.Chunks.Values)
            {
                var result = chunk.Rect(camera.CurrentChunkIndex);
                destinationRects[i] = result.Dest;
                sourceRects[i] = result.Source;
                colors[i] = result.Color;

                i++;
            }

            _graphics.D2DeviceContext.AntialiasMode = AntialiasMode.Aliased;
            _graphics.D2DeviceContext.BeginDraw();
            _graphics.SpriteBatch.Clear();
            _graphics.SpriteBatch.AddSprites(ChunkManager.Chunks.Count, destinationRects, sourceRects, colors, null, size, size, size, 0);
            _graphics.D2DeviceContext.DrawSpriteBatch(_graphics.SpriteBatch, 0, ChunkManager.Chunks.Count, _textures, BitmapInterpolationMode.Linear, SpriteOptions.ClampToSourceRectangle);
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

        public static readonly RawColor4[] ColorsBySteps = new[]
        {
            Color.White.ToRawColor4(), // terraing gen

            Color.LightGray.ToRawColor4(), // neighbor assigner
            Color.Yellow.ToRawColor4(), // artifact gen
            
            Color.Gray.ToRawColor4(), // pool
            Color.Red.ToRawColor4(), // localizer
            
            Color.DarkGray.ToRawColor4(), // pool
            Color.Green.ToRawColor4(), // globalizer
            
            Color.Black.ToRawColor4(), // pool

            Color.Blue.ToRawColor4(), // dispatcher
            Color.Purple.ToRawColor4(), // finalized

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
            var color = RenderedColor;

            if (!chunk.IsRendered)
            {
                color = ColorsBySteps[chunk.Stage];
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