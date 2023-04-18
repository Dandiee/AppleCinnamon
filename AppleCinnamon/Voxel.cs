using System;
using System.Numerics;
using System.Runtime.InteropServices;
using AppleCinnamon.ChunkBuilder;
using AppleCinnamon.Common;
using AppleCinnamon.Options;
using SharpDX;
using Vector3 = SharpDX.Vector3;

namespace AppleCinnamon;

[StructLayout(LayoutKind.Explicit)]
public struct Voxel
{
    public static readonly Voxel Zero;
    public static readonly Voxel Bedrock = new(VoxelDefinition.Stone.Type, 15, 0, 0, Face.Top);
    public static readonly Voxel SunBlock = new(0, 15, 0, 0, Face.Top);

    [FieldOffset(0)] public readonly byte BlockType;
    [FieldOffset(1)] public readonly byte CompositeLight;    // 4 + 4 bit Sun + Custom sunlight
    [FieldOffset(2)] public readonly byte MetaData;          // 4 + 3 bit Hue + Orientation

    public readonly byte Sunlight => (byte)(CompositeLight & 0b00001111);
    public readonly byte EmittedLight => (byte)(CompositeLight >> 4);
    public Face Orientation => (Face)(MetaData >> 4);

    private Voxel(ref Voxel original, byte light)
    {
        BlockType = original.BlockType;
        CompositeLight = light;
        MetaData = original.MetaData;
    }

    [Obsolete("Do. Not. Use.")]
    public Voxel(byte blockType, byte sunlight, byte emittedLight, byte metaData, Face face)
    {
        BlockType = blockType;
        CompositeLight = (byte)((emittedLight << 4) | sunlight);
        MetaData = metaData;
        MetaData |= (byte)((byte)face << 4);
    }

    public Voxel SetSunlight(byte sunlight) => new(ref this, (byte)((CompositeLight & 0b11110000) | sunlight));
    public Voxel SetCustomLight(byte emittedLight) => new(ref this, (byte)((CompositeLight & 0b00001111) | (emittedLight << 4)));
    public Voxel SetCompositeLight(byte compositeLight) => new(ref this, compositeLight);
    public VoxelDefinition GetDefinition() => VoxelDefinition.DefinitionByType[BlockType];
}