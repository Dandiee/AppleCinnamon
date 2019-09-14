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

            for (var i = 0; i != Chunk.Size.X; i++)
            {
                for (var k = 0; k != Chunk.Size.Z; k++)
                {
                    for (var j = Chunk.Size.Y - 1; j > 0; j--)
                    {
                        var index = i + Chunk.SizeXy * (j + Chunk.Height * k);
                        var voxel = voxels[index];
                        var definition = VoxelDefinition.DefinitionByType[voxel.Block];
                        var isTransmittance = definition.IsTransmittance.Y;

                        if (isTransmittance)
                        {
                            voxels[index] = new Voxel(voxel.Block, 15);
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
