using System.Runtime.InteropServices;
using AppleCinnamon.Settings;

namespace AppleCinnamon
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Voxel
    {
        public static readonly Voxel One = new Voxel(1, 15);
        public static readonly Voxel Zero = new Voxel(0, 0);

        public byte Block { get; }
        public byte Lightness { get; }

        public VoxelDefinition GetDefinition() => VoxelDefinition.DefinitionByType[Block];

        public Voxel(byte block, byte lightness)
        {
            Block = block;
            Lightness = lightness;
        }
    }
}