using System.Diagnostics;
using AppleCinnamon.Settings;
using AppleCinnamon.System;

namespace AppleCinnamon.Pipeline
{
    public interface IFullScanner
    {
        DataflowContext<Chunk> Process(DataflowContext<Chunk> context);
    }

    public sealed class FullScanner : IFullScanner
    {
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
                        var flatIndex = E.GetFlatIndex(i, j, k);
                        var voxel = chunk.Voxels[flatIndex];
                        //var definition = VoxelDefinition.DefinitionByType[voxel.Block];


                        var hasVisibilityFlag = voxel.Block > 0 && //!definition.IsSprite &&
                                                voxel.Block != VoxelDefinition.Water.Type;

                        var isTransparent = /*definition.IsTransparent*/ voxel.Block < 16 && voxel.Lightness == 0;

                        if (!hasVisibilityFlag && !isTransparent)
                        {
                            continue;
                        }

                        byte visibilityFlag = 0;
                        var voxelLight = voxel.Lightness;

                        if (j < Chunk.Height - 1) // top
                        {
                            var neighbor = chunk.Voxels[E.GetFlatIndex(i, j + 1, k)];
                            if (hasVisibilityFlag && neighbor.Block < 16)
                            {
                                visibilityFlag += 1;
                                chunk.VoxelCount.Top++;
                            }

                            if (isTransparent && voxelLight < neighbor.Lightness - 1)
                            {
                                voxelLight = (byte)(neighbor.Lightness - 1);
                            }
                        }

                        if (j > 0) // bottom
                        {
                            var neighbor = chunk.Voxels[E.GetFlatIndex(i, j - 1, k)];
                            if (hasVisibilityFlag && neighbor.Block < 16)
                            {
                                visibilityFlag += 2;
                                chunk.VoxelCount.Bottom++;
                            }
                        }

                        if (i > 0) //left
                        {
                            var neighbor = chunk.Voxels[E.GetFlatIndex(i - 1, j, k)];
                            if (hasVisibilityFlag && neighbor.Block < 16)
                            {
                                visibilityFlag += 4;
                                chunk.VoxelCount.Left++;
                            }

                            if (isTransparent && voxelLight < neighbor.Lightness - 1)
                            {
                                voxelLight = (byte)(neighbor.Lightness - 1);
                            }
                        }
                        else if (hasVisibilityFlag) chunk.PendingLeftVoxels.Add(flatIndex);


                        if (i < Chunk.SizeXy - 1) // right
                        {
                            var neighbor = chunk.Voxels[E.GetFlatIndex(i + 1, j, k)];
                            if (hasVisibilityFlag && neighbor.Block < 16)
                            {
                                visibilityFlag += 8;
                                chunk.VoxelCount.Right++;
                            }

                            if (isTransparent && voxelLight < neighbor.Lightness - 1)
                            {
                                voxelLight = (byte)(neighbor.Lightness - 1);
                            }
                        }
                        else if (hasVisibilityFlag) chunk.PendingRightVoxels.Add(flatIndex);

                        if (k > 0) // front
                        {
                            var neighbor = chunk.Voxels[E.GetFlatIndex(i, j, k - 1)];
                            if (hasVisibilityFlag && neighbor.Block < 16)
                            {
                                visibilityFlag += 16;
                                chunk.VoxelCount.Front++;
                            }
                            if (isTransparent && voxelLight < neighbor.Lightness - 1)
                            {
                                voxelLight = (byte)(neighbor.Lightness - 1);
                            }
                        }
                        else if (hasVisibilityFlag) chunk.PendingFrontVoxels.Add(flatIndex);

                        if (k < Chunk.SizeXy - 1) // back
                        {
                            var neighbor = chunk.Voxels[E.GetFlatIndex(i, j, k + 1)];
                            if (hasVisibilityFlag && neighbor.Block < 16)
                            {
                                visibilityFlag += 32;
                                chunk.VoxelCount.Back++;
                            }

                            if (isTransparent && voxelLight < neighbor.Lightness - 1)
                            {
                                voxelLight = (byte)(neighbor.Lightness - 1);
                            }
                        }
                        else if (hasVisibilityFlag) chunk.PendingBackVoxels.Add(flatIndex);


                        if (visibilityFlag > 0)
                        {
                            chunk.VisibilityFlags[flatIndex] = visibilityFlag;
                        }

                        if (voxel.Lightness != voxelLight)
                        {

                            chunk.Voxels[flatIndex] = new Voxel(voxel.Block, voxelLight);
                            chunk.LightPropagationVoxels.Add(flatIndex);
                        }
                    }
                }
            }


            sw.Stop();

            return new DataflowContext<Chunk>(context, chunk, sw.ElapsedMilliseconds, nameof(FullScanner));
        }
    }
}
