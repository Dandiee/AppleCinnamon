using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using AppleCinnamon.Settings;
using AppleCinnamon.System;

namespace AppleCinnamon.Pipeline
{
    public sealed class FullScanner
    {
        public struct FullScanNeighbourIndex
        {
            public readonly int FlatIndex;
            public readonly byte Flag;

            public FullScanNeighbourIndex(int flatIndex, byte flag)
            {
                FlatIndex = flatIndex;
                Flag = flag;
            }
        }

        public struct FullScanIndex
        {
            public readonly int FlatIndex;
            public readonly FullScanNeighbourIndex[] Neighbours;

            public FullScanIndex(int flatIndex, FullScanNeighbourIndex[] neighbours)
            {
                FlatIndex = flatIndex;
                Neighbours = neighbours;
            }
        }

        public static FullScanIndex[] PreBuildIndexes(int sliceIndex)
        {
            var height = (sliceIndex + 1) * Chunk.SliceHeight;
            var indexes = new FullScanIndex[Chunk.SizeXy * height * Chunk.SizeXy];
            var counter = 0;

            for (var i = 0; i < Chunk.SizeXy; i++)
            {
                for (var j = 0; j < height; j++)
                {
                    for (var k = 0; k < Chunk.SizeXy; k++)
                    {
                        var index = Help.GetFlatIndex(i, j, k, height);
                        var neighbours = new List<FullScanNeighbourIndex>(6);

                        if (i > 0)
                        {
                            neighbours.Add(new FullScanNeighbourIndex(Help.GetFlatIndex(i - 1, j, k, height), 4));
                        }

                        if (i < Chunk.SizeXy - 1)
                        {
                            neighbours.Add(new FullScanNeighbourIndex(Help.GetFlatIndex(i + 1, j, k, height), 8));
                        }


                        if (j > 0)
                        {
                            neighbours.Add(new FullScanNeighbourIndex(Help.GetFlatIndex(i, j - 1, k, height), 2));
                        }

                        if (j < height - 1)
                        {
                            neighbours.Add(new FullScanNeighbourIndex(Help.GetFlatIndex(i, j + 1, k, height), 1));
                        }

                       
                        if (k > 0)
                        {
                            neighbours.Add(new FullScanNeighbourIndex(Help.GetFlatIndex(i, j, k - 1, height), 16));
                        }

                        if (k < Chunk.SizeXy - 1)
                        {
                            neighbours.Add(new FullScanNeighbourIndex(Help.GetFlatIndex(i, j, k + 1, height), 32));
                        }

                        indexes[counter++] = new FullScanIndex(index, neighbours.ToArray());
                    }
                }
            }

            return indexes;
        }

        private static readonly Dictionary<int, FullScanIndex[]> IndexesBySlices =
            Enumerable.Range(0, 16).ToDictionary(sliceIndex => sliceIndex, PreBuildIndexes);

        public static readonly Dictionary<int, Dictionary<int, int[]>> PendingVoxelIndexes =
            Enumerable.Range(0, 16).ToDictionary(sliceIndex => sliceIndex, GetPendingVoxelIndexes);

        private static Dictionary<int, int[]> GetPendingVoxelIndexes(int sliceIndex)
        {
            var height = (sliceIndex + 1) * Chunk.SliceHeight;
            var counter = 0;
            var length = Chunk.SizeXy * height;

            var leftIndexes = new int[length];
            var rightIndexes = new int[length];
            var frontIndexes = new int[length];
            var backIndexes = new int[length];

            for (var n = 0; n < Chunk.SizeXy; n++)
            {
                for (var j = 0; j < height; j++)
                {
                    leftIndexes[counter] = Help.GetFlatIndex(0, j, n, height);
                    rightIndexes[counter] = Help.GetFlatIndex(Chunk.SizeXy - 1, j, n, height);
                    frontIndexes[counter] = Help.GetFlatIndex(n, j, 0, height);
                    backIndexes[counter] = Help.GetFlatIndex(n, j, Chunk.SizeXy - 1, height);

                    counter++;
                }
            }

            return new Dictionary<int, int[]>(4)
            {
                [4] = leftIndexes,
                [8] = rightIndexes,
                [16] = frontIndexes,
                [32] = backIndexes,
            };
        }

        private static readonly Dictionary<int, Func<Chunk, List<int>>> PendingVoxelActions =
            new Dictionary<int, Func<Chunk, List<int>>>(4)
            {
                [4] = chunk => chunk.PendingLeftVoxels,
                [8] = chunk => chunk.PendingRightVoxels,
                [16] = chunk => chunk.PendingFrontVoxels,
                [32] = chunk => chunk.PendingBackVoxels,
            };

        public unsafe DataflowContext<Chunk> Process(DataflowContext<Chunk> context)
        {
            var sw = Stopwatch.StartNew();
            var chunk = context.Payload;

            var sliceIndex = chunk.CurrentHeight / Chunk.SliceHeight - 1;
            var indexes = IndexesBySlices[sliceIndex];
            
            foreach (var index in indexes)
            {
                //var voxel = chunk.GetVoxel(index.FlatIndex);
                var voxel = *((Voxel*) chunk.Handle.Pointer + index.FlatIndex);

                var hasVisibilityFlag = voxel.Block > 0 && voxel.Block != VoxelDefinition.Water.Type;
                var isTransparent = voxel.Block < 16 && voxel.Lightness == 0;
            
                if (!hasVisibilityFlag && !isTransparent)
                {
                    continue;
                }
            
                byte visibilityFlag = 0;
                var voxelLight = voxel.Lightness;
            
                foreach (var neighbour in index.Neighbours) 
                {
                    //var neighbourVoxel = chunk.GetVoxel(neighbour.FlatIndex);
                    var neighbourVoxel = *((Voxel*) chunk.Handle.Pointer + neighbour.FlatIndex);

                    if (hasVisibilityFlag && neighbourVoxel.Block < 16)
                    {
                        visibilityFlag += neighbour.Flag;
                      
                        switch (neighbour.Flag)
                        {
                            case 1: chunk.VoxelCount.Top++; break;
                            case 2: chunk.VoxelCount.Bottom++; break;
                            case 4: chunk.VoxelCount.Left++; break;
                            case 8: chunk.VoxelCount.Right++; break;
                            case 16: chunk.VoxelCount.Front++; break;
                            case 32: chunk.VoxelCount.Back++; break;
                        }
                    }
            
                    if (isTransparent && voxelLight < neighbourVoxel.Lightness - 1)
                    {
                        voxelLight = (byte)(neighbourVoxel.Lightness - 1);
                    }
                }
            
                if (visibilityFlag > 0)
                {
                    chunk.VisibilityFlags[index.FlatIndex] = visibilityFlag;
                }
            
                if (voxel.Lightness != voxelLight)
                {
            
                    //chunk.Voxels[flatIndex] = new Voxel(voxel.Block, voxelLight);
                    //chunk.SetVoxel(index.FlatIndex, new Voxel(voxel.Block, voxelLight));
                    chunk.SetVoxelNoInline(index.FlatIndex, new Voxel(voxel.Block, voxelLight));
                    chunk.LightPropagationVoxels.Add(index.FlatIndex);
                }
            }

            var pendingIndexes = PendingVoxelIndexes[sliceIndex];
            foreach (var side in pendingIndexes)
            {
                var list = PendingVoxelActions[side.Key](chunk);
                foreach (var flatIndex in side.Value)
                {
                    var voxel = *((Voxel*)chunk.Handle.Pointer + flatIndex);
                    var hasVisibilityFlag = voxel.Block > 0 && voxel.Block != VoxelDefinition.Water.Type;
                    if (hasVisibilityFlag)
                    {
                        list.Add(flatIndex);
                    }
                }
            }


           // var height = chunk.CurrentHeight;

           // for (var i = 0; i != Chunk.SizeXy; i++)
           // {
           //     for (var j = 0; j != height; j++)
           //     {
           //         for (var k = 0; k != Chunk.SizeXy; k++)
           //         {
           //             var flatIndex = Help.GetFlatIndex(i, j, k, chunk.CurrentHeight);
           //             //var voxel = chunk.GetVoxel(flatIndex);
           //             var voxel = *((Voxel*)chunk.Handle.Pointer + flatIndex);


           //             var hasVisibilityFlag = voxel.Block > 0 && //!definition.IsSprite &&
           //                                     voxel.Block != VoxelDefinition.Water.Type;
           //             var isTransparent = /*definition.IsTransparent*/ voxel.Block < 16 && voxel.Lightness == 0;

           //             if (!hasVisibilityFlag && !isTransparent)
           //             {
           //                 continue;
           //             }

           //             byte visibilityFlag = 0;
           //             var voxelLight = voxel.Lightness;

           //             if (j < chunk.CurrentHeight - 1) // top
           //             {
           //                 var neighbor = *((Voxel*)chunk.Handle.Pointer + Help.GetFlatIndex(i, j + 1, k, chunk.CurrentHeight));
           //                 //var neighbor = chunk.GetVoxel(Help.GetFlatIndex(i, j + 1, k, chunk.CurrentHeight));

           //                 if (hasVisibilityFlag && neighbor.Block < 16)
           //                 {
           //                     visibilityFlag += 1;
           //                     chunk.VoxelCount.Top++;
           //                 }

           //                 if (isTransparent && voxelLight < neighbor.Lightness - 1)
           //                 {
           //                     voxelLight = (byte)(neighbor.Lightness - 1);
           //                 }
           //             }
           //             else if (hasVisibilityFlag)
           //             {
           //                 visibilityFlag += 1;
           //                 chunk.VoxelCount.Top++;
           //             }

           //             if (j > 0) // bottom
           //             {
           //                 var neighbor = *((Voxel*)chunk.Handle.Pointer + Help.GetFlatIndex(i, j - 1, k, chunk.CurrentHeight));
           //                 //var neighbor = chunk.GetVoxel(Help.GetFlatIndex(i, j - 1, k, chunk.CurrentHeight));

           //                 if (hasVisibilityFlag && neighbor.Block < 16)
           //                 {
           //                     visibilityFlag += 2;
           //                     chunk.VoxelCount.Bottom++;
           //                 }
           //             }

           //             if (i > 0) //left
           //             {
           //                 var neighbor = *((Voxel*)chunk.Handle.Pointer + Help.GetFlatIndex(i - 1, j, k, chunk.CurrentHeight));
           //                 //var neighbor = chunk.GetVoxel(Help.GetFlatIndex(i - 1, j, k, chunk.CurrentHeight));

           //                 if (hasVisibilityFlag && neighbor.Block < 16)
           //                 {
           //                     visibilityFlag += 4;
           //                     chunk.VoxelCount.Left++;
           //                 }

           //                 if (isTransparent && voxelLight < neighbor.Lightness - 1)
           //                 {
           //                     voxelLight = (byte)(neighbor.Lightness - 1);
           //                 }
           //             }
           //             else if (hasVisibilityFlag) chunk.PendingLeftVoxels.Add(flatIndex);


           //             if (i < Chunk.SizeXy - 1) // right
           //             {
           //                 var neighbor = *((Voxel*)chunk.Handle.Pointer + Help.GetFlatIndex(i + 1, j, k, chunk.CurrentHeight));
           //                 //var neighbor = chunk.GetVoxel(Help.GetFlatIndex(i + 1, j, k, chunk.CurrentHeight));

           //                 if (hasVisibilityFlag && neighbor.Block < 16)
           //                 {
           //                     visibilityFlag += 8;
           //                     chunk.VoxelCount.Right++;
           //                 }

           //                 if (isTransparent && voxelLight < neighbor.Lightness - 1)
           //                 {
           //                     voxelLight = (byte)(neighbor.Lightness - 1);
           //                 }
           //             }
           //             else if (hasVisibilityFlag) chunk.PendingRightVoxels.Add(flatIndex);

           //             if (k > 0) // front
           //             {
           //                 var neighbor = *((Voxel*)chunk.Handle.Pointer + Help.GetFlatIndex(i, j, k - 1, chunk.CurrentHeight));
           //                 //var neighbor = chunk.GetVoxel(Help.GetFlatIndex(i, j, k - 1, chunk.CurrentHeight));

           //                 if (hasVisibilityFlag && neighbor.Block < 16)
           //                 {
           //                     visibilityFlag += 16;
           //                     chunk.VoxelCount.Front++;
           //                 }

           //                 if (isTransparent && voxelLight < neighbor.Lightness - 1)
           //                 {
           //                     voxelLight = (byte)(neighbor.Lightness - 1);
           //                 }
           //             }
           //             else if (hasVisibilityFlag) chunk.PendingFrontVoxels.Add(flatIndex);

           //             if (k < Chunk.SizeXy - 1) // back
           //             {
           //                 var neighbor = *((Voxel*)chunk.Handle.Pointer + Help.GetFlatIndex(i, j, k + 1, chunk.CurrentHeight));
           //                 //var neighbor = chunk.GetVoxel(Help.GetFlatIndex(i, j, k + 1, chunk.CurrentHeight));

           //                 if (hasVisibilityFlag && neighbor.Block < 16)
           //                 {
           //                     visibilityFlag += 32;
           //                     chunk.VoxelCount.Back++;
           //                 }

           //                 if (isTransparent && voxelLight < neighbor.Lightness - 1)
           //                 {
           //                     voxelLight = (byte)(neighbor.Lightness - 1);
           //                 }
           //             }
           //             else if (hasVisibilityFlag) chunk.PendingBackVoxels.Add(flatIndex);


           //             if (visibilityFlag > 0)
           //             {
           //                 chunk.VisibilityFlags[flatIndex] = visibilityFlag;
           //             }

           //             if (voxel.Lightness != voxelLight)
           //             {

           //                 //chunk.Voxels[flatIndex] = new Voxel(voxel.Block, voxelLight);
           //                 chunk.SetVoxelNoInline(flatIndex, new Voxel(voxel.Block, voxelLight));
           //                 chunk.LightPropagationVoxels.Add(flatIndex);
           //             }
           //         }
           //     }
           // }


            sw.Stop();

            return new DataflowContext<Chunk>(context, chunk, sw.ElapsedMilliseconds, nameof(FullScanner));
        }
    }
}
