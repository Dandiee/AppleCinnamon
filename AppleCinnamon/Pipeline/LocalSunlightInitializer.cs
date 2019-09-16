using System.Diagnostics;
using AppleCinnamon.Settings;

namespace AppleCinnamon.Pipeline
{
    public interface ILocalSunlightInitializer
    {
        DataflowContext<Chunk> Process(DataflowContext<Chunk> context);
    }

    public sealed class LocalSunlightInitializer : ILocalSunlightInitializer
    {
        public DataflowContext<Chunk> Process(DataflowContext<Chunk> context)
        {
            var sw = Stopwatch.StartNew();
            var voxels = context.Payload.Voxels;
            var waterVoxels = context.Payload.TopMostWaterVoxels;

            for (var i = 0; i != Chunk.Size.X; i++)
            {
                for (var k = 0; k != Chunk.Size.Z; k++)
                {

                    var previousBlock = 0;
                    var topMostFound = false;

                    for (var j = Chunk.Size.Y - 1; j > 0; j--)
                    {
                        var index = i + Chunk.SizeXy * (j + Chunk.Height * k);
                        var voxel = voxels[index];

                        if (!topMostFound)
                        {
                            if (voxel.Block == 0)
                            {
                                voxels[index] = new Voxel(voxel.Block, 15);
                            }
                            else
                            {
                                topMostFound = true;
                            }
                        }


                        if (voxel.Block == VoxelDefinition.Water.Type &&
                            previousBlock != VoxelDefinition.Water.Type)
                        {
                            waterVoxels.Add(index);
                        }

                        previousBlock = voxel.Block;
                    }
                }
            }

            sw.Stop();
            return new DataflowContext<Chunk>(context, context.Payload, sw.ElapsedMilliseconds, nameof(LocalSunlightInitializer));
        }
    }
}
