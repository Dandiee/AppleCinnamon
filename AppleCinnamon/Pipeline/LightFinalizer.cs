﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using AppleCinnamon.Settings;
using AppleCinnamon.System;
using SharpDX;

namespace AppleCinnamon.Pipeline
{
    public interface ILightFinalizer
    {
        DataflowContext<Chunk> Finalize(DataflowContext<Chunk> context);
    }

    public sealed class LightFinalizer : ILightFinalizer
    {
        private static readonly Int2[] Corners = {new Int2(-1, -1), new Int2(-1, +1), new Int2(+1, -1), new Int2(+1, +1)};
        private static readonly Int2[] Edges = {Int2.UniX, -Int2.UniX, Int2.UniY, -Int2.UniY};
        public static readonly Int3[] Directions = { Int3.UnitY, -Int3.UnitY, -Int3.UnitX, Int3.UnitX, -Int3.UnitZ, Int3.UnitZ };
        private static readonly IDictionary<Int2, Int2[]> EdgeMapping = new Dictionary<Int2, Int2[]>
        {
            [Int2.UniX] = new[] { new Int2(15, 0), new Int2(0, 0) },
            [-Int2.UniX] = new[] { new Int2(0, 0), new Int2(15, 0) },
            [Int2.UniY] = new[] { new Int2(0, 15), new Int2(0, 0) },
            [-Int2.UniY] = new[] { new Int2(0, 0), new Int2(0, 15) }
        };

        public static readonly Tuple<Int3, Bool3>[] Directions2 =
        {
            new Tuple<Int3, Bool3>(Int3.UnitY, Bool3.UnitY),
            new Tuple<Int3, Bool3>(-Int3.UnitY, Bool3.UnitY),
            new Tuple<Int3, Bool3>(-Int3.UnitX, Bool3.UnitX),
            new Tuple<Int3, Bool3>(Int3.UnitX, Bool3.UnitX),
            new Tuple<Int3, Bool3>(-Int3.UnitZ, Bool3.UnitZ),
            new Tuple<Int3, Bool3>(Int3.UnitZ, Bool3.UnitZ)
        };


        public DataflowContext<Chunk> Finalize(DataflowContext<Chunk> context)
        {
            var sw = Stopwatch.StartNew();
            var chunk = context.Payload;


            foreach (var corner in Corners)
            {
                var cornerChunk = chunk.Neighbours[corner];

                ProcessEdge(cornerChunk, chunk.Neighbours[new Int2(corner.X, 0)]);
                ProcessEdge(cornerChunk, chunk.Neighbours[new Int2(0, corner.Y)]);
            }

            foreach (var edge in Edges)
            {
                var edgeChunk = chunk.Neighbours[edge];

                var offset = new Int2(Math.Sign(edge.Y), Math.Sign(edge.X));
                ProcessEdge(chunk.Neighbours[edge + offset], edgeChunk);
                ProcessEdge(chunk.Neighbours[edge - offset], edgeChunk);

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

            var sourceVoxels = sourceChunk.Voxels;
            var targetVoxels = targetChunk.Voxels;
            
            for (var n = 0; n < Chunk.SizeXy; n++)
            {
                for (var j = Chunk.Height - 1; j > 0; j--)
                {
                    
                    var sourceIndexX = source.X + step.X * n;
                    var sourceIndexY = source.Y + step.Y * n;
                    var sourceIndex = sourceIndexX + Chunk.SizeXy * (j + Chunk.Height * sourceIndexY);

                    var sourceVoxel = sourceVoxels[sourceIndex];
                    var sourceDefinition = VoxelDefinition.DefinitionByType[sourceVoxel.Block];

                    if (!sourceDefinition.IsTransparent)
                    {
                        continue;
                    }

                    var targetIndexX = target.X + step.X * n;
                    var targetIndexY = target.Y + step.Y * n;
                    var targetIndex = targetIndexX + Chunk.SizeXy * (j + Chunk.Height * targetIndexY);

                    var targetVoxel = targetVoxels[targetIndex];
                    var targetDefinition = VoxelDefinition.DefinitionByType[targetVoxel.Block];

                    if (!targetDefinition.IsTransparent)
                    {
                        continue;
                    }

                    var lightDifference = targetVoxel.Lightness - sourceVoxel.Lightness;
                    if (Math.Abs(lightDifference) > 1)
                    {
                        // target -> source
                        if (lightDifference > 0)
                        {
                            var newSourceVoxel = new Voxel(sourceVoxel.Block, (byte)(targetVoxel.Lightness - 1));
                            sourceVoxels[sourceIndex] = newSourceVoxel;
                            PropagateSunlight(sourceVoxels, sourceIndexX, j, sourceIndexY, sourceDefinition, newSourceVoxel);
                        }
                        else // source -> target
                        {
                            var newTargetVoxel = new Voxel(targetVoxel.Block, (byte)(sourceVoxel.Lightness - 1));
                            targetVoxels[targetIndex] = newTargetVoxel;
                            PropagateSunlight(targetVoxels, targetIndexX, j, targetIndexY, targetDefinition, newTargetVoxel);
                        }
                    }
                }
            }
        }

        private void PropagateSunlight(Voxel[] voxels, int sourceIndexX, int sourceIndexY, int sourceIndexZ, VoxelDefinition sourceDefinition, Voxel sourceVoxel)
        {
            if (sourceDefinition.IsTransparent && sourceVoxel.Lightness > 0)
            {
                foreach (var direction in Directions2)
                {
                    var neighbourIndexX = sourceIndexX + direction.Item1.X;
                    var neighbourIndexY = sourceIndexY + direction.Item1.Y;
                    var neighbourIndexZ = sourceIndexZ + direction.Item1.Z;

                    if ((neighbourIndexX & 16) == 0 && ((ushort)neighbourIndexY & 256) == 0 && (neighbourIndexZ & 16) == 0)
                    {
                        var flatNeighbourIndex = neighbourIndexX + Chunk.SizeXy * (neighbourIndexY + Chunk.Height * neighbourIndexZ);
                        var neighbourVoxel = voxels[flatNeighbourIndex];

                        if (neighbourVoxel.Lightness < sourceVoxel.Lightness - 1)
                        {
                            var targetDefinition = VoxelDefinition.DefinitionByType[neighbourVoxel.Block];
                            if (targetDefinition.IsTransparent)
                            {
                                var newTargetVoxel = new Voxel(neighbourVoxel.Block, (byte) (sourceVoxel.Lightness - 1));
                                voxels[flatNeighbourIndex] = newTargetVoxel;
                                PropagateSunlight(voxels, neighbourIndexX, neighbourIndexY, neighbourIndexZ, targetDefinition, newTargetVoxel);
                            }
                        }
                    }
                }
            }
        }
    }
}
