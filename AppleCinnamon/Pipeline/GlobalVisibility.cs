using System.Diagnostics;
using AppleCinnamon.System;

namespace AppleCinnamon.Pipeline
{
    public sealed class GlobalVisibility
    {
        public DataflowContext<Chunk> Process(DataflowContext<Chunk> context)
        {
            var sw = Stopwatch.StartNew();
            var chunk = context.Payload;

            var leftChunk = chunk.Neighbours[new Int2(-1, 0)];
            var rightChunk = chunk.Neighbours[new Int2(1, 0)];
            var frontChunk = chunk.Neighbours[new Int2(0, -1)];
            var backChunk = chunk.Neighbours[new Int2(0, 1)];

            foreach (var index in chunk.PendingLeftVoxels)
            {
                var k = index / (Chunk.SizeXy * Chunk.Height);
                var j = (index - k * Chunk.SizeXy * Chunk.Height) / Chunk.SizeXy;

                var neighbour = leftChunk.Voxels[Chunk.SizeXy - 1 + Chunk.SizeXy * (j + Chunk.Height * k)];
                if (neighbour.Block == 0)
                {
                    chunk.VisibilityFlags.TryGetValue(index, out var visibility);
                    chunk.VisibilityFlags[index] = (byte)(visibility + 4);
                }
            }

            foreach (var index in chunk.PendingRightVoxels)
            {
                var k = index / (Chunk.SizeXy * Chunk.Height);
                var j = (index - k * Chunk.SizeXy * Chunk.Height) / Chunk.SizeXy;

                var neighbour = rightChunk.Voxels[Chunk.SizeXy * (j + Chunk.Height * k)];
                if (neighbour.Block == 0)
                {
                    chunk.VisibilityFlags.TryGetValue(index, out var visibility);
                    chunk.VisibilityFlags[index] = (byte)(visibility + 8);
                }
            }

            foreach (var index in chunk.PendingFrontVoxels)
            {
                var j = index / Chunk.SizeXy;
                var i = index - j * Chunk.SizeXy;

                var neighbour = frontChunk.Voxels[i + Chunk.SizeXy * (j + Chunk.Height * (Chunk.SizeXy - 1))];
                if (neighbour.Block == 0)
                {
                    chunk.VisibilityFlags.TryGetValue(index, out var visibility);
                    chunk.VisibilityFlags[index] = (byte)(visibility + 16);
                }
            }

            foreach (var index in chunk.PendingBackVoxels)
            {
                var k = Chunk.SizeXy - 1;
                var j = (index - k * Chunk.SizeXy * Chunk.Height) / Chunk.SizeXy;
                var i = index - (k * Chunk.SizeXy * Chunk.Height + j * Chunk.SizeXy);

                var neighbour = backChunk.Voxels[i + Chunk.SizeXy * j];
                if (neighbour.Block == 0)
                {
                    chunk.VisibilityFlags.TryGetValue(index, out var visibility);
                    chunk.VisibilityFlags[index] = (byte)(visibility + 32);
                }

            }

            sw.Stop();
            
            return new DataflowContext<Chunk>(context, chunk, sw.ElapsedMilliseconds, nameof(GlobalVisibility));
        }


    }
}
