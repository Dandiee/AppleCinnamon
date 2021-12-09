using System.Runtime.InteropServices;
using AppleCinnamon.Pipeline;
using AppleCinnamon.Settings;

namespace AppleCinnamon
{
    [StructLayout(LayoutKind.Explicit)]
    public struct Voxel
    {
        public static readonly Voxel One = new(1, 15);
        public static readonly Voxel Zero = new(0, 0);
        public static readonly Voxel Invalid = new(255, 255);
        public static readonly Voxel Air = new(0, 15);

        [FieldOffset(0)]
        public readonly byte Block;

        [FieldOffset(1)]
        public readonly byte Lightness;
        

        public VoxelDefinition GetDefinition() => VoxelDefinition.DefinitionByType[Block];

        public Voxel(byte block, byte lightness)
        {
            Block = block;
            Lightness = lightness;
        }
    }

    //public static class VoxelExtensions
    //{
    //    public static bool IsFaceVisible(this Voxel current, Voxel neighbor, VisibilityFlag neighborFace)
    //        => current.(newDefinition.CoverFlags & direction.OppositeDirection) == 0;
    //}
}