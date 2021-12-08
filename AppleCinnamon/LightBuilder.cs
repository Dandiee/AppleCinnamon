using System;
using System.Collections.Generic;
using AppleCinnamon.Settings;
using AppleCinnamon.System;
using SharpDX;

namespace AppleCinnamon
{
    public sealed class LightUpdater
    {
        public static readonly Tuple<Int3, Bool3>[] Directions =
        {
            new(Int3.UnitY, Bool3.UnitY),
            new(-Int3.UnitY, Bool3.UnitY),
            new(-Int3.UnitX, Bool3.UnitX),
            new(Int3.UnitX, Bool3.UnitX),
            new(-Int3.UnitZ, Bool3.UnitZ),
            new(Int3.UnitZ, Bool3.UnitZ)
        };

        public void UpdateLighting(Chunk chunk, Int3 relativeIndex, Voxel oldVoxel, Voxel newVoxel)
        {
            var oldDefinition = oldVoxel.GetDefinition();
            var newDefinition = newVoxel.GetDefinition();

            //if (oldDefinition.IsSolidTransparent || newDefinition.IsSolidTransparent )
            //{
            //    chunk.SetVoxel(relativeIndex.ToFlatIndex(chunk.CurrentHeight),
            //        new Voxel(newDefinition.Type, oldVoxel.Lightness));

            //    return;
            //}

            if (newVoxel.Block == 0) // remove
            {
                if (oldDefinition.LightEmitting > 0) // emitter
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

        private Tuple<Chunk, Int3, byte> GetBrightestneighbor(Chunk chunk, Int3 relativeIndex)
        {
            Tuple<Chunk, Int3, byte> brightest = null;

            foreach (var direction in Directions)
            {
                var neighborIndex = direction.Item1 + relativeIndex;
                var voxel = chunk.GetLocalWithneighbors(neighborIndex.X, neighborIndex.Y, neighborIndex.Z, out var address);

                if (brightest == null || brightest.Item3 < voxel.Lightness)
                {
                    //brightest = new Tuple<Chunk, Int3, byte>(chunk.neighbors[address.ChunkIndex],
                    brightest = new Tuple<Chunk, Int3, byte>(chunk.neighbors2[ Help.GetChunkFlatIndex(address.ChunkIndex)],
                        address.RelativeVoxelIndex, voxel.Lightness);
                }
            }

            return brightest;
        }

        private void AddEmitter(Chunk chunk, Int3 relativeIndex, Voxel newVoxel, VoxelDefinition newDefinition)
        {
            var brightestneighbor = GetBrightestneighbor(chunk, relativeIndex);

            chunk.SetVoxel(relativeIndex.ToFlatIndex(chunk.CurrentHeight),
                new Voxel(newVoxel.Block,  Math.Max(brightestneighbor.Item3, newDefinition.LightEmitting)));

            PropagateLightness(chunk, relativeIndex);
        }

        private void RemoveEmitter(Chunk chunk, Int3 relativeIndex, Voxel oldVoxel)
        {
            var upperVoxelIndex = new Int3(relativeIndex.X, relativeIndex.Y + 1, relativeIndex.Z);
            var upperVoxel = chunk.GetVoxel(upperVoxelIndex.ToFlatIndex(chunk.CurrentHeight));
            var lightSources = new List<Tuple<Chunk, Int3>>();
            PropagateDarkness(chunk, relativeIndex, lightSources, oldVoxel.Lightness);

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
            var upperVoxel = chunk.CurrentHeight <= relativeIndex.Y + 1
                             ? Voxel.Air
                             : chunk.GetVoxel(upperIndex.ToFlatIndex(chunk.CurrentHeight));

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
                var brightestneighbor = GetBrightestneighbor(chunk, relativeIndex);
                if (brightestneighbor.Item3 > 0)
                {
                    lightSources.Add(new Tuple<Chunk, Int3>(brightestneighbor.Item1, brightestneighbor.Item2));
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
                PropagateDarkness(chunk, resetVoxel, lightSources, oldVoxel.Lightness);
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
                var voxel = chunk.GetVoxel(Help.GetFlatIndex(relativeIndex.X, j, relativeIndex.Z, chunk.CurrentHeight));
                if (voxel.GetDefinition().IsTransparent)
                {
                    var resetVoxelIndex = new Int3(relativeIndex.X, j, relativeIndex.Z);
                    chunk.SetVoxel(resetVoxelIndex.ToFlatIndex(chunk.CurrentHeight), new Voxel(voxel.Block, toLightness));
                    sunlightSources.Add(resetVoxelIndex);
                }
                else break;
            }

            return sunlightSources;
        }

        private void PropagateDarkness(Chunk sourceChunk, Int3 sourceIndex, List<Tuple<Chunk, Int3>> lightSources, byte originalLightness)
        {
            foreach (var direction in Directions)
            {
                var voxel = sourceChunk.GetLocalWithneighbors(sourceIndex.X + direction.Item1.X, sourceIndex.Y + direction.Item1.Y,
                    sourceIndex.Z + direction.Item1.Z, out var address);

                var definition = voxel.GetDefinition();

                //if ((definition.IsTransmittance.Bytes & direction.Item2.Bytes) > 0)
                if (definition.IsTransparent)
                {
                    //if (voxel.Lightness == 15 || definition.LightEmitting > 0)
                    if (voxel.Lightness >= originalLightness) // voxel.Lightness == 15 || definition.LightEmitting > 0)
                    {
                        lightSources.Add(new Tuple<Chunk, Int3>(
                            sourceChunk.neighbors2[Help.GetChunkFlatIndex(address.ChunkIndex)], address.RelativeVoxelIndex));
                    }
                    else if (
                        //((direction.Item2 & definition.IsTransmittance) == direction.Item2)
                        
                        //definition.IsTransmittance 
                        //&& 
                        voxel.Lightness > 0)
                    {
                        //var newChunk = sourceChunk.neighbors[address.ChunkIndex];
                        var newChunk = sourceChunk.neighbors2[Help.GetChunkFlatIndex(address.ChunkIndex)];
                        newChunk.SetVoxel(address.RelativeVoxelIndex.ToFlatIndex(newChunk.CurrentHeight), new Voxel(voxel.Block, 0));

                        PropagateDarkness(newChunk, address.RelativeVoxelIndex, lightSources, voxel.Lightness);
                    }
                }
            }
        }

        private void PropagateLightness(Chunk sourceChunk, Int3 sourceIndex)
        {
            var sourceVoxel = sourceChunk.CurrentHeight <= sourceIndex.Y
                              ? Voxel.Air
                              : sourceChunk.GetVoxel(sourceIndex.ToFlatIndex(sourceChunk.CurrentHeight));
            var sourceDefinition = VoxelDefinition.DefinitionByType[sourceVoxel.Block];

            foreach (var direction in Directions)
            {
                var targetIndex = sourceIndex + direction.Item1;
                var targetVoxel = sourceChunk.GetLocalWithneighbors(targetIndex.X, targetIndex.Y, targetIndex.Z,
                    out var targetAddress);
                var targetDefinition = targetVoxel.GetDefinition();

                if (
                    //((direction.Item2 & targetDefinition.IsTransmittance) == direction.Item2)
                    // targetDefinition.IsTransmittance 
                    targetDefinition.IsTransparent
                    && targetVoxel.Lightness < (sourceVoxel.Lightness - sourceDefinition.Transmittance))
                {
                    //var targetChunk = sourceChunk.neighbors[targetAddress.ChunkIndex];
                    var targetChunk = sourceChunk.neighbors2[Help.GetChunkFlatIndex(targetAddress.ChunkIndex)];
                    targetChunk.SetVoxel(targetAddress.RelativeVoxelIndex.ToFlatIndex(targetChunk.CurrentHeight),
                        new Voxel(targetVoxel.Block, (byte) ((sourceVoxel.Lightness - sourceDefinition.Transmittance))));

                    PropagateLightness(targetChunk, targetAddress.RelativeVoxelIndex);
                }
            }
        }
    }
}
