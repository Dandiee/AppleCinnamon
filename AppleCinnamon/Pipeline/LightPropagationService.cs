using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using AppleCinnamon.System;
using SharpDX;

namespace AppleCinnamon.Pipeline
{
    public interface ILightPropagationService
    {
        DataflowContext<Chunk> InitializeLocalLight(DataflowContext<Chunk> context);
        void PropagateSunlight(Chunk chunk, Int3 index, Int3 sourceIndex);
    }

    public sealed class LightPropagationService : ILightPropagationService
    {
        public static readonly Int3[] Directions = { Int3.UnitY, -Int3.UnitY, -Int3.UnitX, Int3.UnitX, -Int3.UnitZ, Int3.UnitZ };
        public static readonly  ConcurrentDictionary<Int2, long> Debug = new ConcurrentDictionary<Int2, long>();

        public DataflowContext<Chunk> InitializeLocalLight(DataflowContext<Chunk> context)
        {
            var sw = Stopwatch.StartNew();
            var lightSources = InitializeSunlight(context.Payload);
            PropagateSunlight(context.Payload, lightSources);
            sw.Stop();
            if (!Debug.TryAdd(context.Payload.ChunkIndex, sw.ElapsedMilliseconds))
            {
                throw new Exception();
            }
            return new DataflowContext<Chunk>(context, context.Payload, sw.ElapsedMilliseconds, nameof(LightPropagationService));
        }

        public void PropagateSunlight(Chunk chunk, Int3 index, Int3 sourceIndex)
        {
            var voxel = chunk.GetLocalVoxel(index.X, index.Y, index.Z);
            if (voxel.Block == 0 && voxel.Lightness > 0)
            {
                foreach (var direction in Directions)
                {
                    var neighbourIndex = new Int3(index.X + direction.X, index.Y + direction.Y, index.Z + direction.Z);
                    if (sourceIndex.X == neighbourIndex.X && sourceIndex.Y == neighbourIndex.Y && sourceIndex.Z == neighbourIndex.Z)
                    {
                        continue;
                    }

                    if ((byte)neighbourIndex.X < Chunk.Size.X && 
                        neighbourIndex.Y < Chunk.Size.Y && neighbourIndex.Y > -1 &&
                        (byte)neighbourIndex.Z < Chunk.Size.Z)
                    {
                        var neighbourVoxel = chunk.GetLocalVoxel(neighbourIndex.X, neighbourIndex.Y, neighbourIndex.Z);
                        if (neighbourVoxel.Block == 0 && neighbourVoxel.Lightness < voxel.Lightness - 1)
                        {
                            chunk.SetLocalVoxel(neighbourIndex.X, neighbourIndex.Y, neighbourIndex.Z,
                                new Voxel(neighbourVoxel.Block, (byte) (voxel.Lightness - 1)));

                            PropagateSunlight(chunk, neighbourIndex, -direction);
                        }
                    }
                }
            }
        }


        private void PropagateSunlight(Chunk chunk, List<Int3> lightSources)
        {
            foreach (var index in lightSources)
            {
                PropagateSunlight(chunk, index, index);
            }
        }

        private List<Int3> InitializeSunlight(Chunk chunk)
        {
            var lightSources = new List<Int3>();

            for (var i = 0; i != Chunk.Size.X; i++)
            {
                for (var k = 0; k != Chunk.Size.Z; k++)
                {
                    for (var j = Chunk.Size.Y - 1; j > 0; j--)
                    {
                        var voxel = chunk.GetLocalVoxel(i, j, k);
                        if (voxel.Block > 0)
                        {
                            break;
                        }

                        chunk.SetLocalVoxel(i, j, k, new Voxel(voxel.Block, 15));
                        lightSources.Add(new Int3(i, j, k));
                    }
                }
            }

            return lightSources;
        }
    }
}
