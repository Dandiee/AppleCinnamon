using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using AppleCinnamon.Helper;
using AppleCinnamon.Settings;
using SharpDX;
using Help = AppleCinnamon.Helper.Help;

namespace AppleCinnamon.Pipeline
{
    public sealed class LightFinalizer
    {
        private static readonly IReadOnlyDictionary<int, Face[]> CornerMapping =
            new Dictionary<int, Face[]>
            {
                [Help.GetChunkFlatIndex(-1, -1)] = new []{ Face.Right, Face.Back },
                [Help.GetChunkFlatIndex( 1, -1)] = new []{ Face.Left, Face.Back },
                [Help.GetChunkFlatIndex( 1,  1)] = new []{ Face.Left, Face.Front },
                [Help.GetChunkFlatIndex(-1,  1)] = new []{ Face.Right, Face.Front },
            };

        public DataflowContext<Chunk> Finalize(DataflowContext<Chunk> context)
        {
            var sw = Stopwatch.StartNew();
            var chunk = context.Payload;

            foreach (var corner in CornerMapping)
            {
                var cornerChunk = chunk.neighbors2[corner.Key];
                EdgeSolver(cornerChunk, EdgePropogation.All[(byte)corner.Value[0]]);
                EdgeSolver(cornerChunk, EdgePropogation.All[(byte)corner.Value[1]]);
            }

            foreach (var direction in EdgePropogation.All.Skip(2))
            {
                EdgeSolver(chunk, direction);
            }

            sw.Stop();
            return new DataflowContext<Chunk>(context, context.Payload, sw.ElapsedMilliseconds, nameof(LightFinalizer));
        }

        private struct EdgePropogation
        {
            public static readonly EdgePropogation[] All =
            {
                new(), new(),
                new(Face.Left, Help.GetChunkFlatIndex(-1, 0), new Int3(0, 1, 1), new Int3(31,0,0), new Int3(0, 0, 0)),
                new(Face.Right, Help.GetChunkFlatIndex(1, 0), new Int3(0, 1, 1), new Int3(0, 0, 0), new Int3(31, 0, 0)),
                new(Face.Front, Help.GetChunkFlatIndex(0, -1), new Int3(1, 1, 0), new Int3(0, 0, 31), new Int3(0, 0, 0)),
                new(Face.Back, Help.GetChunkFlatIndex(0, 1), new Int3(1, 1, 0), new Int3(0, 0, 0), new Int3(0, 0, 31)),
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
            var sourceChunk = targetChunk.neighbors2[context.RelativeSourceChunkIndex];

            var height = Math.Min(sourceChunk.CurrentHeight, targetChunk.CurrentHeight);

            var queue = new Queue<int>();

            for (var j = height - 1; j > 0; j--)
            {
                for (var h = 0; h < Chunk.SizeXy; h++)
                {
                    var indexMask = new Int3(h * context.DirectionMask.X, j, h * context.DirectionMask.Z);

                    var sourceIndex = indexMask + context.SourceOffset;
                    var sourceFlatIndex = sourceIndex.ToFlatIndex(sourceChunk.CurrentHeight);
                    var sourceVoxel = sourceChunk.Voxels[sourceFlatIndex];
                    var sourceDefinition = VoxelDefinition.DefinitionByType[sourceVoxel.Block];

                    var targetIndex = indexMask + context.TargetOffset;
                    var targetFlatIndex = targetIndex.ToFlatIndex(targetChunk.CurrentHeight);
                    var targetVoxel = targetChunk.Voxels[targetFlatIndex];
                    var targetDefinition = VoxelDefinition.DefinitionByType[targetVoxel.Block];

                    var brightnessLoss = VoxelDefinition.GetBrightnessLoss(sourceDefinition, targetDefinition, context.TargetToSourceDirection);
                    if (brightnessLoss != 0 && targetVoxel.Lightness < sourceVoxel.Lightness - brightnessLoss)
                    {
                        targetChunk.SetVoxelNoInline(targetFlatIndex, targetVoxel.SetLight((byte)(sourceVoxel.Lightness - brightnessLoss)));
                        queue.Enqueue(targetFlatIndex);
                    }
                }
            }

            LocalLightPropagationService.InitializeLocalLight(targetChunk, queue);
        }
    }
}
