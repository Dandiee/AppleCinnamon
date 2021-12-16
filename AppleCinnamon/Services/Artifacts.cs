using System;
using AppleCinnamon.Extensions;
using AppleCinnamon.Helper;
using AppleCinnamon.Settings;
using SharpDX;

namespace AppleCinnamon.Services
{
    public static class Artifacts
    {
        private static Random _rnd = new(54654);

        public const int LeavesDespawnRate = 100;

        public static Action<Chunk, int, Int3>[] CanopyFunctions =
        {
            //CanopyBeveledRectangle,
            //CanopyPyramid,
            //CanopySphere,
            CanopyVanilla
        };

        public static VoxelDefinition[] TreeTypes =
        {
            VoxelDefinition.Wood1,
            VoxelDefinition.Wood2,
            VoxelDefinition.Wood3,
            VoxelDefinition.Wood4,
            VoxelDefinition.Wood5
        };

        public static void Tree(Chunk chunk, Int3 relativeIndex)
        {
            var trunkHeight = _rnd.Next(3, 12);

            var canopy = CanopyFunctions[_rnd.Next(0, CanopyFunctions.Length)];
            var treeType = TreeTypes[_rnd.Next(0, TreeTypes.Length)];

            for (var j = 0; j < trunkHeight; j++)
            {
                chunk.SetSafe(relativeIndex.X, relativeIndex.Y + j, relativeIndex.Z, treeType.Create());

                if (_rnd.Next() % 2 == 0)
                {
                    if (relativeIndex.X < Chunk.SizeXy - 1)
                    {
                        var fifi = chunk.GetFlatIndex(relativeIndex.X + 1, relativeIndex.Y + j, relativeIndex.Z);
                        if (chunk.Voxels[fifi].BlockType == 0)
                        {
                            chunk.SetSafe(fifi, VoxelDefinition.Tendril.Create(2, Face.Left));
                        }
                    }

                    if (relativeIndex.X > 0)
                    {
                        var fifi = chunk.GetFlatIndex(relativeIndex.X - 1, relativeIndex.Y + j, relativeIndex.Z);
                        if (chunk.Voxels[fifi].BlockType == 0)
                        {
                            chunk.SetSafe(fifi, VoxelDefinition.Tendril.Create(2, Face.Right));
                        }
                    }

                    if (relativeIndex.Z < Chunk.SizeXy - 1)
                    {
                        var fifi = chunk.GetFlatIndex(relativeIndex.X, relativeIndex.Y + j, relativeIndex.Z + 1);
                        if (chunk.Voxels[fifi].BlockType == 0)
                        {
                            chunk.SetSafe(fifi, VoxelDefinition.Tendril.Create(2, Face.Front));
                        }
                    }

                    if (relativeIndex.Z > 0)
                    {
                        var fifi = chunk.GetFlatIndex(relativeIndex.X, relativeIndex.Y + j, relativeIndex.Z - 1);
                        if (chunk.Voxels[fifi].BlockType == 0)
                        {
                            chunk.SetSafe(fifi, VoxelDefinition.Tendril.Create(2, Face.Back));
                        }
                    }
                }

            }

            canopy(chunk, trunkHeight, relativeIndex + new Int3(0, trunkHeight, 0));
        }

        public static void GoDown(Chunk chunk, Int3 relativeIndex, Func<Chunk, int, bool> callback)
        {
            var address = chunk.GetAddressChunk(relativeIndex);

            var height = relativeIndex.Y;
            var flatIndex = chunk.GetFlatIndex(address.RelativeVoxelIndex.X, height, address.RelativeVoxelIndex.Z);
            var voxel = address.Chunk.Voxels[flatIndex];
            while (voxel.BlockType == 0)
            {
                if (callback(address.Chunk, flatIndex))
                {
                    height--;
                    flatIndex = address.Chunk.GetFlatIndex(address.RelativeVoxelIndex.X, height, address.RelativeVoxelIndex.Z);
                    voxel = address.Chunk.Voxels[flatIndex];
                }
                else break;
            }
        }

        public static void CanopyVanilla(Chunk chunk, int trunkHeight, Int3 trunkTop)
        {
            foreach (var relativeVoxelIndex in ShapeGenerator.Rectangle(trunkTop, 3, 2, true))
            {
                if (_rnd.Next() % LeavesDespawnRate == 0) continue;

                var address = chunk.GetAddressChunk(relativeVoxelIndex);
                address.SetVoxel(VoxelDefinition.Leaves.Create(2));

                if (relativeVoxelIndex.X == trunkTop.X - 3)
                {
                    GoDown(chunk, relativeVoxelIndex - Int3.UnitX, (c, fi) =>
                    {
                        c.SetSafe(fi, VoxelDefinition.Tendril.Create(2, Face.Right));
                        return _rnd.Next() % 2 == 0;
                    });
                }

                if (relativeVoxelIndex.X == trunkTop.X + 3)
                {
                    GoDown(chunk, relativeVoxelIndex + Int3.UnitX, (c, fi) =>
                    {
                        c.SetSafe(fi, VoxelDefinition.Tendril.Create(2, Face.Left));
                        return _rnd.Next() % 2 == 0;
                    });
                }

                if (relativeVoxelIndex.Z == trunkTop.Z - 3)
                {
                    GoDown(chunk, relativeVoxelIndex - Int3.UnitZ, (c, fi) =>
                    {
                        c.SetSafe(fi, VoxelDefinition.Tendril.Create(2, Face.Back));
                        return _rnd.Next() % 2 == 0;
                    });
                }

                if (relativeVoxelIndex.Z == trunkTop.Z + 3)
                {
                    GoDown(chunk, relativeVoxelIndex + Int3.UnitZ, (c, fi) =>
                    {
                        c.SetSafe(fi, VoxelDefinition.Tendril.Create(2, Face.Front));
                        return _rnd.Next() % 2 == 0;
                    });
                }
            }

            foreach (var relativeVoxelIndex in ShapeGenerator.Rectangle(trunkTop + new Int3(0, 2, 0), 1, 2, true))
            {
                if (_rnd.Next() % LeavesDespawnRate == 0) continue;

                var address = chunk.GetAddressChunk(relativeVoxelIndex);
                address.SetVoxel(VoxelDefinition.Leaves.Create(2));
            }
        }

        public static void CanopyPyramid(Chunk chunk, int trunkHeight, Int3 trunkTop)
        {
            foreach (var relativeVoxelIndex in ShapeGenerator.Pyramid(trunkTop, 3, 4))
            {
                if (_rnd.Next() % LeavesDespawnRate == 0) continue;

                var address = chunk.GetAddressChunk(relativeVoxelIndex);
                address.SetVoxel(VoxelDefinition.Leaves.Create(2));
            }
        }

        public static void CanopySphere(Chunk chunk, int trunkHeight, Int3 trunkTop)
        {
            foreach (var relativeVoxelIndex in ShapeGenerator.Sphere(trunkTop, 4))
            {
                if (_rnd.Next() % LeavesDespawnRate == 0) continue;

                var address = chunk.GetAddressChunk(relativeVoxelIndex);
                address.SetVoxel(VoxelDefinition.Leaves.Create(2));
            }
        }

        public static void CanopyBeveledRectangle(Chunk chunk, int trunkHeight, Int3 trunkTop)
        {
            var radius = 2;
            var minCorner = trunkTop - new Int3(radius);
            var size = new Int3(radius * 2 + 1);

            foreach (var relativeVoxelIndex in ShapeGenerator.RectangleWithBevel(minCorner, size))
            {
                if (_rnd.Next() % LeavesDespawnRate == 0) continue;

                var address = chunk.GetAddressChunk(relativeVoxelIndex);
                address.SetVoxel(VoxelDefinition.Leaves.Create(2));
            }
        }
    }
}
