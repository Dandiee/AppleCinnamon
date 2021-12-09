using System;
using System.Collections.Generic;
using System.Diagnostics;
using AppleCinnamon.Settings;
using AppleCinnamon.System;

namespace AppleCinnamon.Pipeline
{
    public sealed class LightFinalizer
    {
        private static readonly Int2[] Corners = { new(-1, -1), new(-1, +1), new(+1, -1), new(+1, +1) };
        private static readonly Int2[] Edges = {Int2.UniX,-Int2.UniX, Int2.UniY, -Int2.UniY };

        private static readonly IDictionary<Int2, Int2[]> EdgeMapping = new Dictionary<Int2, Int2[]>
        {
            [Int2.UniX] = new[] { new Int2(Chunk.SizeXy - 1, 0), new Int2(0, 0) },
            [-Int2.UniX] = new[] { new Int2(0, 0), new Int2(Chunk.SizeXy - 1, 0) },
            [Int2.UniY] = new[] { new Int2(0, Chunk.SizeXy - 1), new Int2(0, 0) },
            [-Int2.UniY] = new[] { new Int2(0, 0), new Int2(0, Chunk.SizeXy - 1) }
        };

        private static readonly IDictionary<Int2, Face> DirFaceMapping = new Dictionary<Int2, Face>
        {
            [Int2.UniX] = Face.Left,
            [-Int2.UniX] = Face.Right,
            [Int2.UniY] = Face.Front,
            [-Int2.UniY] = Face.Back
        };

        public DataflowContext<Chunk> Finalize(DataflowContext<Chunk> context)
        {
            var sw = Stopwatch.StartNew();
            var chunk = context.Payload;

            foreach (var corner in Corners)
            {
                var cornerChunk = chunk.neighbors2[Help.GetChunkFlatIndex(corner)];
                ProcessEdge(cornerChunk, chunk.neighbors2[Help.GetChunkFlatIndex(corner.X, 0)]);
                ProcessEdge(cornerChunk, chunk.neighbors2[Help.GetChunkFlatIndex(0, corner.Y)]);
            }

            foreach (var edge in Edges)
            {
                var edgeChunk = chunk.neighbors2[Help.GetChunkFlatIndex(edge)];
                var offset = new Int2(Math.Sign(edge.Y), Math.Sign(edge.X));
                
                ProcessEdge(chunk.neighbors2[Help.GetChunkFlatIndex(edge + offset)], edgeChunk);
                ProcessEdge(chunk.neighbors2[Help.GetChunkFlatIndex(edge - offset)], edgeChunk);
                ProcessEdge(edgeChunk, chunk);
            }

            sw.Stop();
            return new DataflowContext<Chunk>(context, context.Payload, sw.ElapsedMilliseconds, nameof(LightFinalizer));
        }

        private void ProcessEdge(Chunk sourceChunk, Chunk targetChunk)
        {
            var dir = targetChunk.ChunkIndex - sourceChunk.ChunkIndex;

            var step = new Int2(Math.Abs(Math.Sign(dir.Y)), Math.Abs(Math.Sign(dir.X)));
            var map = EdgeMapping[dir];

            var source = map[0];
            var target = map[1];

            var height = Math.Min(sourceChunk.CurrentHeight, targetChunk.CurrentHeight);

            for (var n = 0; n < Chunk.SizeXy; n++)
            {
                for (var j = height - 1; j > 0; j--)
                {

                    if (sourceChunk.ChunkIndex == new Int2(0, 0) && j == 2)
                    {

                    }

                    // this whole thing is an educated first guess
                    var sourceIndexX = source.X + step.X * n;
                    var sourceIndexY = source.Y + step.Y * n;
                    var sourceFlatIndex = Help.GetFlatIndex(sourceIndexX, j, sourceIndexY, sourceChunk.CurrentHeight);
                    var sourceVoxel = sourceChunk.GetVoxelNoInline(sourceFlatIndex);
                    var sourceDefinition = VoxelDefinition.DefinitionByType[sourceVoxel.Block];
                    if (sourceDefinition.IsOpaque)
                    {
                        continue;
                    }

                    var targetIndexX = target.X + step.X * n;
                    var targetIndexY = target.Y + step.Y * n;
                    var targetFlatIndex = Help.GetFlatIndex(targetIndexX, j, targetIndexY, targetChunk.CurrentHeight);
                    var targetVoxel = targetChunk.GetVoxelNoInline(targetFlatIndex);
                    var targetDefinition = VoxelDefinition.DefinitionByType[targetVoxel.Block];
                    if (targetDefinition.IsOpaque)
                    {
                        continue;
                    }


                    var sourceToTargetDir = targetChunk.ChunkIndex - sourceChunk.ChunkIndex;
                    var sourceDirection = DirFaceMapping[sourceToTargetDir];
                    var sourceToTargetDrop = VoxelDefinition.GetBrightnessLoss(sourceDefinition, targetDefinition, sourceDirection);
                    if (targetVoxel.Lightness < sourceVoxel.Lightness - sourceToTargetDrop)
                    {
                        var newTargetVoxel = new Voxel(targetVoxel.Block, (byte)(sourceVoxel.Lightness - sourceToTargetDrop));
                        targetChunk.SetVoxelNoInline(targetFlatIndex, newTargetVoxel);
                        //PropagateLight(new PropagateLightRecord(targetChunk, targetIndexX, j, targetIndexY, targetDefinition, newTargetVoxel));
                        LocalLightPropagationService.InitializeLocalLight(targetChunk, targetFlatIndex);
                    }


                    //var targetToSourceDir = sourceChunk.ChunkIndex - targetChunk.ChunkIndex;
                    //var targetDirection = DirFaceMapping[targetToSourceDir];
                    //var targetToSourceDrop = VoxelDefinition.GetBrightnessLoss(targetDefinition, sourceDefinition, targetDirection);
                    //if (sourceVoxel.Lightness < targetVoxel.Lightness - targetToSourceDrop)
                    //{
                    //    var newSourceVoxel = new Voxel(sourceVoxel.Block, (byte)(targetVoxel.Lightness - targetToSourceDrop));
                    //    sourceChunk.SetVoxelNoInline(sourceFlatIndex, newSourceVoxel);
                    //    //PropagateLight(new PropagateLightRecord(sourceChunk, sourceIndexX, j, sourceIndexY, sourceDefinition, newSourceVoxel));
                    //    LocalLightPropagationService.InitializeLocalLight(targetChunk, sourceFlatIndex);
                    //}
                }
            }
        }
    }
}
