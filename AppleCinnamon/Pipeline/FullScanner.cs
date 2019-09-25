using System.Diagnostics;
using System.Runtime.CompilerServices;
using AppleCinnamon.Settings;
using AppleCinnamon.System;

namespace AppleCinnamon.Pipeline
{
    public sealed class FullScanner
    {
        public unsafe DataflowContext<Chunk> Process(DataflowContext<Chunk> context)
        {
           
            var sw = Stopwatch.StartNew();

            var chunk = context.Payload;

            InnerScan(chunk);

            var height = chunk.CurrentHeight;

            for (var i = 0; i != Chunk.SizeXy; i++)
            {
                for (var j = 0; j != height; j++)
                {
                    for (var k = 0; k != Chunk.SizeXy; k++)
                    {
                        var flatIndex = Help.GetFlatIndex(i, j, k, chunk.CurrentHeight);
                        //var voxel =  chunk.Voxels[flatIndex];
                        var voxel = chunk.GetVoxel(flatIndex);


                        var hasVisibilityFlag = voxel.Block > 0 && //!definition.IsSprite &&
                                                voxel.Block != VoxelDefinition.Water.Type;
                        var isTransparent = /*definition.IsTransparent*/ voxel.Block < 16 && voxel.Lightness == 0;

                        if (!hasVisibilityFlag && !isTransparent)
                        {
                            continue;
                        }

                        byte visibilityFlag = 0;
                        var voxelLight = voxel.Lightness;

                        if (j < chunk.CurrentHeight - 1) // top
                        {
                            //var neighbor = chunk.Voxels[Help.GetFlatIndex(i, j + 1, k, chunk.CurrentHeight)];
                            var neighbor = chunk.GetVoxel(Help.GetFlatIndex(i, j + 1, k, chunk.CurrentHeight));

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
                        else if (hasVisibilityFlag)
                        {
                            visibilityFlag += 1;
                            chunk.VoxelCount.Top++;
                        }

                        if (j > 0) // bottom
                        {
                            //var neighbor = chunk.Voxels[Help.GetFlatIndex(i, j - 1, k, chunk.CurrentHeight)];
                            var neighbor = chunk.GetVoxel(Help.GetFlatIndex(i, j - 1, k, chunk.CurrentHeight));

                            if (hasVisibilityFlag && neighbor.Block < 16)
                            {
                                visibilityFlag += 2;
                                chunk.VoxelCount.Bottom++;
                            }
                        }

                        if (i > 0) //left
                        {
                            //var neighbor = chunk.Voxels[Help.GetFlatIndex(i - 1, j, k, chunk.CurrentHeight)];
                            var neighbor = chunk.GetVoxel(Help.GetFlatIndex(i - 1, j, k, chunk.CurrentHeight));

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
                            //var neighbor = chunk.Voxels[Help.GetFlatIndex(i + 1, j, k, chunk.CurrentHeight)];
                            var neighbor = chunk.GetVoxel(Help.GetFlatIndex(i + 1, j, k, chunk.CurrentHeight));

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
                            //var neighbor = chunk.Voxels[Help.GetFlatIndex(i, j, k - 1, chunk.CurrentHeight)];
                            var neighbor = chunk.GetVoxel(Help.GetFlatIndex(i, j, k - 1, chunk.CurrentHeight));

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
                            //var neighbor = chunk.Voxels[Help.GetFlatIndex(i, j, k + 1, chunk.CurrentHeight)];
                            var neighbor = chunk.GetVoxel(Help.GetFlatIndex(i, j, k + 1, chunk.CurrentHeight));

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

                            //chunk.Voxels[flatIndex] = new Voxel(voxel.Block, voxelLight);
                            chunk.SetVoxelNoInline(flatIndex, new Voxel(voxel.Block, voxelLight));
                            chunk.LightPropagationVoxels.Add(flatIndex);
                        }
                    }
                }
            }


            sw.Stop();

            return new DataflowContext<Chunk>(context, chunk, sw.ElapsedMilliseconds, nameof(FullScanner));
        }

        private unsafe void EdgeScan(Chunk chunk)
        {

            // Y Axis
            for (var i = 1; i < chunk.CurrentHeight - 1; i++)
            {
                for (var k = 1; k < Chunk.SizeXy - 1; k++)
                {

                    // When J is 0
                    var flatIndex = Help.GetFlatIndex(i, 0, k, chunk.CurrentHeight);
                    var voxel = chunk.GetVoxel(flatIndex);


                    var hasVisibilityFlag = voxel.Block > 0 && //!definition.IsSprite &&
                                            voxel.Block != VoxelDefinition.Water.Type;
                    var isTransparent = /*definition.IsTransparent*/ voxel.Block < 16 && voxel.Lightness == 0;

                    if (!hasVisibilityFlag && !isTransparent)
                    {
                        continue;
                    }

                    byte visibilityFlag = 0;
                    var voxelLight = voxel.Lightness;

                    InnerSide(chunk, i, 1, k, hasVisibilityFlag, isTransparent, 1, ref visibilityFlag, ref voxelLight);
                    InnerSide(chunk, i - 1, 0, k, hasVisibilityFlag, isTransparent, 4, ref visibilityFlag, ref voxelLight);
                    InnerSide(chunk, i + 1, 0, k, hasVisibilityFlag, isTransparent, 8, ref visibilityFlag, ref voxelLight);
                    InnerSide(chunk, i, 0, k - 1, hasVisibilityFlag, isTransparent, 16, ref visibilityFlag, ref voxelLight);
                    InnerSide(chunk, i, 0, k + 1, hasVisibilityFlag, isTransparent, 32, ref visibilityFlag, ref voxelLight);
                }
            }


            // X Axis
            for (var j = 1; j < chunk.CurrentHeight - 1; j++)
            {
                for (var k = 1; k < Chunk.SizeXy - 1; k++)
                {

                }
            }

            // Z Axis
            for (var i = 1; i < Chunk.SizeXy - 1; i++)
            {
                for (var j = 1; j < chunk.CurrentHeight - 1; j++)
                {

                }
            }
        }

        private unsafe void InnerScan(Chunk chunk)
        {
            for (var i = 1; i < Chunk.SizeXy - 1; i++)
            {
                for (var j = 1; j < chunk.CurrentHeight - 1; j++)
                {
                    for (var k = 1; k < Chunk.SizeXy - 1; k++)
                    {
                        var flatIndex = Help.GetFlatIndex(i, j, k, chunk.CurrentHeight);
                        //var voxel =  chunk.Voxels[flatIndex];
                        var voxel = chunk.GetVoxel(flatIndex);


                        var hasVisibilityFlag = voxel.Block > 0 && voxel.Block != VoxelDefinition.Water.Type;
                        var isTransparent = voxel.Block < 16 && voxel.Lightness == 0;

                        if (!hasVisibilityFlag && !isTransparent)
                        {
                            continue;
                        }

                        byte visibilityFlag = 0;
                        var voxelLight = voxel.Lightness;

                        InnerSide(chunk, i, j + 1, k, hasVisibilityFlag, isTransparent, 1, ref visibilityFlag, ref voxelLight);
                        InnerSide(chunk, i, j - 1, k, hasVisibilityFlag, isTransparent, 2, ref visibilityFlag, ref voxelLight);
                        InnerSide(chunk, i - 1, j, k, hasVisibilityFlag, isTransparent, 4, ref visibilityFlag, ref voxelLight);
                        InnerSide(chunk, i + 1, j, k, hasVisibilityFlag, isTransparent, 8, ref visibilityFlag, ref voxelLight);
                        InnerSide(chunk, i, j, k - 1, hasVisibilityFlag, isTransparent, 16, ref visibilityFlag, ref voxelLight);
                        InnerSide(chunk, i, j, k + 1, hasVisibilityFlag, isTransparent, 32, ref visibilityFlag, ref voxelLight);

                      

                        if (visibilityFlag > 0)
                        {
                            chunk.VisibilityFlags[flatIndex] = visibilityFlag;
                        }

                        if (voxel.Lightness != voxelLight)
                        {
                            chunk.SetVoxelNoInline(flatIndex, new Voxel(voxel.Block, voxelLight));
                            chunk.LightPropagationVoxels.Add(flatIndex);
                        }
                    }
                }
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void InnerSide(Chunk chunk, int i, int j, int k, bool hasVisibilityFlag, bool isTransparent, byte flagStep, ref byte visibilityFlag, ref byte voxelLight)
        {
            //var neighbor = chunk.GetVoxel(Help.GetFlatIndex(i, j, k, chunk.CurrentHeight));
            var neighbor = *((Voxel*) chunk.Handle.Pointer + Help.GetFlatIndex(i, j, k, chunk.CurrentHeight));
            if (hasVisibilityFlag && neighbor.Block < 16)
            {
                visibilityFlag += flagStep;
                chunk.VoxelCount.Left++;
            }

            if (isTransparent && voxelLight < neighbor.Lightness - 1)
            {
                voxelLight = (byte)(neighbor.Lightness - 1);
            }
        }
      
    }

    
}
