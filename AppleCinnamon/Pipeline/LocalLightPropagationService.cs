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

            while (chunk.BuildingContext.LightPropagationVoxels.Count > 0)
            {
                var lightSourceFlatIndex = chunk.BuildingContext.LightPropagationVoxels.Dequeue();

                var voxelLightness = chunk.GetVoxel(lightSourceFlatIndex).Lightness;
                var index = lightSourceFlatIndex.ToIndex(chunk.CurrentHeight);

                foreach (var direction in Directions)
                {
                    var neighborX = index.X + direction.X;
                    if ((neighborX & Chunk.SizeXy) == 0)
                    {
                        var neighborY = index.Y + direction.Y;
                        if (neighborY > 0 && neighborY < chunk.CurrentHeight)
                        {
                            var neighborZ = index.Z + direction.Z;
                            if ((neighborZ & Chunk.SizeXy) == 0)
                            {
                                var neighborFlatIndex = Help.GetFlatIndex(neighborX, neighborY, neighborZ, chunk.CurrentHeight);
                                var neighborVoxel = chunk.GetVoxelNoInline(neighborFlatIndex);
                                
                                if (VoxelDefinition.DefinitionByType[neighborVoxel.Block].IsTransparent && neighborVoxel.Lightness < voxelLightness - 1)
                                {
                                    chunk.SetVoxelNoInline(neighborFlatIndex, new Voxel(neighborVoxel.Block, (byte)(voxelLightness - 1)));
                                    chunk.BuildingContext.LightPropagationVoxels.Enqueue(neighborFlatIndex);
                                }
                            }
                        }
                    }
                }
            }

            sw.Stop();

            return new DataflowContext<Chunk>(context, context.Payload, sw.ElapsedMilliseconds, nameof(LocalLightPropagationService));
        }
    }
}
