using System;
using System.Runtime.InteropServices;
using AppleCinnamon.Helper;
using AppleCinnamon.Settings;

namespace AppleCinnamon
{
    [StructLayout(LayoutKind.Explicit)]
    public struct Voxel
    {
        public static readonly Voxel Bedrock = new(VoxelDefinition.Stone.Type, 15, 0, 0, Face.Top);
        public static readonly Voxel SunBlock = new(0, 15, 0, 0, Face.Top);

        [FieldOffset(0)] public readonly byte Block;
        [FieldOffset(1)] public readonly byte Lightness;    // 4 + 4 bit Sun + Custom light
        [FieldOffset(2)] public readonly byte HueIndex;     // 4 + 3 bit Hue + Orientation

        public readonly byte Sunlight => (byte)(Lightness & 0b00001111);
        public readonly byte CustomLight => (byte)(Lightness >> 4);
        public Face Orientation => (Face)(HueIndex >> 4);

        private Voxel(ref Voxel original, byte light) 
        {
            Block = original.Block;
            Lightness = light;
            HueIndex = original.HueIndex;
        }

        [Obsolete("Do. Not. Use.")]
        public Voxel(byte block, byte sunlight, byte customLight, byte hueIndex, Face face)
        {
            Block = block;
            Lightness = (byte)((customLight << 4) | sunlight);
            HueIndex = hueIndex;
            HueIndex |= (byte)((byte)face << 4);
        }

        public Voxel SetSunlight(byte light) => new(ref this, (byte)((Lightness & 0b11110000) | light));
        public Voxel SetCustomLight(byte light) => new(ref this, (byte)((Lightness & 0b00001111) | (light << 4)));
        public VoxelDefinition GetDefinition() => VoxelDefinition.DefinitionByType[Block];
    }
}