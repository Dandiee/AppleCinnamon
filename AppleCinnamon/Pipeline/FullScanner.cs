using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using AppleCinnamon.Settings;
using AppleCinnamon.System;
using SharpDX.Direct2D1;

namespace AppleCinnamon.Pipeline
{
    public sealed class FullScanner
    {
        [StructLayout(LayoutKind.Explicit)]
        public struct FullScanNeighbourIndex
        {
            [FieldOffset(0)] public readonly int FlatIndex;

            [FieldOffset(4)] public readonly byte Flag;

            public FullScanNeighbourIndex(int flatIndex, byte flag)
            {
                FlatIndex = flatIndex;
                Flag = flag;
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct FullScanIndex
        {
            [FieldOffset(0)] public readonly int FlatIndex;

            [FieldOffset(4)] public readonly FullScanNeighbourIndex[] Neighbours;

            public FullScanIndex(int flatIndex, FullScanNeighbourIndex[] neighbours)
            {
                FlatIndex = flatIndex;
                Neighbours = neighbours;
            }
        }

        public static int[] PreBuildIndexes(int sliceIndex)
        {
            var height = (sliceIndex + 1) * Chunk.SliceHeight;
            var indexes = new int[Chunk.SizeXy * height * Chunk.SizeXy * 7];
            var counter = 0;

            for (var i = 0; i < Chunk.SizeXy; i++)
            {
                for (var j = 0; j < height; j++)
                {
                    for (var k = 0; k < Chunk.SizeXy; k++)
                    {
                        var head = counter * 7;
                        indexes[head] = Help.GetFlatIndex(i, j, k, height);

                        indexes[head + 1] = j < height - 1 ? Help.GetFlatIndex(i, j + 1, k, height) : -1;
                        indexes[head + 2] = j > 0 ? Help.GetFlatIndex(i, j - 1, k, height) : -1;


                        indexes[head + 3] = i > 0 ? Help.GetFlatIndex(i - 1, j, k, height) : -1;
                        indexes[head + 4] = i < Chunk.SizeXy - 1 ? Help.GetFlatIndex(i + 1, j, k, height) : -1;

                        indexes[head + 5] = k > 0 ? Help.GetFlatIndex(i, j, k - 1, height) : -1;
                        indexes[head + 6] = k < Chunk.SizeXy - 1 ? Help.GetFlatIndex(i, j, k + 1, height) : -1;

                        counter++;
                    }
                }
            }

            return indexes;
        }

        private static readonly Dictionary<int, int[]> IndexesBySlices =
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


        public class BlockIndex
        {
            public byte VisibilityFlag;
            public byte VoxelLight;

            public BlockIndex(byte voxelLight)
            {
                VoxelLight = voxelLight;
                VisibilityFlag = 0;
            }
        }


        public unsafe DataflowContext<Chunk> Process(DataflowContext<Chunk> context)
        {
            var sw = Stopwatch.StartNew();
            var chunk = context.Payload;

            var height = chunk.CurrentHeight;

            for (var i = 0; i != Chunk.SizeXy; i++)
            {
                for (var j = 0; j != height; j++)
                {
                    for (var k = 0; k != Chunk.SizeXy; k++)
                    {
                        var flatIndex = Help.GetFlatIndex(i, j, k, chunk.CurrentHeight);
                        //var voxel = chunk.GetVoxel(flatIndex);
      
                        var voxel = chunk.Voxels[flatIndex];


                        var hasVisibilityFlag = voxel.Block > 0 && voxel.Block != VoxelDefinition.Water.Type;
                        var isTransparent = voxel.Block < 16 && voxel.Lightness == 0;

                        if (!hasVisibilityFlag && !isTransparent)
                        {
                            continue;
                        }

                        byte visibilityFlag = 0;
                        var voxelLight = voxel.Lightness;

                        if (j < chunk.CurrentHeight - 1) // top
                        {
                            var neighbor = chunk.Voxels[Help.GetFlatIndex(i, j + 1, k, chunk.CurrentHeight)];
                            if (hasVisibilityFlag)
                            {
                                if (neighbor.Block < 16)
                                {
                                    visibilityFlag += 1;
                                    chunk.VoxelCount.Top.Value++;
                                }
                            }
                            else if (voxelLight < neighbor.Lightness - 1)
                            {
                                if (voxelLight < neighbor.Lightness - 1)
                                {
                                    voxelLight = (byte)(neighbor.Lightness - 1);
                                }
                            }
                        }
                        else if (hasVisibilityFlag)
                        {
                            visibilityFlag += 1;
                            chunk.VoxelCount.Top.Value++;
                        }

                        if (j > 0) // bottom
                        {
                            var neighbor = chunk.Voxels[Help.GetFlatIndex(i, j - 1, k, chunk.CurrentHeight)];
                            if (hasVisibilityFlag && neighbor.Block < 16)
                            {
                                visibilityFlag += 2;
                                chunk.VoxelCount.Bottom.Value++;
                            }
                            else if (voxelLight < neighbor.Lightness - 1)
                            {
                                if (voxelLight < neighbor.Lightness - 1)
                                {
                                    voxelLight = (byte)(neighbor.Lightness - 1);
                                }
                            }
                        }

                        if (i > 0) //left
                        {
                            var neighbor = chunk.Voxels[Help.GetFlatIndex(i - 1, j, k, chunk.CurrentHeight)];
                            if (hasVisibilityFlag)
                            {
                                if (neighbor.Block < 16)
                                {
                                    visibilityFlag += 4;
                                    chunk.VoxelCount.Left.Value++;
                                }
                            }
                            else if (voxelLight < neighbor.Lightness - 1)
                            {
                                voxelLight = (byte)(neighbor.Lightness - 1);
                            }
                        }
                        else if (hasVisibilityFlag) chunk.PendingLeftVoxels.Add(flatIndex);


                        if (i < Chunk.SizeXy - 1) // right
                        {
                            var neighbor = chunk.Voxels[Help.GetFlatIndex(i + 1, j, k, chunk.CurrentHeight)];
                            if (hasVisibilityFlag)
                            {
                                if (neighbor.Block < 16)
                                {
                                    visibilityFlag += 8;
                                    chunk.VoxelCount.Right.Value++;
                                }
                            }
                            else if (voxelLight < neighbor.Lightness - 1)
                            {
                                voxelLight = (byte)(neighbor.Lightness - 1);
                            }
                        }
                        else if (hasVisibilityFlag) chunk.PendingRightVoxels.Add(flatIndex);

                        if (k > 0) // front
                        {
                            var neighbor = chunk.Voxels[Help.GetFlatIndex(i, j, k - 1, chunk.CurrentHeight)];
                            if (hasVisibilityFlag)
                            {
                                if (neighbor.Block < 16)
                                {
                                    visibilityFlag += 16;
                                    chunk.VoxelCount.Front.Value++;
                                }
                            }
                            else if (voxelLight < neighbor.Lightness - 1)
                            {
                                voxelLight = (byte)(neighbor.Lightness - 1);
                            }
                        }
                        else if (hasVisibilityFlag) chunk.PendingFrontVoxels.Add(flatIndex);

                        if (k < Chunk.SizeXy - 1) // back
                        {
                            var neighbor = chunk.Voxels[Help.GetFlatIndex(i, j, k + 1, chunk.CurrentHeight)];
                            if (hasVisibilityFlag)
                            {
                                if (neighbor.Block < 16)
                                {
                                    visibilityFlag += 32;
                                    chunk.VoxelCount.Back.Value++;
                                }
                            }
                            else if (voxelLight < neighbor.Lightness - 1)
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
    }
}

