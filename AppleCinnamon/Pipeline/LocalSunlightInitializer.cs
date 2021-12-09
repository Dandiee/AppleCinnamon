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

            for (var i = 0; i != Chunk.SizeXy; i++)
            {
                for (var k = 0; k != Chunk.SizeXy; k++)
                {
                    for (var j = chunk.CurrentHeight - 1; j > 0; j--)
                    {
                        var index = new Int3(i, j, k);
                        var flatIndex = index.ToFlatIndex(chunk.CurrentHeight);
                        var voxel = chunk.GetVoxelNoInline(flatIndex);

                        if (voxel.Block == VoxelDefinition.Air.Type)
                        {
                            chunk.SetVoxel(flatIndex, voxel.SetLight(15));
                        }
                        else break;

                    }
                }
            }

            sw.Stop();
            return new DataflowContext<Chunk>(context, context.Payload, sw.ElapsedMilliseconds, nameof(LocalSunlightInitializer));
        }
    }
}
