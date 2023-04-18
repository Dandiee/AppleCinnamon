﻿using System;
using System.Collections.Generic;
using System.Linq;
using AppleCinnamon.Common;
using AppleCinnamon.Options;
using SharpDX;

namespace AppleCinnamon.ChunkBuilder;

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
            EdgeSolver(cornerChunk, EdgePropagation.All[(byte)corner.Value[0]]);
            EdgeSolver(cornerChunk, EdgePropagation.All[(byte)corner.Value[1]]);
        }

        foreach (var direction in EdgePropagation.All.Skip(2))
        {
            EdgeSolver(chunk, direction);
        }
    }

    private readonly struct EdgePropagation
    {
        public static readonly EdgePropagation[] All =
        {
            new(), new(),
            new(Face.Left, Chunk.GetChunkFlatIndex(-1, 0), new Int3(0, 1, 1), new Int3(GameOptions.CHUNK_SIZE - 1,0,0), new Int3(0, 0, 0)),
            new(Face.Right, Chunk.GetChunkFlatIndex(1, 0), new Int3(0, 1, 1), new Int3(0, 0, 0), new Int3(GameOptions.CHUNK_SIZE - 1, 0, 0)),
            new(Face.Front, Chunk.GetChunkFlatIndex(0, -1), new Int3(1, 1, 0), new Int3(0, 0, GameOptions.CHUNK_SIZE - 1), new Int3(0, 0, 0)),
            new(Face.Back, Chunk.GetChunkFlatIndex(0, 1), new Int3(1, 1, 0), new Int3(0, 0, 0), new Int3(0, 0, GameOptions.CHUNK_SIZE - 1)),
        };

        public readonly Face TargetToSourceDirection;
        public readonly int RelativeSourceChunkIndex;
        public readonly Int3 DirectionMask;
        public readonly Int3 SourceOffset;
        public readonly Int3 TargetOffset;

        private EdgePropagation(Face targetToSourceDirection, int relativeSourceChunkIndex, Int3 directionMask, Int3 sourceOffset, Int3 targetOffset)
        {
            TargetToSourceDirection = targetToSourceDirection;
            RelativeSourceChunkIndex = relativeSourceChunkIndex;
            DirectionMask = directionMask;
            SourceOffset = sourceOffset;
            TargetOffset = targetOffset;
        }
    }

    private static void EdgeSolver(Chunk targetChunk, EdgePropagation context)
    {
        var sourceChunk = targetChunk.Neighbors[context.RelativeSourceChunkIndex];

        var height = Math.Min(sourceChunk.CurrentHeight, targetChunk.CurrentHeight);

        var queue = new Queue<int>();

        for (var j = height - 1; j > 0; j--)
        {
            for (var h = 0; h < GameOptions.CHUNK_SIZE; h++)
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