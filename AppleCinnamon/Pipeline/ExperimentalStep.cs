using System.Collections.Generic;
using System.Diagnostics;
using SharpDX;
using SharpDX.Direct2D1.Effects;

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

            for (var i = 0; i != Chunk.SizeXy; i++)
            {
                for (var j = 0; j != Chunk.Height; j++)
                {
                    for (var k = 0; k != Chunk.SizeXy; k++)
                    {
                        var index = i + Chunk.SizeXy * (j + Chunk.Height * k);
                        var voxel = chunk.Voxels[index];
                        if (voxel.Block > 0)
                        {
                            byte visibilityFlag = 0;

                            if (j < Chunk.Height - 1)
                            {
                                var top = chunk.Voxels[i + Chunk.SizeXy * (j + 1 + Chunk.Height * k)];
                                if (top.Block == 0)
                                {
                                    visibilityFlag += 1;
                                }
                            }

                            if (j > 0)
                            {
                                var bottom = chunk.Voxels[i + Chunk.SizeXy * (j - 1 + Chunk.Height * k)];
                                if (bottom.Block == 0)
                                {
                                    visibilityFlag += 2;
                                }
                            }

                            if (i > 0)
                            {
                                var left = chunk.Voxels[i - 1 + Chunk.SizeXy * (j + Chunk.Height * k)];
                                if (left.Block == 0)
                                {
                                    visibilityFlag += 4;
                                }
                            }
                            else
                            {
                                chunk.PendingLeftVoxels.Add(index);
                            }


                            if (i < Chunk.SizeXy - 1)
                            {
                                var right = chunk.Voxels[i + 1 + Chunk.SizeXy * (j + Chunk.Height * k)];
                                if (right.Block == 0)
                                {
                                    visibilityFlag += 8;
                                }
                            }
                            else
                            {
                                chunk.PendingRightVoxels.Add(index);
                            }

                            if (k > 0)
                            {
                                var front = chunk.Voxels[i + Chunk.SizeXy * (j + Chunk.Height * (k - 1))];
                                if (front.Block == 0)
                                {
                                    visibilityFlag += 16;
                                }
                            }
                            else
                            {
                                chunk.PendingFrontVoxels.Add(index);
                            }

                            if (k < Chunk.SizeXy - 1)
                            {
                                var back = chunk.Voxels[i + Chunk.SizeXy * (j + Chunk.Height * (k + 1))];
                                if (back.Block == 0)
                                {
                                    visibilityFlag += 32;
                                }
                            }
                            else
                            {
                                chunk.PendingBackVoxels.Add(index);
                            }

                            if (visibilityFlag > 0)
                            {
                                chunk.VisibilityFlags[index] = visibilityFlag;
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
