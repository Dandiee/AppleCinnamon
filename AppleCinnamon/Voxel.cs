using System.Runtime.InteropServices;
using AppleCinnamon.Settings;

namespace AppleCinnamon
{
    [StructLayout(LayoutKind.Explicit)]
    public struct Voxel
    {
        public static readonly Voxel One = new Voxel(1, 15);
        public static readonly Voxel Zero = new Voxel(0, 0);
        public static readonly Voxel Invalid = new Voxel(255, 255);
        public static readonly Voxel Air = new Voxel(0, 15);

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
}