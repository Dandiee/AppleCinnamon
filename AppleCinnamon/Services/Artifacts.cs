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
            CanopyBeveledRectangle,
            CanopyPyramid,
            CanopySphere,
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
                var flatIndex = Help.GetFlatIndex(relativeIndex.X, relativeIndex.Y + j, relativeIndex.Z, chunk.CurrentHeight);
                chunk.Voxels[flatIndex] = new Voxel(treeType.Type, 0);
            }

            canopy(chunk, trunkHeight, relativeIndex + new Int3(0, trunkHeight, 0));
        }
        

        public static void CanopyVanilla(Chunk chunk, int trunkHeight, Int3 trunkTop)
        {
            foreach (var relativeVoxelIndex in ShapeGenerator.Rectangle(trunkTop, 3, 2, true))
            {
                if (_rnd.Next() % LeavesDespawnRate == 0) continue;

                var address = chunk.GetAddress(relativeVoxelIndex);
                address.SetVoxel(chunk, new Voxel(VoxelDefinition.Leaves.Type, 0, 2));
            }

            foreach (var relativeVoxelIndex in ShapeGenerator.Rectangle(trunkTop + new Int3(0, 2, 0), 1, 2, true))
            {
                if (_rnd.Next() % LeavesDespawnRate == 0) continue;

                var address = chunk.GetAddress(relativeVoxelIndex);
                address.SetVoxel(chunk, new Voxel(VoxelDefinition.Leaves.Type, 0, 2));
            }
        }

        public static void CanopyPyramid(Chunk chunk, int trunkHeight, Int3 trunkTop)
        {
            foreach (var relativeVoxelIndex in ShapeGenerator.Pyramid(trunkTop, 3, 4))
            {
                if (_rnd.Next() % LeavesDespawnRate == 0) continue;

                var address = chunk.GetAddress(relativeVoxelIndex);
                address.SetVoxel(chunk, new Voxel(VoxelDefinition.Leaves.Type, 0, 2));
            }
        }

        public static void CanopySphere(Chunk chunk, int trunkHeight, Int3 trunkTop)
        {
            foreach (var relativeVoxelIndex in ShapeGenerator.Sphere(trunkTop, 3))
            {
                if (_rnd.Next() % LeavesDespawnRate == 0) continue;

                var address = chunk.GetAddress(relativeVoxelIndex);
                address.SetVoxel(chunk, new Voxel(VoxelDefinition.Leaves.Type, 0, 2));
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

                var address = chunk.GetAddress(relativeVoxelIndex);
                address.SetVoxel(chunk, new Voxel(VoxelDefinition.Leaves.Type, 0, 2));
            }
        }
    }
}
