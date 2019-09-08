using System;
using System.Collections.Generic;
using AppleCinnamon.Settings;
using SharpDX;

namespace AppleCinnamon
{
    public interface ILightUpdater
    {
        void UpdateLighting(Chunk chunk, Int3 relativeIndex, Voxel oldVoxel, Voxel newVoxel);
    }

    public sealed class LightUpdater : ILightUpdater
    {
        public static readonly Int3[] Directions = { Int3.UnitY, -Int3.UnitY, -Int3.UnitX, Int3.UnitX, -Int3.UnitZ, Int3.UnitZ };

        public void UpdateLighting(Chunk chunk, Int3 relativeIndex, Voxel oldVoxel, Voxel newVoxel)
        {
            var oldDefinition = oldVoxel.GetDefinition();
            var newDefinition = newVoxel.GetDefinition();

            if (newVoxel.Block == 0) // remove
            {
                if (oldDefinition.LightEmitting > 0) // emitter
                {
                    RemoveEmitter(chunk, relativeIndex);
                }
                else // solid
                {
                    RemoveSolid(chunk, relativeIndex);
                }
            }
            else // add
            {
                if (newDefinition.LightEmitting > 0) // emitter
                {
                    AddEmitter(chunk, relativeIndex, newVoxel, newDefinition);
                }
                else // solid
                {
                    AddSolid(chunk, relativeIndex, oldVoxel);
                }
            }
        }

        private Tuple<Chunk, Int3, byte> GetBrightestNeighbour(Chunk chunk, Int3 relativeIndex)
        {
            Tuple<Chunk, Int3, byte> brightest = null;

            foreach (var direction in Directions)
            {
                var neighbourIndex = direction + relativeIndex;
                var voxel = chunk.GetLocalWithNeighbours(neighbourIndex.X, neighbourIndex.Y, neighbourIndex.Z, out var address);

                if (brightest == null || brightest.Item3 < voxel.Lightness)
                {
                    brightest = new Tuple<Chunk, Int3, byte>(chunk.Neighbours[address.ChunkIndex],
                        address.RelativeVoxelIndex, voxel.Lightness);
                }
            }

            return brightest;
        }

        private void AddEmitter(Chunk chunk, Int3 relativeIndex, Voxel newVoxel, VoxelDefinition newDefinition)
        {
            var brightestNeighbour = GetBrightestNeighbour(chunk, relativeIndex);

            chunk.SetLocalVoxel(relativeIndex,
                new Voxel(newVoxel.Block,  Math.Max(brightestNeighbour.Item3, newDefinition.LightEmitting)));

            PropagateLightness(chunk, relativeIndex);
        }

        private void RemoveEmitter(Chunk chunk, Int3 relativeIndex)
        {
            var upperVoxelIndex = new Int3(relativeIndex.X, relativeIndex.Y + 1, relativeIndex.Z);
            var upperVoxel = chunk.GetLocalVoxel(upperVoxelIndex);
            var lightSources = new List<Tuple<Chunk, Int3>>();
            PropagateDarkness(chunk, relativeIndex, lightSources);

            if (upperVoxel.Lightness == 15)
            {
                foreach (var sunlightSources in PropagateSunlight(chunk, upperVoxelIndex, 15))
                {
                    lightSources.Add(new Tuple<Chunk, Int3>(chunk, sunlightSources));
                }
            }

            // propagate lightness
            foreach (var lightSource in lightSources)
            {
                PropagateLightness(lightSource.Item1, lightSource.Item2);
            }
        }

        private void RemoveSolid(Chunk chunk, Int3 relativeIndex)
        {
            var upperIndex = new Int3(relativeIndex.X, relativeIndex.Y + 1, relativeIndex.Z);
            var upperVoxel = chunk.GetLocalVoxel(upperIndex);

            // identify light sources
            var lightSources = new List<Tuple<Chunk, Int3>>();

            if (upperVoxel.Lightness == 15)
            {
                foreach (var sunlightSources in PropagateSunlight(chunk, upperIndex, 15))
                {
                    lightSources.Add(new Tuple<Chunk, Int3>(chunk, sunlightSources));
                }
            }

            else
            {
                var brightestNeighbour = GetBrightestNeighbour(chunk, relativeIndex);
                if (brightestNeighbour.Item3 > 0)
                {
                    lightSources.Add(new Tuple<Chunk, Int3>(brightestNeighbour.Item1, brightestNeighbour.Item2));
                }
            }


            // propagate lightness
            foreach (var lightSource in lightSources)
            {
                PropagateLightness(lightSource.Item1, lightSource.Item2);
            }
        }

        

        private void AddSolid(Chunk chunk, Int3 relativeIndex, Voxel oldVoxel)
        {
            var resetVoxels = new List<Int3> {relativeIndex};
            if (oldVoxel.Lightness == 15)
            {
                resetVoxels.AddRange(PropagateSunlight(chunk, relativeIndex, 0));
            }

            // propagate darkness
            var lightSources = new List<Tuple<Chunk, Int3>>();
            foreach (var resetVoxel in resetVoxels)
            {
                PropagateDarkness(chunk, resetVoxel, lightSources);
            }

            // propagate lightness
            foreach (var lightSource in lightSources)
            {
                PropagateLightness(lightSource.Item1, lightSource.Item2);
            }
        }

        private List<Int3> PropagateSunlight(Chunk chunk, Int3 relativeIndex, byte toLightness)
        {
            var sunlightSources = new List<Int3>();

            for (var j = relativeIndex.Y - 1; j > 0; j--)
            {
                var voxel = chunk.GetLocalVoxel(relativeIndex.X, j, relativeIndex.Z);
                if (voxel.GetDefinition().IsTransmittance)
                {
                    var resetVoxelIndex = new Int3(relativeIndex.X, j, relativeIndex.Z);
                    chunk.SetLocalVoxel(resetVoxelIndex, new Voxel(voxel.Block, toLightness));
                    sunlightSources.Add(resetVoxelIndex);
                }
                else break;
            }

            return sunlightSources;
        }

        private void PropagateDarkness(Chunk sourceChunk, Int3 sourceIndex, List<Tuple<Chunk, Int3>> lightSources)
        {
            foreach (var direction in Directions)
            {
                var voxel = sourceChunk.GetLocalWithNeighbours(sourceIndex.X + direction.X, sourceIndex.Y + direction.Y,
                    sourceIndex.Z + direction.Z, out var address);

                var definition = voxel.GetDefinition();

                if (voxel.Lightness == 15 || definition.LightEmitting > 0)
                {
                    lightSources.Add(new Tuple<Chunk, Int3>(sourceChunk.Neighbours[address.ChunkIndex],
                        address.RelativeVoxelIndex));
                }
                else if (definition.IsTransmittance && voxel.Lightness > 0)
                {
                    var newChunk = sourceChunk.Neighbours[address.ChunkIndex];
                    newChunk.SetLocalVoxel(address.RelativeVoxelIndex, new Voxel(voxel.Block, 0));

                    PropagateDarkness(newChunk, address.RelativeVoxelIndex, lightSources);
                }
            }
        }

        private void PropagateLightness(Chunk sourceChunk, Int3 sourceIndex)
        {
            var sourceVoxel = sourceChunk.GetLocalVoxel(sourceIndex);

            foreach (var direction in Directions)
            {
                var targetIndex = sourceIndex + direction;
                var targetVoxel = sourceChunk.GetLocalWithNeighbours(targetIndex.X, targetIndex.Y, targetIndex.Z,
                    out var targetAddress);
                var targetDefinition = targetVoxel.GetDefinition();

                if (targetDefinition.IsTransmittance && targetVoxel.Lightness < sourceVoxel.Lightness - 1)
                {
                    var targetChunk = sourceChunk.Neighbours[targetAddress.ChunkIndex];
                    targetChunk.SetLocalVoxel(targetAddress.RelativeVoxelIndex,
                        new Voxel(targetVoxel.Block, (byte)(sourceVoxel.Lightness - 1)));

                    PropagateLightness(targetChunk, targetAddress.RelativeVoxelIndex);
                }
            }
        }
    }
}
