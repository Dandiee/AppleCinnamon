using System.Collections.Generic;
using System.Diagnostics;
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

        public static readonly Int3[] Directions =
        {
            Int3.UnitY, -Int3.UnitY, -Int3.UnitX, Int3.UnitX, -Int3.UnitZ, Int3.UnitZ
        };


        //public DataflowContext<Chunk> InitializeLocalLight(DataflowContext<Chunk> context)
        //{
        //    var sw = Stopwatch.StartNew();

        //    ItsMoreComplexButIsItFasterTestMethod(context.Payload);
        //    sw.Stop();

        //    return new DataflowContext<Chunk>(context, context.Payload, sw.ElapsedMilliseconds, nameof(LightPropagationService));
        //}


        public DataflowContext<Chunk> InitializeLocalLight(DataflowContext<Chunk> context)
        {
            var sw = Stopwatch.StartNew();
            var chunk = context.Payload;

            var lightPropagationVoxels = chunk.LightPropagationVoxels;


            for (var c = 0; c < lightPropagationVoxels.Count; c++)
            {
                PropagateLightSource(chunk, lightPropagationVoxels[c], lightPropagationVoxels);
            }

            sw.Stop();

            return new DataflowContext<Chunk>(context, context.Payload, sw.ElapsedMilliseconds, nameof(LightPropagationService));
        }



        private void PropagateLightSource(Chunk chunk, int lightSourceIndex, List<int> lightSources)
        {
            

            var voxels = chunk.Voxels;

                var voxelLightness = voxels[lightSourceIndex].Lightness;
            var k = lightSourceIndex / (Chunk.SizeXy * Chunk.Height);
            var j = (lightSourceIndex - k * Chunk.SizeXy * Chunk.Height) / Chunk.SizeXy;
            var i = lightSourceIndex - (k * Chunk.SizeXy * Chunk.Height + j * Chunk.SizeXy);

            foreach (var direction in Directions)
            {
                var neighbourX = i + direction.X;
                if ((neighbourX & 16) == 0)
                {
                    var neighbourY = j + direction.Y;
                    if (((ushort)neighbourY & 256) == 0)
                    {
                        var neighbourZ = k + direction.Z;
                        if ((neighbourZ & 16) == 0)
                        {
                            var neighbourIndex = neighbourX + Chunk.SizeXy * (neighbourY + Chunk.Height * neighbourZ);
                            var neighbourVoxel = voxels[neighbourIndex];

                            if (neighbourVoxel.Lightness < voxelLightness - 1)
                            {
                                voxels[neighbourIndex] = new Voxel(neighbourVoxel.Block, (byte)(voxelLightness - 1));

                                lightSources.Add(neighbourIndex);
                            }
                        }
                    }
                }
            }
        }


        private void ItsMoreComplexButIsItFasterTestMethod(Chunk chunk)
        {
            var result = GetDarkVoxels(chunk);
            var lightSources = FindLightSources(chunk, result);

            // no it cannot be a foreach, the collection is mutating (still better then recursion)
            for (var i = 0; i < lightSources.Count; i++)
            {
                PropagateLightSource(chunk, lightSources[i], lightSources);
            }
        }

        private void PropagateLightSource(Chunk chunk, Int3 lightSource, List<Int3> lightSources)
        {
            var voxels = chunk.Voxels;
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

                                lightSources.Add(new Int3(neighbourX, neighbourY, neighbourZ));
                            }
                        }
                    }
                }
            }
        }

        

        private List<Int3> FindLightSources(Chunk chunk, List<Int3> darkVoxels)
        {
            var voxels = chunk.Voxels;
            var result = new List<Int3>();

            foreach (var darkVoxel in darkVoxels)
            {
                foreach (var direction in Directions)
                {
                    var neighbourX = darkVoxel.X + direction.X;
                    if ((neighbourX & 16) == 0)
                    {
                        var neighbourY = darkVoxel.Y + direction.Y;
                        if (((ushort) neighbourY & 256) == 0)
                        {
                            var neighbourZ = darkVoxel.Z + direction.Z;
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

        private List<Int3> GetDarkVoxels(Chunk chunk)
        {
            var result = new List<Int3>(512);

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
                                result.Add(new Int3(i, j, k));
                            }
                        }
                    }
                }
            }

            return result;
        }
    }
}
