using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private static readonly Int2[] Corners =
            {new Int2(-1, -1), new Int2(-1, +1), new Int2(+1, -1), new Int2(+1, +1)};

        private static readonly Int2[] Edges =
            {Int2.UniX, -Int2.UniX, Int2.UniY, -Int2.UniY};

        private static readonly IDictionary<Int2, Int2[]> EdgeMapping = new Dictionary<Int2, Int2[]>
        {
            [Int2.UniX] = new[] { new Int2(15, 0), new Int2(0, 0) },
            [-Int2.UniX] = new[] { new Int2(0, 0), new Int2(15, 0) },
            [Int2.UniY] = new[] { new Int2(0, 15), new Int2(0, 0) },
            [-Int2.UniY] = new[] { new Int2(0, 0), new Int2(0, 15) }
        };

        private readonly ILightPropagationService _lightPropagationService;

        public LightFinalizer()
        {
            _lightPropagationService = new LightPropagationService();
        }

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
            
            for (var n = 0; n < Chunk.Size.X; n++)
            {
                for (var j = Chunk.Size.Y - 1; j > 0; j--)
                {
                    var sourceIndex = new Int2(source.X + step.X * n, source.Y + step.Y * n);// source + step * n;
                    var sourceVoxel = sourceChunk.GetLocalVoxel(sourceIndex.X, j, sourceIndex.Y);
                    var sourceDefinition = sourceVoxel.GetDefinition();

                    if (
                        !((dir.X == 1 && sourceDefinition.IsTransmittance.X ) || (dir.Y == 1 && sourceDefinition.IsTransmittance.Z))
                        //!sourceDefinition.IsTransmittance 
                        || sourceVoxel.Lightness == 0)
                    {
                        continue;
                    }

                    var targetIndex = new Int2(target.X + step.X * n, target.Y + step.Y * n);
                    var targetVoxelIndex = new Int3(targetIndex.X, j, targetIndex.Y);
                    var targetVoxel = targetChunk.GetLocalVoxel(targetVoxelIndex);
                    var targetDefinition = targetVoxel.GetDefinition();

                    if (
                        ((dir.X == 1 && targetDefinition.IsTransmittance.X) || (dir.Y == 1 && targetDefinition.IsTransmittance.Z))
                        // targetDefinition.IsTransmittance 
                        
                        && targetVoxel.Lightness < sourceVoxel.Lightness - 1)
                    {
                        targetChunk.SetLocalVoxel(targetVoxelIndex,
                            new Voxel(targetVoxel.Block, (byte)(sourceVoxel.Lightness - 1)));
                        _lightPropagationService.PropagateSunlight(targetChunk, targetVoxelIndex, -Int3.One);
                    }
                }
            }
        }
    }
}
