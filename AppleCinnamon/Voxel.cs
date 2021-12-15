using System.Runtime.InteropServices;
using AppleCinnamon.Helper;
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

        [FieldOffset(2)]
        public readonly byte HueIndex;

        public readonly byte Sunlight =>    (byte)(Lightness & 0b00001111);
        public readonly byte CustomLight => (byte)(Lightness >> 4);

        public Face Orientation => (Face) (HueIndex >> 4);


        public VoxelDefinition GetDefinition() => VoxelDefinition.DefinitionByType[Block];

        public Voxel(byte block, byte lightness)
        {
            Block = block;
            Lightness = lightness;
            HueIndex = 0;
        }

        public Voxel(byte block, byte lightness, byte hueIndex)
        {
            Block = block;
            Lightness = lightness;
            HueIndex = hueIndex;
        }

        public Voxel(byte block, byte sunlight, byte customLight, byte hueIndex)
        {
            Block = block;
            Lightness = (byte)((customLight << 4) | sunlight);
            HueIndex = hueIndex;
        }

        public Voxel(byte block, byte lightness, byte hueIndex, Face orientation)
        {
            Block = block;
            Lightness = lightness;
            HueIndex = hueIndex;
            HueIndex |= (byte) ((byte)orientation << 4);
        }

        public Voxel SetSunlight(byte light)
        {
            // keep the first four bits
            return new(Block, (byte)((Lightness & 0b11110000) | light), HueIndex);
        }

        public Voxel SetCustomLight(byte light)
        {
            // keep last four bits and shift the custom light
            return new(Block, (byte)((Lightness & 0b00001111) | (light << 4)), HueIndex);
        }

        public Voxel SetRawLight(byte light)
        {
            // keep the first four bits
            return new(Block, light, HueIndex);
        }
    }
}