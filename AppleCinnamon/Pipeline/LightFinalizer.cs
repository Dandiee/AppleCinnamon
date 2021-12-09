using System;
using System.Collections.Generic;
using System.Diagnostics;
using AppleCinnamon.Settings;
using AppleCinnamon.System;
using SharpDX;

namespace AppleCinnamon.Pipeline
{
    public sealed class LightFinalizer
    {
        private static readonly Int2[] Corners = { new(-1, -1), new(-1, +1), new(+1, -1), new(+1, +1) };
        private static readonly Int2[] Edges = { Int2.UniX, -Int2.UniX, Int2.UniY, -Int2.UniY };

        private static readonly IDictionary<Int2, Int2[]> EdgeMapping = new Dictionary<Int2, Int2[]>
        {
            [Int2.UniX] = new[] { new Int2(Chunk.SizeXy - 1, 0), new Int2(0, 0) },
            [-Int2.UniX] = new[] { new Int2(0, 0), new Int2(Chunk.SizeXy - 1, 0) },
            [Int2.UniY] = new[] { new Int2(0, Chunk.SizeXy - 1), new Int2(0, 0) },
            [-Int2.UniY] = new[] { new Int2(0, 0), new Int2(0, Chunk.SizeXy - 1) }
        };

        public static readonly Tuple<Int3, Bool3>[] Directions2 =
        {
            new(Int3.UnitY, Bool3.UnitY),
            new(-Int3.UnitY, Bool3.UnitY),
            new(-Int3.UnitX, Bool3.UnitX),
            new(Int3.UnitX, Bool3.UnitX),
            new(-Int3.UnitZ, Bool3.UnitZ),
            new(Int3.UnitZ, Bool3.UnitZ)
        };


        public DataflowContext<Chunk> Finalize(DataflowContext<Chunk> context)
        {
            var sw = Stopwatch.StartNew();
            var chunk = context.Payload;


            foreach (var corner in Corners)
            {
                var cornerChunk = chunk.neighbors2[Help.GetChunkFlatIndex(corner)];
                ProcessEdge(cornerChunk, chunk.neighbors2[ Help.GetChunkFlatIndex(corner.X, 0)]);
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
                    var sourceIndexX = source.X + step.X * n;
                    var sourceIndexY = source.Y + step.Y * n;
                    var sourceIndex = Help.GetFlatIndex(sourceIndexX, j, sourceIndexY, sourceChunk.CurrentHeight);
                    var sourceVoxel = sourceChunk.GetVoxelNoInline(sourceIndex);
                    var sourceDefinition = VoxelDefinition.DefinitionByType[sourceVoxel.Block];
                    if (sourceDefinition.IsBlock) // TODO: baj, sok
                    {
                        continue;
                    }

                    var targetIndexX = target.X + step.X * n;
                    var targetIndexY = target.Y + step.Y * n;
                    var targetIndex = Help.GetFlatIndex(targetIndexX, j, targetIndexY, targetChunk.CurrentHeight);
                    var targetVoxel = targetChunk.GetVoxelNoInline(targetIndex);
                    var targetDefinition = VoxelDefinition.DefinitionByType[targetVoxel.Block];
                    if (targetDefinition.IsBlock)
                    {
                        continue;
                    }

                    var lightDifference = targetVoxel.Lightness - sourceVoxel.Lightness;
                    if (Math.Abs(lightDifference) > 1)
                    {
                        if (lightDifference > 0) // target -> source
                        {
                            var newSourceVoxel = new Voxel(sourceVoxel.Block, (byte)(targetVoxel.Lightness - 1));
                            sourceChunk.SetVoxelNoInline(sourceIndex, newSourceVoxel);
                            PropagateSunlight(sourceChunk, sourceIndexX, j, sourceIndexY, sourceDefinition, newSourceVoxel);
                        }
                        else // source -> target
                        {
                            var newTargetVoxel = new Voxel(targetVoxel.Block, (byte)(sourceVoxel.Lightness - 1));
                            targetChunk.SetVoxelNoInline(targetIndex, newTargetVoxel);
                            PropagateSunlight(targetChunk, targetIndexX, j, targetIndexY, targetDefinition, newTargetVoxel);
                        }
                    }
                }
            }
        }

        private void PropagateSunlight(Chunk chunk, int sourceIndexX, int sourceIndexY, int sourceIndexZ, VoxelDefinition sourceDefinition, Voxel sourceVoxel)
        {
            if (sourceDefinition.TransmittanceQuarters[(byte)Face.Bottom] > 0 && sourceVoxel.Lightness > 0) 
            //if (sourceDefinition.IsTransparent && sourceVoxel.Lightness > 0)
            {
                foreach (var direction in Directions2)
                {
                    var neighborIndexX = sourceIndexX + direction.Item1.X;
                    var neighborIndexY = sourceIndexY + direction.Item1.Y;
                    var neighborIndexZ = sourceIndexZ + direction.Item1.Z;

                    if ((neighborIndexX & Chunk.SizeXy) == 0 &&
                        (neighborIndexZ & Chunk.SizeXy) == 0 &&
                        neighborIndexZ > 0 && neighborIndexZ < chunk.CurrentHeight)
                    {
                        var neighborIndex = Help.GetFlatIndex(neighborIndexX, neighborIndexY, neighborIndexZ, chunk.CurrentHeight);
                        var neighborVoxel = chunk.GetVoxelNoInline(neighborIndex);
                        if (neighborVoxel.Lightness < sourceVoxel.Lightness - 1)
                        {
                            var targetDefinition = VoxelDefinition.DefinitionByType[neighborVoxel.Block];
                            if (sourceDefinition.TransmittanceQuarters[(byte)Face.Bottom] > 0)
                            //if (targetDefinition.IsTransparent)
                            {
                                var newTargetVoxel = new Voxel(neighborVoxel.Block, (byte)(sourceVoxel.Lightness - 1));
                                chunk.SetVoxelNoInline(neighborIndex, newTargetVoxel);

                                PropagateSunlight(chunk, neighborIndexX, neighborIndexY, neighborIndexZ, targetDefinition, newTargetVoxel);
                            }
                        }
                    }
                }
            }
        }
    }
}
