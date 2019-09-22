using System.Diagnostics;
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


            foreach (var flatIndex in chunk.PendingLeftVoxels)
            {
                var index = flatIndex.ToIndex();

                var neighbour = leftChunk.Voxels[E.GetFlatIndex(Chunk.SizeXy - 1, index.Y, index.Z)];
                var neighbourDefinition = VoxelDefinition.DefinitionByType[neighbour.Block];
                if (neighbourDefinition.IsTransparent)
                {
                    chunk.VisibilityFlags.TryGetValue(flatIndex, out var visibility);
                    chunk.VisibilityFlags[flatIndex] = (byte)(visibility + 4);
                    chunk.VoxelCount.Left++;
                }
            }

            foreach (var flatIndex in chunk.PendingRightVoxels)
            {
                var index = flatIndex.ToIndex();

                var neighbour = rightChunk.Voxels[E.GetFlatIndex(0, index.Y, index.Z)];
                var neighbourDefinition = VoxelDefinition.DefinitionByType[neighbour.Block];
                if (neighbourDefinition.IsTransparent)
                {
                    chunk.VisibilityFlags.TryGetValue(flatIndex, out var visibility);
                    chunk.VisibilityFlags[flatIndex] = (byte)(visibility + 8);
                    chunk.VoxelCount.Right++;
                }
            }

            foreach (var flatIndex in chunk.PendingFrontVoxels)
            {
                var index = flatIndex.ToIndex();

                var neighbour = frontChunk.Voxels[E.GetFlatIndex(index.X, index.Y, Chunk.SizeXy - 1)];
                var neighbourDefinition = VoxelDefinition.DefinitionByType[neighbour.Block];
                if (neighbourDefinition.IsTransparent)
                {
                    chunk.VisibilityFlags.TryGetValue(flatIndex, out var visibility);
                    chunk.VisibilityFlags[flatIndex] = (byte)(visibility + 16);
                    chunk.VoxelCount.Front++;
                }
            }

            foreach (var flatIndex in chunk.PendingBackVoxels)
            {
                var index = flatIndex.ToIndex();

                var neighbour = backChunk.Voxels[E.GetFlatIndex(index.X, index.Y, 0)];
                var neighbourDefinition = VoxelDefinition.DefinitionByType[neighbour.Block];
                if (neighbourDefinition.IsTransparent)
                {
                    chunk.VisibilityFlags.TryGetValue(flatIndex, out var visibility);
                    chunk.VisibilityFlags[flatIndex] = (byte)(visibility + 32);
                    chunk.VoxelCount.Back++;
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
