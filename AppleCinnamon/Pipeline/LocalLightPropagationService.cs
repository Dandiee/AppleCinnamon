using System.Collections.Generic;
using System.Diagnostics;
using AppleCinnamon.Settings;
using AppleCinnamon.System;
using SharpDX;

namespace AppleCinnamon.Pipeline
{
    public sealed class LocalLightPropagationService
    {
        public static readonly Int3[] Directions =
        {
            Int3.UnitY, -Int3.UnitY, -Int3.UnitX, Int3.UnitX, -Int3.UnitZ, Int3.UnitZ
        };


        public DataflowContext<Chunk> InitializeLocalLight(DataflowContext<Chunk> context)
        {
            var sw = Stopwatch.StartNew();
            var chunk = context.Payload;

            var lightPropagationVoxels = chunk.BuildingContext.LightPropagationVoxels;

            for (var c = 0; c < lightPropagationVoxels.Count; c++)
            {
                PropagateLightSource(chunk, lightPropagationVoxels[c], lightPropagationVoxels);
            }

            chunk.BuildingContext.LightPropagationVoxels.Clear();
            chunk.BuildingContext.LightPropagationVoxels = null;

            sw.Stop();

            return new DataflowContext<Chunk>(context, context.Payload, sw.ElapsedMilliseconds, nameof(LocalLightPropagationService));
        }


        private void PropagateLightSource(Chunk chunk, int lightSourceFlatIndex, List<int> lightSources)
        {
            var voxelLightness = chunk.GetVoxel(lightSourceFlatIndex).Lightness;
            var index = lightSourceFlatIndex.ToIndex(chunk.CurrentHeight);

            foreach (var direction in Directions)
            {
                var neighbourX = index.X + direction.X;
                if ((neighbourX & Chunk.SizeXy) == 0)
                {
                    var neighbourY = index.Y + direction.Y;
                    if (neighbourY > 0 && neighbourY < chunk.CurrentHeight)
                    {
                        var neighbourZ = index.Z + direction.Z;
                        if ((neighbourZ & Chunk.SizeXy) == 0)
                        {
                            var neighbourIndex = new Int3(neighbourX, neighbourY, neighbourZ);
                            var neighbourFlatIndex = neighbourIndex.ToFlatIndex(chunk.CurrentHeight);
                            var neighbourVoxel = chunk.GetVoxelNoInline(neighbourFlatIndex);
                            var neighborDefinition = VoxelDefinition.DefinitionByType[neighbourVoxel.Block];

                            if (neighborDefinition.IsTransparent && neighbourVoxel.Lightness < voxelLightness - 1)
                            {
                                chunk.SetVoxelNoInline(neighbourFlatIndex, new Voxel(neighbourVoxel.Block, (byte)(voxelLightness - 1)));

                                lightSources.Add(neighbourFlatIndex);
                            }
                        }
                    }
                }
            }
        }
    }
}
