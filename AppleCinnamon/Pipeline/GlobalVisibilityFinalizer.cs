using System.Diagnostics;
using System.Linq;
using AppleCinnamon.Settings;
using AppleCinnamon.System;

namespace AppleCinnamon.Pipeline
{
    public interface IGlobalVisibilityFinalizer
    {
        DataflowContext<Chunk> Process(DataflowContext<Chunk> context);
    }

    public sealed class GlobalVisibilityFinalizer : IGlobalVisibilityFinalizer
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
                var neighbourDefinition = VoxelDefinition.DefinitionByType[neighbour.Block];
                if (neighbourDefinition.IsTransparent)
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
                var neighbourDefinition = VoxelDefinition.DefinitionByType[neighbour.Block];
                if (neighbourDefinition.IsTransparent)
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
                var neighbourDefinition = VoxelDefinition.DefinitionByType[neighbour.Block];
                if (neighbourDefinition.IsTransparent)
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
                var neighbourDefinition = VoxelDefinition.DefinitionByType[neighbour.Block];
                if (neighbourDefinition.IsTransparent)
                {
                    chunk.VisibilityFlags.TryGetValue(index, out var visibility);
                    chunk.VisibilityFlags[index] = (byte)(visibility + 32);
                }

            }

            sw.Stop();

            CleanUpMemory(chunk);


            return new DataflowContext<Chunk>(context, chunk, sw.ElapsedMilliseconds, nameof(GlobalVisibilityFinalizer));
        }

        private void CleanUpMemory(Chunk chunk)
        {
            chunk.PendingLeftVoxels.Clear();
            chunk.PendingRightVoxels.Clear();
            chunk.PendingFrontVoxels.Clear();
            chunk.PendingBackVoxels.Clear();


            chunk.PendingLeftVoxels = null;
            chunk.PendingRightVoxels = null;
            chunk.PendingFrontVoxels = null;
            chunk.PendingBackVoxels = null;
        }
    }
}
