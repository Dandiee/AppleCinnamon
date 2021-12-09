using System;
using System.Collections.Generic;
using AppleCinnamon.Pipeline;
using AppleCinnamon.Settings;
using AppleCinnamon.System;
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
        

        //public static readonly Tuple<Int3, Bool3>[] Directions =
        //{
        //    new(Int3.UnitY, Bool3.UnitY),
        //    new(-Int3.UnitY, Bool3.UnitY),
        //    new(-Int3.UnitX, Bool3.UnitX),
        //    new(Int3.UnitX, Bool3.UnitX),
        //    new(-Int3.UnitZ, Bool3.UnitZ),
        //    new(Int3.UnitZ, Bool3.UnitZ)
        //};

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
            var brightestneighbor = GetBrightestneighbor(chunk, relativeIndex);

            chunk.SetVoxel(relativeIndex.ToFlatIndex(chunk.CurrentHeight),
                new Voxel(newVoxel.Block, Math.Max(brightestneighbor.Item3, newDefinition.LightEmitting)));
            LocalLightPropagationService.InitializeLocalLight(chunk, Help.GetFlatIndex(relativeIndex.X, relativeIndex.Y, relativeIndex.Z, chunk.CurrentHeight));
            //PropagateLightness(new LighntessPropogationRecord(chunk, relativeIndex));
        }

        private void RemoveEmitter(Chunk chunk, Int3 relativeIndex, Voxel oldVoxel)
        {
            var upperVoxelIndex = new Int3(relativeIndex.X, relativeIndex.Y + 1, relativeIndex.Z);
            var upperVoxel = chunk.GetVoxel(upperVoxelIndex.ToFlatIndex(chunk.CurrentHeight));
            var lightSources = new List<Tuple<Chunk, Int3>>();
            PropagateDarkness(new DarknessPropogationRecord(chunk, relativeIndex, lightSources, oldVoxel, VoxelDefinition.DefinitionByType[oldVoxel.Block]));

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
                LocalLightPropagationService.InitializeLocalLight(lightSource.Item1, lightSource.Item2.ToFlatIndex(lightSource.Item1.CurrentHeight));
                //PropagateLightness(new LighntessPropogationRecord(lightSource.Item1, lightSource.Item2));
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
                LocalLightPropagationService.InitializeLocalLight(lightSource.Item1, lightSource.Item2.ToFlatIndex(lightSource.Item1.CurrentHeight));
                //PropagateLightness(new LighntessPropogationRecord(lightSource.Item1, lightSource.Item2));
            }
        }



        private void AddSolid(Chunk chunk, Int3 relativeIndex, Voxel oldVoxel, Voxel newVoxel)
        {
            var resetVoxels = new List<Int3> { relativeIndex };
            if (oldVoxel.Lightness == 15)
            {
                resetVoxels.AddRange(PropagateSunlight(chunk, relativeIndex, 0));
            }

            // propagate darkness
            var lightSources = new List<Tuple<Chunk, Int3>>();
            foreach (var resetVoxel in resetVoxels)
            {
                PropagateDarkness(new DarknessPropogationRecord(chunk, resetVoxel, lightSources, oldVoxel, VoxelDefinition.DefinitionByType[oldVoxel.Block]));
            }

            foreach (var lightSource in lightSources)
            {
                LocalLightPropagationService.InitializeLocalLight(lightSource.Item1, lightSource.Item2.ToFlatIndex(lightSource.Item1.CurrentHeight));
                //PropagateLightness(new LighntessPropogationRecord(lightSource.Item1, lightSource.Item2));
            }
        }

        private List<Int3> PropagateSunlight(Chunk chunk, Int3 relativeIndex, byte toLightness)
        {
            var sunlightSources = new List<Int3>();

            for (var j = relativeIndex.Y - 1; j > 0; j--)
            {
                var voxel = chunk.GetVoxel(Help.GetFlatIndex(relativeIndex.X, j, relativeIndex.Z, chunk.CurrentHeight));
                // TODO: ez nem jó, nem elég csak azt mondani hogy > 0
                if (voxel.GetDefinition().TransmittanceQuarters[(byte)Face.Bottom] > 0)
                {
                    var resetVoxelIndex = new Int3(relativeIndex.X, j, relativeIndex.Z);
                    chunk.SetVoxel(resetVoxelIndex.ToFlatIndex(chunk.CurrentHeight), new Voxel(voxel.Block, toLightness));
                    sunlightSources.Add(resetVoxelIndex);
                }
                else break;
            }

            return sunlightSources;
        }


        public struct DarknessPropogationRecord
        {
            public Chunk sourceChunk;
            public Int3 sourceIndex;
            public List<Tuple<Chunk, Int3>> lightSources;
            public Voxel oldVoxel;
            public VoxelDefinition sourceDefinition;

            public DarknessPropogationRecord(Chunk sourceChunk, Int3 sourceIndex, List<Tuple<Chunk, Int3>> lightSources, Voxel oldVoxel, VoxelDefinition sourceDefinition)
            {
                this.sourceChunk = sourceChunk;
                this.sourceIndex = sourceIndex;
                this.lightSources = lightSources;
                this.oldVoxel = oldVoxel;
                this.sourceDefinition = sourceDefinition;
            }
        }

        private void PropagateDarkness(DarknessPropogationRecord record)
        {
            var queue = new Queue<DarknessPropogationRecord>();
            queue.Enqueue(record);

            while (queue.Count > 0)
            {
                var source = queue.Dequeue();

                foreach (var direction in LightDirections.All)
                {
                    var targetVoxel = source.sourceChunk.GetLocalWithneighbors(source.sourceIndex.X + direction.Step.X, source.sourceIndex.Y + direction.Step.Y, source.sourceIndex.Z + direction.Step.Z, out var targetAddress);
                    var targetDefinition = targetVoxel.GetDefinition();
                    var brightnessLoss = VoxelDefinition.GetBrightnessLoss(record.sourceDefinition, targetDefinition, direction.Direction);
                    if (brightnessLoss > 0)
                    {
                        if (targetVoxel.Lightness >= source.oldVoxel.Lightness)
                        {
                            source.lightSources.Add(new Tuple<Chunk, Int3>(source.sourceChunk.neighbors2[Help.GetChunkFlatIndex(targetAddress.ChunkIndex)], targetAddress.RelativeVoxelIndex));
                        }
                        else if (targetVoxel.Lightness > 0)
                        {
                            var newChunk = source.sourceChunk.neighbors2[Help.GetChunkFlatIndex(targetAddress.ChunkIndex)];
                            newChunk.SetVoxel(targetAddress.RelativeVoxelIndex.ToFlatIndex(newChunk.CurrentHeight), new Voxel(targetVoxel.Block, 0));
                            queue.Enqueue(new DarknessPropogationRecord(newChunk, targetAddress.RelativeVoxelIndex, source.lightSources, targetVoxel, targetDefinition));
                        }
                    }
                }
            }


        }
        
    }

}
