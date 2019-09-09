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
    }

    public sealed class LightPropagationService : ILightPropagationService
    {
        private const byte MaximumSunlight = 15;
        public static readonly ConcurrentDictionary<Int2, long> Debug = new ConcurrentDictionary<Int2, long>();

        public static readonly Int3[] Directions =
        {
            Int3.UnitY, -Int3.UnitY, -Int3.UnitX, Int3.UnitX, -Int3.UnitZ, Int3.UnitZ
        };


        public DataflowContext<Chunk> InitializeLocalLight(DataflowContext<Chunk> context)
        {
            var sw = Stopwatch.StartNew();

            ItsMoreComplexButIsItFasterTestMethod(context.Payload);
            //var lightSources = InitializeSunlight(context.Payload);
            //PropagateSunlight(context.Payload, lightSources);
            sw.Stop();
            if (!Debug.TryAdd(context.Payload.ChunkIndex, sw.ElapsedMilliseconds))
            {
                throw new Exception();
            }
            return new DataflowContext<Chunk>(context, context.Payload, sw.ElapsedMilliseconds, nameof(LightPropagationService));
        }

        
        private void ItsMoreComplexButIsItFasterTestMethod(Chunk chunk)
        {
            var result = GetDarkVoxels(chunk);
            var lightSources = FindLightSources(result);

            for (var i = 0; i < lightSources.Count; i++)
            {
                PropagateLightSource(lightSources[i], result, lightSources);
            }
        }

        private void PropagateLightSource(Int3 lightSource, LightPropagationResult result, List<Int3> lightSources)
        {
            var voxels = result.Chunk.Voxels;
            var voxelLightness = voxels[lightSource.X + Chunk.SizeXy * (lightSource.Y + Chunk.Height * lightSource.Z)].Lightness;

            foreach (var direction in Directions)
            {
                var neighbourX = lightSource.X + direction.X;
                if ((neighbourX & 16) == 0)
                {
                    var neighbourY = lightSource.Y + direction.Y;
                    if (((ushort)neighbourY & 256) == 0)
                    {
                        var neighbourZ = lightSource.Z + direction.Z;
                        if ((neighbourZ & 16) == 0)
                        {
                            var neighbourVoxel =
                                voxels[neighbourX + Chunk.SizeXy * (neighbourY + Chunk.Height * neighbourZ)];

                            if (neighbourVoxel.Lightness < voxelLightness - 1)
                            {
                                voxels[neighbourX + Chunk.SizeXy * (neighbourY + Chunk.Height * neighbourZ)]
                                    = new Voxel(neighbourVoxel.Block, (byte)(voxelLightness - 1));

                                // PropagateLightSource(new Int3(neighbourX, neighbourY, neighbourZ), result);

                                lightSources.Add(new Int3(neighbourX, neighbourY, neighbourZ));
                            }
                        }
                    }
                }
            }
        }

        

        private List<Int3> FindLightSources(LightPropagationResult initLightResult)
        {
            var voxels = initLightResult.Chunk.Voxels;
            var result = new List<Int3>();

            foreach (var darkSpot in initLightResult.DarkSpots)
            {
                foreach (var direction in Directions)
                {
                    var neighbourX = darkSpot.X + direction.X;
                    if ((neighbourX & 16) == 0)
                    {
                        var neighbourY = darkSpot.Y + direction.Y;
                        if (((ushort) neighbourY & 256) == 0)
                        {
                            var neighbourZ = darkSpot.Z + direction.Z;
                            if ((neighbourZ & 16) == 0)
                            {
                                if (voxels[neighbourX + Chunk.SizeXy * (neighbourY + Chunk.Height * neighbourZ)].Lightness == MaximumSunlight)
                                {
                                    result.Add(new Int3(neighbourX, neighbourY, neighbourZ));
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        private LightPropagationResult GetDarkVoxels(Chunk chunk)
        {
            var result = new LightPropagationResult(chunk);

            for (var i = 0; i != Chunk.Size.X; i++)
            {
                for (var k = 0; k != Chunk.Size.Z; k++)
                {
                    var isOnSunlight = true;

                    for (var j = Chunk.Size.Y - 1; j > 0; j--)
                    {
                        var voxel = chunk.GetLocalVoxel(i, j, k);
                        var definition = voxel.GetDefinition();

                        var isTransmittance = definition.IsTransmittance.Y;

                        if (isOnSunlight)
                        {
                            if (isTransmittance)
                            {
                                chunk.SetLocalVoxel(i, j, k, new Voxel(voxel.Block, MaximumSunlight));
                            }
                            else isOnSunlight = false;
                        }
                        else
                        {
                            if (isTransmittance)
                            {
                                result.DarkSpots.Add(new Int3(i, j, k));
                            }
                        }
                    }
                }
            }

            return result;
        }
    }

    public class LightPropagationResult
    {
        public Chunk Chunk { get; }
        public List<Int3> DarkSpots { get; set; }

        public LightPropagationResult(Chunk chunk)
        {
            Chunk = chunk;
            DarkSpots = new List<Int3>(512);
        }
    }

}
