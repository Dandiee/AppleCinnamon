using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using AppleCinnamon.Settings;
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
        public static readonly  ConcurrentDictionary<Int2, long> Debug = new ConcurrentDictionary<Int2, long>();

        public static readonly Tuple<Int3, Bool3>[] Directions =
        {
            new Tuple<Int3, Bool3>(Int3.UnitY, Bool3.UnitY),
            new Tuple<Int3, Bool3>(-Int3.UnitY, Bool3.UnitY),
            new Tuple<Int3, Bool3>(-Int3.UnitX, Bool3.UnitX),
            new Tuple<Int3, Bool3>(Int3.UnitX, Bool3.UnitX),
            new Tuple<Int3, Bool3>(-Int3.UnitZ, Bool3.UnitZ),
            new Tuple<Int3, Bool3>(Int3.UnitZ, Bool3.UnitZ)
        };

        public DataflowContext<Chunk> InitializeLocalLight(DataflowContext<Chunk> context)
        {
            var sw = Stopwatch.StartNew();

            ItsMoreComplexButIsItFasterTestMethod(context.Payload);
            // var lightSources = InitializeSunlight(context.Payload);
            // PropagateSunlight(context.Payload, lightSources);
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
            var definition = voxel.GetDefinition();
            if (definition.IsTransmittance.Y && voxel.Lightness > 0)
            {
                foreach (var direction in Directions)
                {
                    var dirBool3 = direction.Item2.Bytes;

                    var neighbourIndex = new Int3(index.X + direction.Item1.X, index.Y + direction.Item1.Y, index.Z + direction.Item1.Z);
                    if (sourceIndex.X == neighbourIndex.X && sourceIndex.Y == neighbourIndex.Y && sourceIndex.Z == neighbourIndex.Z)
                    {
                        continue;
                    }

                    if ((byte)neighbourIndex.X < Chunk.Size.X && 
                        neighbourIndex.Y < Chunk.Size.Y && neighbourIndex.Y > -1 &&
                        (byte)neighbourIndex.Z < Chunk.Size.Z)
                    {
                        var neighbourVoxel = chunk.GetLocalVoxel(neighbourIndex.X, neighbourIndex.Y, neighbourIndex.Z);


                        var condition1 = (dirBool3 & VoxelDefinition.DefinitionByType[neighbourVoxel.Block].TransmittanceBytes) > 0;

                        if (condition1)
                        {
                            var condition2 = condition1 && neighbourVoxel.Lightness < voxel.Lightness - 1;

                            if(condition2)
                            {
                                chunk.SetLocalVoxel(neighbourIndex.X, neighbourIndex.Y, neighbourIndex.Z,
                                    new Voxel(neighbourVoxel.Block, (byte) (voxel.Lightness - 1)));

                                PropagateSunlight(chunk, neighbourIndex, -direction.Item1);
                            }
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
                        var definition = voxel.GetDefinition();
                        if (!definition.IsTransmittance.Y)
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
            var chunk = result.Chunk;

            var voxel = chunk.GetLocalVoxel(lightSource.X, lightSource.Y, lightSource.Z);

            for (var i = -1; i < 2; i++)
            {
                for (var j = -1; j < 2; j++)
                {
                    for (var k = -1; k < 2; k++)
                    {
                        var neighbourIndex = new Int3(lightSource.X + i, lightSource.Y + j, lightSource.Z + k);
                        if ((byte) neighbourIndex.X < Chunk.SizeXy &&
                            neighbourIndex.Y < Chunk.Height && neighbourIndex.Y > -1 &&
                            (byte) neighbourIndex.Z < Chunk.SizeXy)
                        {
                            {
                                var neighbourVoxel = chunk.GetLocalVoxel(neighbourIndex.X, neighbourIndex.Y,
                                    neighbourIndex.Z);

                                if (neighbourVoxel.Lightness < voxel.Lightness - 1)
                                {
                                    chunk.SetLocalVoxel(neighbourIndex.X, neighbourIndex.Y, neighbourIndex.Z,
                                        new Voxel(neighbourVoxel.Block, (byte) (voxel.Lightness - 1)));

                                    lightSources.Add(neighbourIndex);
                                    // PropagateLightSource(neighbourIndex, result);
                                }
                            }
                        }
                    }
                }
            }
        }

        private List<Int3> FindLightSources(LightPropagationResult initLightResult)
        {
            var chunk = initLightResult.Chunk;
            var result = new List<Int3>();

            foreach (var darkSpot in initLightResult.DarkSpots)
            {
                var maximumVoxel = Voxel.Zero;
                var maximumIndex = Int3.Zero;
                var foundLightSource = false;
                var isBroken = false;

                for (var i = -1; i < 2 && !isBroken; i++)
                {
                    for (var j = -1; j < 2 && !isBroken; j++)
                    {
                        for (var k = -1; k < 2 && !isBroken; k++)
                        {
                            var neighbourIndex = new Int3(darkSpot.X + i, darkSpot.Y + j, darkSpot.Z + k);

                            if ((neighbourIndex.X & 16) == 0 &&
                                ((short)neighbourIndex.Y & 256) == 0 &&
                                (neighbourIndex.Z & 16) == 0)
                            {

                                var voxel = chunk.GetLocalVoxel(darkSpot.X + i,
                                    darkSpot.Y + j,
                                    darkSpot.Z + k);

                                if (voxel.Lightness == 15)
                                {
                                    maximumVoxel = voxel;
                                    maximumIndex = neighbourIndex;
                                    isBroken = true;
                                }
                                // else if (maximumVoxel.Lightness < voxel.Lightness)
                                // {
                                //     foundLightSource = true;
                                //     maximumVoxel = voxel;
                                //     maximumIndex = neighbourIndex;
                                // }
                            }
                        }
                    }
                }

                if (isBroken || foundLightSource)
                {
                    result.Add(maximumIndex);
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
                                // result.LightSpots.Add(new Int3(i, j, k));
                                chunk.SetLocalVoxel(i, j, k, new Voxel(voxel.Block, 15));
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
        // public List<Int3> LightSpots { get; set; }

        public LightPropagationResult(Chunk chunk)
        {
            Chunk = chunk;
            DarkSpots = new List<Int3>();
            // LightSpots = new List<Int3>();
        }
    }

}
