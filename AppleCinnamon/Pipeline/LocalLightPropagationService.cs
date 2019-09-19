using System.Collections.Generic;
using System.Diagnostics;
using AppleCinnamon.Settings;
using SharpDX;

namespace AppleCinnamon.Pipeline
{
    public interface ILocalLightPropagationService
    {
        DataflowContext<Chunk> InitializeLocalLight(DataflowContext<Chunk> context);
    }

    public sealed class LocalLocalLightPropagationService : ILocalLightPropagationService
    {
        public static readonly Int3[] Directions =
        {
            Int3.UnitY, -Int3.UnitY, -Int3.UnitX, Int3.UnitX, -Int3.UnitZ, Int3.UnitZ
        };


        public DataflowContext<Chunk> InitializeLocalLight(DataflowContext<Chunk> context)
        {
            var sw = Stopwatch.StartNew();
            var chunk = context.Payload;

            var lightPropagationVoxels = chunk.LightPropagationVoxels;

            for (var c = 0; c < lightPropagationVoxels.Count; c++)
            {
                PropagateLightSource(chunk, lightPropagationVoxels[c], lightPropagationVoxels);
            }

            chunk.LightPropagationVoxels.Clear();
            chunk.LightPropagationVoxels = null;

            sw.Stop();

            return new DataflowContext<Chunk>(context, context.Payload, sw.ElapsedMilliseconds, nameof(LocalLocalLightPropagationService));
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
                if ((neighbourX & Chunk.SizeXy) == 0)
                {
                    var neighbourY = j + direction.Y;
                    if (((ushort)neighbourY & Chunk.Height) == 0)
                    {
                        var neighbourZ = k + direction.Z;
                        if ((neighbourZ & Chunk.SizeXy) == 0)
                        {
                            var neighbourIndex = neighbourX + Chunk.SizeXy * (neighbourY + Chunk.Height * neighbourZ);
                            var neighbourVoxel = voxels[neighbourIndex];
                            var neighborDefinition = VoxelDefinition.DefinitionByType[neighbourVoxel.Block];

                            if (neighborDefinition.IsTransparent && neighbourVoxel.Lightness < voxelLightness - 1)
                            {
                                voxels[neighbourIndex] = new Voxel(neighbourVoxel.Block, (byte)(voxelLightness - 1));

                                lightSources.Add(neighbourIndex);
                            }
                        }
                    }
                }
            }
        }
    }
}
