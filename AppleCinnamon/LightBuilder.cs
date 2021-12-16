using System;
using AppleCinnamon.Helper;
using SharpDX;

namespace AppleCinnamon
{
    public sealed class LightUpdater
    {
        public void UpdateLighting(Chunk chunk, Int3 relativeIndex, Voxel oldVoxel, Voxel newVoxel)
        {
            var upperVoxelIndex = new Int3(relativeIndex.X, relativeIndex.Y + 1, relativeIndex.Z);
            
            // globally propagate darkness
            var lightSources = LightingService.GlobalPropagateDarkness(new LightingService.DarknessPropogationRecord(chunk, relativeIndex, newVoxel, 
                newVoxel.GetDefinition(), oldVoxel, oldVoxel.GetDefinition()));

            // locally propagate sunlight vertically
            if (chunk.GetVoxel(upperVoxelIndex.ToFlatIndex(chunk.CurrentHeight)).Sunlight == 15)
            {
                foreach (var sunlightSources in LightingService.Sunlight(chunk, upperVoxelIndex, 15))
                {
                    lightSources.Add(new Tuple<Chunk, Int3>(chunk, sunlightSources));
                }
            }

            // enqueue the new voxel itself - as an emitter it could be a light source as well
            lightSources.Add(new Tuple<Chunk, Int3>(chunk, relativeIndex));

            // globally propagate lightness
            foreach (var lightSource in lightSources)
            {
                LightingService.GlobalPropagate(lightSource.Item1, lightSource.Item2);
            }
        }
    }

}
