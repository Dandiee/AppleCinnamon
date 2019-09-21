using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using AppleCinnamon.Settings;
using SharpDX;

namespace AppleCinnamon.Pipeline
{
    public interface IFullScanner
    {
        DataflowContext<Chunk> Process(DataflowContext<Chunk> context);
    }

    public sealed class FullScanner : IFullScanner
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



                        // So, in debug mode there's no method inlining which case really serious performance issues
                        // For testing it is just better to use a rolled out, inlined side check with high redundancy

                        //Vertical(chunk, hasVisibilityFlag, isTransparent, 1, i, j + 1, k, ref voxelLight, ref visibilityFlag);
                        //Vertical(chunk, hasVisibilityFlag, isTransparent, 2, i, j - 1, k, ref voxelLight, ref visibilityFlag);
                        //Horizontal(chunk, chunk.PendingLeftVoxels, hasVisibilityFlag, isTransparent, 4, i - 1, j, k, index, ref voxelLight, ref visibilityFlag);
                        //Horizontal(chunk, chunk.PendingRightVoxels, hasVisibilityFlag, isTransparent, 8, i + 1, j, k, index, ref voxelLight, ref visibilityFlag);
                        //Horizontal(chunk, chunk.PendingFrontVoxels, hasVisibilityFlag, isTransparent, 16, i, j, k - 1, index, ref voxelLight, ref visibilityFlag);
                        //Horizontal(chunk, chunk.PendingBackVoxels, hasVisibilityFlag, isTransparent, 32, i, j, k + 1, index, ref voxelLight, ref visibilityFlag);

                        if (j < Chunk.Height - 1) // top
                        {
                            var neighbor = chunk.Voxels[i + Chunk.SizeXy * (j + 1 + Chunk.Height * k)];
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
                            var neighbor = chunk.Voxels[i + Chunk.SizeXy * (j - 1 + Chunk.Height * k)];
                            if (hasVisibilityFlag && neighbor.Block < 16)
                            {
                                visibilityFlag += 2;
                                chunk.VoxelCount.Bottom++;
                            }
                        }

                        if (i > 0) //left
                        {
                            var neighbor = chunk.Voxels[i - 1 + Chunk.SizeXy * (j + Chunk.Height * k)];
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
                        else if (hasVisibilityFlag) chunk.PendingLeftVoxels.Add(index);


                        if (i < Chunk.SizeXy - 1) // right
                        {
                            var neighbor = chunk.Voxels[i + 1 + Chunk.SizeXy * (j + Chunk.Height * k)];
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
                        else if (hasVisibilityFlag) chunk.PendingRightVoxels.Add(index);

                        if (k > 0) // front
                        {
                            var neighbor = chunk.Voxels[i + Chunk.SizeXy * (j + Chunk.Height * (k - 1))];
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
                        else if (hasVisibilityFlag) chunk.PendingFrontVoxels.Add(index);

                        if (k < Chunk.SizeXy - 1) // back
                        {
                            var neighbor = chunk.Voxels[i + Chunk.SizeXy * (j + Chunk.Height * (k + 1))];
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
                        else if (hasVisibilityFlag) chunk.PendingBackVoxels.Add(index);


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

            return new DataflowContext<Chunk>(context, chunk, sw.ElapsedMilliseconds, nameof(FullScanner));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Vertical(Chunk chunk, bool hasVisibilityFlag, bool isTransparent, byte flagIncrease,
            int i, int j, int k, ref byte voxelLight, ref byte visibilityFlag)
        {
            // the current voxel's neighbour is available locally
            if (((ushort)j & 256) == 0)
            {
                UpdateLocalVoxel(chunk, i, j, k, hasVisibilityFlag, isTransparent, flagIncrease, ref visibilityFlag, ref voxelLight);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Horizontal(Chunk chunk, List<int> pendingVoxelSide, bool hasVisibilityFlag, bool isTransparent, byte flagIncrease, int i, int j, int k, int index, ref byte voxelLight, ref byte visibilityFlag)
        {
            // the current voxel's neighbour is available locally
            if ((i & Chunk.SizeXy) == 0 && (k & Chunk.SizeXy) == 0) // back
            {
                UpdateLocalVoxel(chunk, i, j, k, hasVisibilityFlag, isTransparent, flagIncrease, ref visibilityFlag, ref voxelLight);
            }

            // the current voxel may or may not be visible side from the neighbour chunk based on the direction
            // all the other visibility flags will be up-to-date locally, just the pending neighbour one is questionable
            else if (hasVisibilityFlag)
            {
                pendingVoxelSide.Add(index);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateLocalVoxel(Chunk chunk, int i, int j, int k, bool hasVisibilityFlag, bool isTransparent,
            byte flagIncrease, ref byte visibilityFlag, ref byte voxelLight)
        {
            var neighbor = chunk.Voxels[i + Chunk.SizeXy * (j + Chunk.Height * k)];

            // if the block has visibility flags (like dirt, stone, bush, ...) then check transparency of the neighbour
            // if the neighbour is transparent, then the side must be drawn
            // in the case of double-transparent pairs, both sides will be rendered
            if (hasVisibilityFlag &&
                (neighbor.Block == 0 || VoxelDefinition.DefinitionByType[neighbor.Block].IsTransparent))
            {
                visibilityFlag += flagIncrease;
            }


            // if the block is transparent and it has no light effect on it set, we can steal light from the neighbor
            // the light will be propagated in an other step
            // if a light level has been set already, use the brightest source
            if (isTransparent && neighbor.Lightness > 1 && voxelLight < neighbor.Lightness - 1)
            {
                voxelLight = (byte)(neighbor.Lightness - 1);
            }
        }
    }
}
