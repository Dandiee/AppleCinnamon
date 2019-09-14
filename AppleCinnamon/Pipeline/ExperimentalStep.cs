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

            for (var i = 0; i != Chunk.SizeXy; i++)
            {
                for (var j = 0; j != Chunk.Height; j++)
                {
                    for (var k = 0; k != Chunk.SizeXy; k++)
                    {
                        var index = i + Chunk.SizeXy * (j + Chunk.Height * k);
                        var voxel = chunk.Voxels[index];
                        var isNotTransparent = voxel.Block > 0;

                        if (!isNotTransparent && voxel.Lightness > 0)
                        {
                            continue;
                        }

                        byte visibilityFlag = 0;
                        var voxelLight = voxel.Lightness;

                        if (isNotTransparent)
                        {
                            if (j < Chunk.Height - 1) // top
                            {
                                var neighbor = chunk.Voxels[i + Chunk.SizeXy * (j + 1 + Chunk.Height * k)];
                                if (neighbor.Block == 0)
                                {
                                    visibilityFlag += 1;
                                }
                            }

                            if (j > 0) // bottom
                            {
                                var neighbor = chunk.Voxels[i + Chunk.SizeXy * (j - 1 + Chunk.Height * k)];
                                if (neighbor.Block == 0)
                                {
                                    visibilityFlag += 2;
                                }
                            }
                        }

                        if (i > 0) //left
                        {
                            var neighbor = chunk.Voxels[i - 1 + Chunk.SizeXy * (j + Chunk.Height * k)];
                            if (isNotTransparent)
                            {
                                if (neighbor.Block == 0)
                                {
                                    visibilityFlag += 4;
                                }
                            }
                            else if (neighbor.Lightness == 15 && neighbor.Lightness - 1 > voxelLight)
                            {
                                voxelLight = (byte) (neighbor.Lightness - 1);
                            }
                        }
                        else if (isNotTransparent)
                        {
                            chunk.PendingLeftVoxels.Add(index);
                        }


                        if (i < Chunk.SizeXy - 1) // right
                        {
                            var neighbor = chunk.Voxels[i + 1 + Chunk.SizeXy * (j + Chunk.Height * k)];
                            if (isNotTransparent)
                            {
                                if (neighbor.Block == 0)
                                {
                                    visibilityFlag += 8;
                                }
                            }
                            else if (neighbor.Lightness == 15 && neighbor.Lightness - 1 > voxelLight)
                            {
                                voxelLight = (byte)(neighbor.Lightness - 1);
                            }
                        }
                        else if (isNotTransparent)
                        {
                            chunk.PendingRightVoxels.Add(index);
                        }

                        if (k > 0) // front
                        {
                            var neighbor = chunk.Voxels[i + Chunk.SizeXy * (j + Chunk.Height * (k - 1))];
                            if (isNotTransparent)
                            {
                                if (neighbor.Block == 0)
                                {
                                    visibilityFlag += 16;
                                }
                            }
                            else if (neighbor.Lightness == 15 && neighbor.Lightness - 1 > voxelLight)
                            {
                                voxelLight = (byte)(neighbor.Lightness - 1);
                            }



                        }
                        else if (isNotTransparent)
                        {
                            chunk.PendingFrontVoxels.Add(index);
                        }

                        if (k < Chunk.SizeXy - 1) // back
                        {
                            var neighbor = chunk.Voxels[i + Chunk.SizeXy * (j + Chunk.Height * (k + 1))];
                            if (isNotTransparent)
                            {
                                if (neighbor.Block == 0)
                                {
                                    visibilityFlag += 32;
                                }
                            }
                            else if (neighbor.Lightness == 15 && neighbor.Lightness - 1 > voxelLight)
                            {
                                voxelLight = (byte)(neighbor.Lightness - 1);
                            }
                        }
                        else if (isNotTransparent)
                        {
                            chunk.PendingBackVoxels.Add(index);
                        }

                        if (visibilityFlag > 0)
                        {
                            chunk.VisibilityFlags[index] = visibilityFlag;
                        }

                        if (voxel.Lightness != voxelLight)
                        {
                            chunk.Voxels[index] = new Voxel(voxel.Block, voxelLight);
                            chunk.LightPropagationVoxels.Add(index);
                        }

                    }
                }
            }

            sw.Stop();

            return new DataflowContext<Chunk>(context, chunk, sw.ElapsedMilliseconds, nameof(ExperimentalStep));
        }
    }
}
