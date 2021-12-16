using System;
using System.Collections.Generic;
using AppleCinnamon.Helper;
using SharpDX;

namespace AppleCinnamon
{
    public sealed class LightUpdater
    {
        public void UpdateLighting(Chunk chunk, Int3 relativeIndex, Voxel oldVoxel, Voxel newVoxel)
        {
            // locally darkening sunlight vertically
            var darknessSources = new Queue<LightingService.DarknessPropogationRecord>();
            var lowerVoxelIndex = new Int3(relativeIndex.X, relativeIndex.Y - 1, relativeIndex.Z);
            if (chunk.GetVoxel(lowerVoxelIndex.ToFlatIndex(chunk.CurrentHeight)).Sunlight == 15)
            {
                foreach (var sunlightRelativeIndex in LightingService.Sunlight(chunk, relativeIndex, 0))
                {
                    var voxel = chunk.GetVoxel(Help.GetFlatIndex(sunlightRelativeIndex, chunk.CurrentHeight));
                    var definition = voxel.GetDefinition();
                    darknessSources.Enqueue(new LightingService.DarknessPropogationRecord(chunk, sunlightRelativeIndex, voxel.SetSunlight(0), 
                        definition, voxel.SetSunlight(15), definition));
                }
            }

            // globally propagate darkness
            darknessSources.Enqueue(new LightingService.DarknessPropogationRecord(chunk, relativeIndex, newVoxel, newVoxel.GetDefinition(), oldVoxel, 
                oldVoxel.GetDefinition()));
            var lightSources = LightingService.GlobalPropagateDarkness(darknessSources);

            // locally propagate sunlight vertically
            var upperVoxelIndex = new Int3(relativeIndex.X, relativeIndex.Y + 1, relativeIndex.Z);
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
