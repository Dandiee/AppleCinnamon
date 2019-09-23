using System;
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
            var height = chunk.CurrentHeight;

            var vox = Voxel.Air;
            var ind = 0;
            var vis = false;
            var tra = true;

            var prevIInd = 0;
            var prevIVox = Voxel.Air;
            var prevIVis = false;
            var prevITra = true;

            var prevJInd = 0;
            var prevJVox = Voxel.Air;
            var prevJVis = false;
            var prevJTra = true;

            var prevKInd = 0;
            var prevKVox = Voxel.Air;
            var prevKVis = false;
            var prevKTra = true;

            for (var i = 0; i < Chunk.SizeXy; i++)
            {
                for (var k = 0; k < Chunk.SizeXy; k++)
                {
                    for (var j = 0; j < height; j++)
                    {
                        if (i > 0)
                        {
                            prevIInd = Help.GetFlatIndex(i - 1, j, k, height);
                            prevIVox = chunk.Voxels[prevIInd];
                            prevIVis = prevIVox.Block > 0 && prevIVox.Block != VoxelDefinition.Water.Type;
                            prevITra = prevIVox.Block < 16;
                        }

                        if (j > 0)
                        {
                            prevJInd = ind;
                            prevJVis = vis;
                            prevJTra = tra;
                        }

                        if (k > 0)
                        {
                            prevKInd = Help.GetFlatIndex(i, j, k - 1, height);
                            prevKVox = chunk.Voxels[prevKInd];
                            prevKVis = prevKVox.Block > 0 && prevKVox.Block != VoxelDefinition.Water.Type;
                            prevKTra = prevKVox.Block < 16; // && prevKVox.Lightness == 0;
                        }

                        ind = Help.GetFlatIndex(i, j, k, height);
                        vox = chunk.Voxels[ind];
                        vis = vox.Block > 0 && vox.Block != VoxelDefinition.Water.Type;
                        tra = vox.Block < 16; // && vox.Lightness == 0;

                        // X Axis
                        if (i > 0)
                        {
                            if (vis && prevITra) // there's always previous
                            {
                                chunk.VisibilityFlags.TryGetValue(ind, out var flags);
                                chunk.VisibilityFlags[ind] = (byte)(flags + 4);
                                chunk.VoxelCount.Left++;
                            }
                            if (prevIVis && tra)
                            {
                                chunk.VisibilityFlags.TryGetValue(prevIInd, out var flags);
                                chunk.VisibilityFlags[prevIInd] = (byte)(flags + 8);
                                chunk.VoxelCount.Right++;
                            }
                            else if (i == Chunk.SizeXy - 1 && vis) chunk.PendingRightVoxels.Add(ind);


                            //// light
                            //if (prevITra && tra && Math.Abs(prevIVox.Lightness - vox.Lightness) > 1)
                            //{
                            //    if (prevIVox.Lightness < vox.Lightness)
                            //    {
                            //        chunk.Voxels[prevIInd] = new Voxel(prevIVox.Block, (byte)(vox.Lightness - 1));
                            //        chunk.LightPropagationVoxels.Add(prevIInd);
                            //    }
                            //    else
                            //    {
                            //        chunk.Voxels[ind] = new Voxel(vox.Block, (byte)(prevIVox.Lightness - 1));
                            //        chunk.LightPropagationVoxels.Add(ind);
                            //    }
                            //}
                        }
                        else if (vis) chunk.PendingLeftVoxels.Add(ind);

                        // Y Axis
                        if (j > 0)
                        {
                            if (vis && prevJTra)
                            {
                                chunk.VisibilityFlags.TryGetValue(ind, out var flags);
                                chunk.VisibilityFlags[ind] = (byte)(flags + 2);
                                chunk.VoxelCount.Bottom++;
                            }

                            if (prevJVis && tra)
                            {
                                chunk.VisibilityFlags.TryGetValue(prevJInd, out var flags);
                                chunk.VisibilityFlags[prevJInd] = (byte)(flags + 1);
                                chunk.VoxelCount.Top++;
                            }

                            //// light
                            //if (prevJTra && tra && Math.Abs(prevJVox.Lightness - vox.Lightness) > 1)
                            //{
                            //    if (prevJVox.Lightness < vox.Lightness)
                            //    {
                            //        chunk.Voxels[prevJInd] = new Voxel(prevJVox.Block, (byte)(vox.Lightness - 1));
                            //        chunk.LightPropagationVoxels.Add(prevJInd);
                            //    }
                            //    else
                            //    {
                            //        chunk.Voxels[ind] = new Voxel(vox.Block, (byte)(prevJVox.Lightness - 1));
                            //        chunk.LightPropagationVoxels.Add(ind);
                            //    }
                            //}
                        }

                        // Z Axis
                        if (k > 0)
                        {
                            if (vis && prevKTra)
                            {
                                chunk.VisibilityFlags.TryGetValue(ind, out var flags);
                                chunk.VisibilityFlags[ind] = (byte)(flags + 16);
                                chunk.VoxelCount.Front++;
                            }

                            if (prevKVis && tra)
                            {
                                chunk.VisibilityFlags.TryGetValue(prevKInd, out var flags);
                                chunk.VisibilityFlags[prevKInd] = (byte)(flags + 32);
                                chunk.VoxelCount.Back++;
                            }
                            else if (k == Chunk.SizeXy - 1 && vis) chunk.PendingBackVoxels.Add(ind);


                            //// light
                            //if (prevKTra && tra && Math.Abs(prevKVox.Lightness - vox.Lightness) > 1)
                            //{
                            //    if (prevKVox.Lightness < vox.Lightness)
                            //    {
                            //        chunk.Voxels[prevKInd] = new Voxel(prevKVox.Block, (byte)(vox.Lightness - 1));
                            //        chunk.LightPropagationVoxels.Add(prevKInd);
                            //    }
                            //    else
                            //    {
                            //        chunk.Voxels[ind] = new Voxel(vox.Block, (byte)(prevKVox.Lightness - 1));
                            //        chunk.LightPropagationVoxels.Add(ind);
                            //    }
                            //}
                        }
                        else if (vis) chunk.PendingFrontVoxels.Add(ind);
                    }
                }
            }

            sw.Stop();

            return new DataflowContext<Chunk>(context, chunk, sw.ElapsedMilliseconds, nameof(FullScanner));
        }
    }
}
