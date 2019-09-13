using System.Collections.Generic;
using System.Diagnostics;
using SharpDX;

namespace AppleCinnamon.Pipeline
{
    public sealed class ExperimentalStep
    {
        public static readonly Int3[] Directions =
        {
            Int3.UnitY, -Int3.UnitY, -Int3.UnitX, Int3.UnitX, -Int3.UnitZ, Int3.UnitZ
        };

        public DataflowContext<Chunk> Process(DataflowContext<Chunk> context)
        {
            var sw = Stopwatch.StartNew();

            var chunk = context.Payload;

            for (var i = 1; i != Chunk.SizeXy - 1; i++)
            {
                for (var j = 1; j != Chunk.Height - 1; j++)
                {
                    for (var k = 1; k != Chunk.SizeXy - 1; k++)
                    {
                        var index = i + Chunk.SizeXy * (j + Chunk.Height * k);
                        var voxel = chunk.Voxels[index];
                        if (voxel.Block > 0)
                        {
                            byte visibilityFlag = 0;

                            var top = chunk.Voxels[i + 1 + Chunk.SizeXy * (j + Chunk.Height * k)];
                            if (top.Block == 0)
                            {
                                visibilityFlag += 1;
                            }

                            var bottom = chunk.Voxels[i - 1 + Chunk.SizeXy * (j + Chunk.Height * k)];
                            if (bottom.Block == 0)
                            {
                                visibilityFlag += 2;
                            }

                            var left = chunk.Voxels[i + Chunk.SizeXy * (j + Chunk.Height * (k - 1))];
                            if (left.Block == 0)
                            {
                                visibilityFlag += 4;
                            }

                            var right = chunk.Voxels[i + Chunk.SizeXy * (j + Chunk.Height * (k + 1))];
                            if (right.Block == 0)
                            {
                                visibilityFlag += 8;
                            }


                            var front = chunk.Voxels[i + 1 + Chunk.SizeXy * (j - 1 + Chunk.Height * k)];
                            if (front.Block == 0)
                            {
                                visibilityFlag += 16;
                            }

                            var back = chunk.Voxels[i + 1 + Chunk.SizeXy * (j + 1 + Chunk.Height * k)];
                            if (back.Block == 0)
                            {
                                visibilityFlag += 32;
                            }

                            if (visibilityFlag > 0)
                            {
                                chunk.VisibilityFlags.Add(index, visibilityFlag);
                            }
                        }
                    }
                }
            }

            sw.Stop();

            return new DataflowContext<Chunk>(context, chunk, sw.ElapsedMilliseconds, nameof(ExperimentalStep));
        }
    }
}
