using System;
using System.Collections.Generic;
using AppleCinnamon.Helper;
using AppleCinnamon.Pipeline;
using AppleCinnamon.Settings;
using SharpDX;

namespace AppleCinnamon
{
    public readonly struct LightDirections
    {
        public readonly Face Direction;
        public readonly Int3 Step;
        public readonly Bool3 Bools;

        public LightDirections(Face direction, Int3 step, Bool3 bools)
        {
            Direction = direction;
            Step = step;
            Bools = bools;
        }

        public static readonly LightDirections[] All =
        {
            new(Face.Top, Int3.UnitY, Bool3.UnitY),
            new(Face.Bottom, -Int3.UnitY, Bool3.UnitY),
            new(Face.Left, -Int3.UnitX, Bool3.UnitX),
            new(Face.Right, Int3.UnitX, Bool3.UnitX),
            new(Face.Front, -Int3.UnitZ, Bool3.UnitZ),
            new(Face.Back, Int3.UnitZ, Bool3.UnitZ)
        };
    }

    public sealed class LightUpdater
    {
        public void UpdateLighting(Chunk chunk, Int3 relativeIndex, Voxel oldVoxel, Voxel newVoxel)
        {
            var oldDefinition = oldVoxel.GetDefinition();
            var newDefinition = newVoxel.GetDefinition();

            if (newVoxel.Block == 0) // remove
            {
                if (oldDefinition.IsLightEmitter) // emitter
                {
                    RemoveEmitter(chunk, relativeIndex, oldVoxel);
                }
                else // solid
                {
                    RemoveSolid(chunk, relativeIndex);
                }
            }
            else // add
            {
                if (newDefinition.IsLightEmitter) // emitter
                {
                    AddEmitter(chunk, relativeIndex, newVoxel, newDefinition);
                }
                else // solid
                {
                    AddSolid(chunk, relativeIndex, oldVoxel, newVoxel);
                }
            }
        }

        private Tuple<Chunk, Int3, byte> GetBrightestneighbor(Chunk chunk, Int3 relativeIndex)
        {
            Tuple<Chunk, Int3, byte> brightest = null;

            foreach (var direction in LightDirections.All)
            {
                var neighborIndex = direction.Step + relativeIndex;
                var voxel = chunk.GetLocalWithneighbors(neighborIndex.X, neighborIndex.Y, neighborIndex.Z, out var address);

                if (brightest == null || brightest.Item3 < voxel.Lightness)
                {
                    //brightest = new Tuple<Chunk, Int3, byte>(chunk.neighbors[address.ChunkIndex],
                    brightest = new Tuple<Chunk, Int3, byte>(chunk.neighbors2[Help.GetChunkFlatIndex(address.ChunkIndex)],
                        address.RelativeVoxelIndex, voxel.Lightness);
                }
            }

            return brightest;
        }

        private void AddEmitter(Chunk chunk, Int3 relativeIndex, Voxel newVoxel, VoxelDefinition newDefinition)
        {
            LightingService.GlobalPropagate(chunk, relativeIndex);
        }

        private void RemoveEmitter(Chunk chunk, Int3 relativeIndex, Voxel oldVoxel)
        {
            var upperVoxelIndex = new Int3(relativeIndex.X, relativeIndex.Y + 1, relativeIndex.Z);
            var upperVoxel = chunk.GetVoxel(upperVoxelIndex.ToFlatIndex(chunk.CurrentHeight));
            var lightSources = new List<Tuple<Chunk, Int3>>();
            LightingService.GlobalPropagateDarkness(new LightingService.DarknessPropogationRecord(chunk, relativeIndex, lightSources, oldVoxel, VoxelDefinition.DefinitionByType[oldVoxel.Block]));

            if (upperVoxel.Lightness == 15)
            {
                foreach (var sunlightSources in LightingService.Sunlight(chunk, upperVoxelIndex, 15))
                {
                    lightSources.Add(new Tuple<Chunk, Int3>(chunk, sunlightSources));
                }
            }

            // propagate lightness
            foreach (var lightSource in lightSources)
            {
                LightingService.GlobalPropagate(lightSource.Item1, lightSource.Item2);
            }
        }

        private void RemoveSolid(Chunk chunk, Int3 relativeIndex)
        {
            var upperIndex = new Int3(relativeIndex.X, relativeIndex.Y + 1, relativeIndex.Z);
            var upperVoxel = chunk.CurrentHeight <= relativeIndex.Y + 1
                             ? Voxel.Air
                             : chunk.GetVoxel(upperIndex.ToFlatIndex(chunk.CurrentHeight));

            // identify light sources
            var lightSources = new List<Tuple<Chunk, Int3>>();

            if (upperVoxel.Lightness == 15)
            {
                foreach (var sunlightSources in LightingService.Sunlight(chunk, upperIndex, 15))
                {
                    lightSources.Add(new Tuple<Chunk, Int3>(chunk, sunlightSources));
                }
            }

            else
            {
                var brightestneighbor = GetBrightestneighbor(chunk, relativeIndex);
                if (brightestneighbor.Item3 > 0)
                {
                    lightSources.Add(new Tuple<Chunk, Int3>(brightestneighbor.Item1, brightestneighbor.Item2));
                }
            }


            // propagate lightness
            foreach (var lightSource in lightSources)
            {
                LightingService.GlobalPropagate(lightSource.Item1, lightSource.Item2);
            }
        }



        private void AddSolid(Chunk chunk, Int3 relativeIndex, Voxel oldVoxel, Voxel newVoxel)
        {
            var resetVoxels = new List<Int3> { relativeIndex };
            if (oldVoxel.Lightness == 15)
            {
                resetVoxels.AddRange(LightingService.Sunlight(chunk, relativeIndex, 0));
            }

            // propagate darkness
            var lightSources = new List<Tuple<Chunk, Int3>>();
            foreach (var resetVoxel in resetVoxels)
            {
                LightingService.GlobalPropagateDarkness(new LightingService.DarknessPropogationRecord(chunk, resetVoxel, lightSources, oldVoxel, VoxelDefinition.DefinitionByType[oldVoxel.Block]));
            }

            foreach (var lightSource in lightSources)
            {
                LightingService.GlobalPropagate(lightSource.Item1, lightSource.Item2);
            }
        }
    }

}
