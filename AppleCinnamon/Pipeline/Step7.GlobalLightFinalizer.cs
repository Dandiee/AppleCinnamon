using System;
using System.Collections.Generic;
using System.Linq;
using AppleCinnamon.Helper;
using AppleCinnamon.Pipeline.Context;
using AppleCinnamon.Settings;
using SharpDX;

namespace AppleCinnamon.Pipeline
{
    public sealed class GlobalLightFinalizer : TransformChunkPipelineBlock<Chunk>
    {
        public override Chunk Process(Chunk chunk)
        {
            foreach (var corner in AnnoyingMappings.GlobalLightFinalizerCornerMapping)
            {
                var cornerChunk = chunk.Neighbors[corner.Key];
                EdgeSolver(cornerChunk, EdgePropogation.All[(byte)corner.Value[0]]);
                EdgeSolver(cornerChunk, EdgePropogation.All[(byte)corner.Value[1]]);
            }

            foreach (var direction in EdgePropogation.All.Skip(2))
            {
                EdgeSolver(chunk, direction);
            }

            return chunk;
        }

        private struct EdgePropogation
        {
            public static readonly EdgePropogation[] All =
            {
                new(), new(),
                new(Face.Left, Chunk.GetChunkFlatIndex(-1, 0), new Int3(0, 1, 1), new Int3(Chunk.SizeXy - 1,0,0), new Int3(0, 0, 0)),
                new(Face.Right, Chunk.GetChunkFlatIndex(1, 0), new Int3(0, 1, 1), new Int3(0, 0, 0), new Int3(Chunk.SizeXy - 1, 0, 0)),
                new(Face.Front, Chunk.GetChunkFlatIndex(0, -1), new Int3(1, 1, 0), new Int3(0, 0, Chunk.SizeXy - 1), new Int3(0, 0, 0)),
                new(Face.Back, Chunk.GetChunkFlatIndex(0, 1), new Int3(1, 1, 0), new Int3(0, 0, 0), new Int3(0, 0, Chunk.SizeXy - 1)),
            };

            public readonly Face TargetToSourceDirection;
            public readonly int RelativeSourceChunkIndex;
            public readonly Int3 DirectionMask;
            public readonly Int3 SourceOffset;
            public readonly Int3 TargetOffset;

            public EdgePropogation(Face targetToSourceDirection, int relativeSourceChunkIndex, Int3 directionMask, Int3 sourceOffset, Int3 targetOffset)
            {
                TargetToSourceDirection = targetToSourceDirection;
                RelativeSourceChunkIndex = relativeSourceChunkIndex;
                DirectionMask = directionMask;
                SourceOffset = sourceOffset;
                TargetOffset = targetOffset;
            }
        }

        private void EdgeSolver(Chunk targetChunk, EdgePropogation context)
        {
            var sourceChunk = targetChunk.Neighbors[context.RelativeSourceChunkIndex];

            var height = Math.Min(sourceChunk.CurrentHeight, targetChunk.CurrentHeight);

            var queue = new Queue<int>();

            for (var j = height - 1; j > 0; j--)
            {
                for (var h = 0; h < Chunk.SizeXy; h++)
                {
                    var indexMask = new Int3(h * context.DirectionMask.X, j, h * context.DirectionMask.Z);

                    var sourceIndex = indexMask + context.SourceOffset;
                    var sourceFlatIndex = sourceChunk.GetFlatIndex(sourceIndex);
                    var sourceVoxel = sourceChunk.Voxels[sourceFlatIndex];
                    var sourceDefinition = sourceVoxel.GetDefinition();

                    var targetIndex = indexMask + context.TargetOffset;
                    var targetFlatIndex = targetChunk.GetFlatIndex(targetIndex);
                    var targetVoxel = targetChunk.Voxels[targetFlatIndex];
                    var targetDefinition = targetVoxel.GetDefinition();

                    var brightnessLoss = VoxelDefinition.GetBrightnessLoss(sourceDefinition, targetDefinition, context.TargetToSourceDirection);
                    if (brightnessLoss != 0 && targetVoxel.CompositeLight < sourceVoxel.CompositeLight - brightnessLoss)
                    {
                        targetChunk.SetVoxel(targetFlatIndex, targetVoxel.SetSunlight((byte)(sourceVoxel.CompositeLight - brightnessLoss)));
                        queue.Enqueue(targetFlatIndex);
                    }
                }
            }

            LightingService.LocalPropagate(targetChunk, queue);
        }
    }
}
