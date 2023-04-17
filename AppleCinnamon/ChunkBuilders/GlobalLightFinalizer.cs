﻿using System;
using System.Collections.Generic;
using System.Linq;
using AppleCinnamon.Chunks;
using AppleCinnamon.Common;
using AppleCinnamon.Settings;
using SharpDX;

namespace AppleCinnamon.ChunkBuilders
{
    public static class GlobalLightFinalizer
    {
        private static readonly IReadOnlyDictionary<int, Face[]> GlobalLightFinalizerCornerMapping =
            new Dictionary<int, Face[]>
            {
                [Chunk.GetChunkFlatIndex(-1, -1)] = new[] { Face.Right, Face.Back },
                [Chunk.GetChunkFlatIndex(1, -1)] = new[] { Face.Left, Face.Back },
                [Chunk.GetChunkFlatIndex(1, 1)] = new[] { Face.Left, Face.Front },
                [Chunk.GetChunkFlatIndex(-1, 1)] = new[] { Face.Right, Face.Front },
            };

        public static void FinalizeGlobalLighting(Chunk chunk)
        {
            foreach (var corner in GlobalLightFinalizerCornerMapping)
            {
                var cornerChunk = chunk.Neighbors[corner.Key];
                EdgeSolver(cornerChunk, EdgePropogation.All[(byte)corner.Value[0]]);
                EdgeSolver(cornerChunk, EdgePropogation.All[(byte)corner.Value[1]]);
            }

            foreach (var direction in EdgePropogation.All.Skip(2))
            {
                EdgeSolver(chunk, direction);
            }
        }

        private struct EdgePropogation
        {
            public static readonly EdgePropogation[] All =
            {
                new(), new(),
                new(Face.Left, Chunk.GetChunkFlatIndex(-1, 0), new Int3(0, 1, 1), new Int3(WorldSettings.ChunkSize - 1,0,0), new Int3(0, 0, 0)),
                new(Face.Right, Chunk.GetChunkFlatIndex(1, 0), new Int3(0, 1, 1), new Int3(0, 0, 0), new Int3(WorldSettings.ChunkSize - 1, 0, 0)),
                new(Face.Front, Chunk.GetChunkFlatIndex(0, -1), new Int3(1, 1, 0), new Int3(0, 0, WorldSettings.ChunkSize - 1), new Int3(0, 0, 0)),
                new(Face.Back, Chunk.GetChunkFlatIndex(0, 1), new Int3(1, 1, 0), new Int3(0, 0, 0), new Int3(0, 0, WorldSettings.ChunkSize - 1)),
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

        private static void EdgeSolver(Chunk targetChunk, EdgePropogation context)
        {
            var sourceChunk = targetChunk.Neighbors[context.RelativeSourceChunkIndex];

            var height = Math.Min(sourceChunk.CurrentHeight, targetChunk.CurrentHeight);

            var queue = new Queue<int>();

            for (var j = height - 1; j > 0; j--)
            {
                for (var h = 0; h < WorldSettings.ChunkSize; h++)
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
