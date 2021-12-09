using System.Diagnostics;
using AppleCinnamon.Settings;
using AppleCinnamon.System;
using SharpDX;

namespace AppleCinnamon.Pipeline
{
    public sealed class LocalSunlightInitializer
    {
        public DataflowContext<Chunk> Process(DataflowContext<Chunk> context)
        {
            var sw = Stopwatch.StartNew();
            var chunk = context.Payload;
            var waterVoxels = context.Payload.TopMostWaterVoxels;

            for (var i = 0; i != Chunk.SizeXy; i++)
            {
                for (var k = 0; k != Chunk.SizeXy; k++)
                {
                    var previousBlock = 0;
                    var topMostFound = false;

                    for (var j = chunk.CurrentHeight - 1; j > 0; j--)
                    {
                        var index = new Int3(i, j, k);
                        var flatIndex = index.ToFlatIndex(chunk.CurrentHeight);
                        var voxel = chunk.GetVoxelNoInline(flatIndex);
                        if (!topMostFound)
                        {
                            if (voxel.Block == 0)
                            {
                                chunk.SetVoxel(flatIndex, new Voxel(voxel.Block, 15));
                            }
                            else
                            {
                                topMostFound = true;
                            }
                        }


                        if (voxel.Block == VoxelDefinition.Water.Type &&
                            previousBlock != VoxelDefinition.Water.Type)
                        {
                            waterVoxels.Add(flatIndex);
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
