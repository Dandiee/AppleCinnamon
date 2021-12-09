using System;
using System.Collections.Generic;
using System.Linq;
using AppleCinnamon.Pipeline;
using AppleCinnamon.System;
using SharpDX;

namespace AppleCinnamon.Settings
{
    public sealed class VoxelDefinition
    {
        private static readonly List<int> RegisteredDefinitions = new();

        private bool Equals(VoxelDefinition other)
        {
            return Type == other.Type;
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is VoxelDefinition other && Equals(other);
        }

        public override int GetHashCode() => Type;


        public static byte[] BrightnessLosses;

        private static byte[] BuildTransmitterPairs()
        {
            var length = DefinitionByType.Length;
            var result = new byte[length * length * 6];

            foreach (var sourceType in RegisteredDefinitions)
            {
                foreach (var targetType in RegisteredDefinitions)
                {
                    var sourceDef = DefinitionByType[sourceType];
                    var targetDef = DefinitionByType[targetType];

                    for (var direction = 0; direction < 6; direction++)
                    {
                        var sourceQuarters = sourceDef.TransmittanceQuarters[direction];
                        var targetQuarters = targetDef.TransmittanceQuarters[direction];

                        var overlap = sourceQuarters & targetQuarters;
                        var overlapCount = HammingWeight((uint)overlap);

                        
                        var flatIndex = sourceDef.Type + length * (targetDef.Type + length * direction);

                        result[flatIndex] = (byte)(overlapCount == 0
                            ? 0
                            : targetDef.DimFactors[direction] + overlapCount - 4);
                    }
                }
            }

            return result;
        }

        private static int HammingWeight(uint v)
        {
            v = v - ((v >> 1) & 0x55555555); // reuse input as temporary
            v = (v & 0x33333333) + ((v >> 2) & 0x33333333); // temp
            var c = ((v + (v >> 4) & 0xF0F0F0F) * 0x1010101) >> 24; // count
            return (int)c;
        }

        public readonly byte Type;
        public readonly Cube<Vector2> Textures;
        public readonly Cube<Int2> TextureIndexes;
        

        // Physics flags
        public readonly bool IsPermeable;
        public readonly bool IsFluid;

        public readonly bool IsSprite;
        public readonly bool IsBlock;
        public readonly bool IsOpaque;
        public readonly bool IsNotBlock;
        public readonly VisibilityFlag CoverFlags;

        public readonly byte LightEmitting;
        public readonly byte[] DimFactors;
        public readonly TransmittanceFlags[] TransmittanceQuarters;

        public static readonly VoxelDefinition[] DefinitionByType = new VoxelDefinition[255];

        public static readonly VoxelDefinition Air =
            new(0, null, null, 0, true, false, false,
                VisibilityFlag.None,
                new byte[] { 1, 1, 1, 1, 1, 1 }, new[]
                {
                    TransmittanceFlags.All, TransmittanceFlags.All, TransmittanceFlags.All, TransmittanceFlags.All,
                    TransmittanceFlags.All, TransmittanceFlags.All
                });

        public static readonly VoxelDefinition Water = new BlockDefinitionBuilder(1).WithAllSideTexture(13, 12)
            .AsFluid()
            .AsPermeable()
            .WithCoverFlags(VisibilityFlag.None)
            .WithDimFactors(2)
            .WithTransmittanceQuarters(TransmittanceFlags.All)
            .Build();

        public static readonly VoxelDefinition Leaves = new BlockDefinitionBuilder(2).WithAllSideTexture(4, 8)
            .WithCoverFlags(VisibilityFlag.None)
            .WithDimFactors(2)
            .WithTransmittanceQuarters(TransmittanceFlags.All)
            .Build();

        public static readonly VoxelDefinition Lava = new BlockDefinitionBuilder(3).WithAllSideTexture(15, 15).AsFluid().Build();


        //public static readonly VoxelDefinition Stone = new BlockDefinitionBuilder(10).WithAllSideTexture(1, 0).Build();
        public static readonly VoxelDefinition Log = new BlockDefinitionBuilder(16).WithAllSideTexture(4, 1).Build();
        public static readonly VoxelDefinition Stone = new BlockDefinitionBuilder(17).WithAllSideTexture(1, 0).Build();
        public static readonly VoxelDefinition Gravel = new BlockDefinitionBuilder(18).WithAllSideTexture(3, 1).Build();
        public static readonly VoxelDefinition CoalOre = new BlockDefinitionBuilder(19).WithAllSideTexture(2, 2).Build();
        public static readonly VoxelDefinition IronOre = new BlockDefinitionBuilder(20).WithAllSideTexture(1, 2).Build();
        public static readonly VoxelDefinition GoldOre = new BlockDefinitionBuilder(21).WithAllSideTexture(0, 2).Build();
        public static readonly VoxelDefinition Dirt = new BlockDefinitionBuilder(22).WithAllSideTexture(2, 0).Build();


        public static readonly VoxelDefinition Grass = new BlockDefinitionBuilder(23).WithBottomTexture(2, 0).WithTopTexture(0, 0).WithSideTexture(3, 0).Build();
        public static readonly VoxelDefinition Snow = new BlockDefinitionBuilder(24).WithBottomTexture(2, 0).WithTopTexture(0, 4).WithSideTexture(4, 4).WithHeight(1f).Build();
        public static readonly VoxelDefinition EmitterStone = new BlockDefinitionBuilder(25).WithAllSideTexture(1, 0).AsPermeable().WithLightEmitting(6).WithSize(.2f, 1, .2f).Build();
        public static readonly VoxelDefinition Sand = new BlockDefinitionBuilder(26).WithAllSideTexture(1, 0).Build();

        //public static readonly VoxelDefinition Snow = new BlockDefinitionBuilder(4).WithAllSideTexture(2, 4).WithSize(1, 0.2f, 1).WithTranslation(0, -0.4f, 0).WithTransmittance(true, true, true).AsPermeable().Build();

        

        public static bool operator ==(VoxelDefinition lhs, VoxelDefinition rhs) => lhs is not null && rhs is not null && lhs.Type == rhs.Type;
        public static bool operator !=(VoxelDefinition lhs, VoxelDefinition rhs) => !(lhs == rhs);

        public VoxelDefinition(byte type, Cube<Vector2> textures, Cube<Int2> textureIndexes, byte lightEmitting, bool isPermeable, bool isSprite, bool isBlock, VisibilityFlag coverFlags, byte[] dimFactors, TransmittanceFlags[] transmittanceQuarters)
        {

            CoverFlags = coverFlags;
            DimFactors = dimFactors;
            TransmittanceQuarters = transmittanceQuarters;
            Type = type;
            Textures = textures;
            TextureIndexes = textureIndexes;
            LightEmitting = lightEmitting;
            IsPermeable = isPermeable;
            DefinitionByType[type] = this;
            IsSprite = isSprite;
            IsBlock = isBlock;
            IsNotBlock = !isBlock;
            IsOpaque = transmittanceQuarters[0] == TransmittanceFlags.None &&
                       transmittanceQuarters[1] == TransmittanceFlags.None &&
                       transmittanceQuarters[2] == TransmittanceFlags.None &&
                       transmittanceQuarters[3] == TransmittanceFlags.None;

            RegisteredDefinitions.Add(type);
            
        }

        static VoxelDefinition()
        {
            BrightnessLosses = BuildTransmitterPairs();
        }

        public static byte GetBrightnessLoss(VoxelDefinition sourceDefinition, VoxelDefinition targetDefinition,
            Face direction)
            => BrightnessLosses[
                sourceDefinition.Type + DefinitionByType.Length *
                (targetDefinition.Type + DefinitionByType.Length * (byte) direction)];
    }


    public sealed class BlockDefinitionBuilder
    {
        private readonly byte _type;
        private byte _lightEmitting;
        private bool _isBlock = true;
        private bool _isFluid = false;
        private bool _isOpaque = true;
        private bool _isPermeable;
        private byte _transmittance;
        private bool _isSprite;
        private Vector3 _size;
        private Vector3 _translation;

        private VisibilityFlag _coverFlags = VisibilityFlag.All;
        private byte[] _dimFactors = { 0, 0, 0, 0, 0, 0 };

        private TransmittanceFlags[] _transmittanceQuarters =
        {
            TransmittanceFlags.None, TransmittanceFlags.None, TransmittanceFlags.None, TransmittanceFlags.None, TransmittanceFlags.None, TransmittanceFlags.None
        };


        private readonly Cube<Vector2> _textures;
        private readonly Cube<Int2> _textureIndexes;

        private float _height = 1;

        public BlockDefinitionBuilder(byte type)
        {
            _type = type;
            _textures = Cube<Vector2>.CreateDefault(() => Vector2.Zero);
            _textureIndexes = Cube<Int2>.CreateDefault(() => Int2.Zero);
            _size = Vector3.One;
        }

        public BlockDefinitionBuilder WithSize(float x, float y, float z)
        {
            _size = new Vector3(x, y, z);

            return this;
        }

        public BlockDefinitionBuilder AsSprite()
        {
            _isSprite = true;
            _isBlock = false;
            return this;
        }

        public BlockDefinitionBuilder WithLightEmitting(byte lightEmitting)
        {
            if (lightEmitting < 1)
            {
                throw new ArgumentException("Light emitting must be at least 1");
            }

            _lightEmitting = lightEmitting;

            return this;
        }

        public BlockDefinitionBuilder AsPermeable()
        {
            _isPermeable = true;
            return this;
        }

        public BlockDefinitionBuilder WithTopTexture(Int2 uv) => WithTopTexture(uv.X, uv.Y);
        public BlockDefinitionBuilder WithTopTexture(int u, int v)
        {
            _textures.SetTop(new Vector2(u / 16f, v / 16f));
            _textureIndexes.SetTop(new Int2(u, v));
            return this;
        }

        public BlockDefinitionBuilder WithBottomTexture(Int2 uv) => WithBottomTexture(uv.X, uv.Y);
        public BlockDefinitionBuilder WithBottomTexture(int u, int v)
        {
            _textures.SetBottom(new Vector2(u / 16f, v / 16f));
            _textureIndexes.SetBottom(new Int2(u, v));
            return this;
        }


        public BlockDefinitionBuilder WithSideTexture(Int2 uv) => WithSideTexture(uv.X, uv.Y);
        public BlockDefinitionBuilder WithSideTexture(int u, int v)
        {
            var coords = new Vector2(u / 16f, v / 16f);
            _textures.SetLeft(coords);
            _textures.SetRight(coords);
            _textures.SetFront(coords);
            _textures.SetBack(coords);


            _textureIndexes.SetLeft(new Int2(u, v));
            _textureIndexes.SetRight(new Int2(u, v));
            _textureIndexes.SetFront(new Int2(u, v));
            _textureIndexes.SetBack(new Int2(u, v));

            return this;
        }

        public BlockDefinitionBuilder WithAllSideTexture(Int2 uv) => WithAllSideTexture(uv.X, uv.Y);
        public BlockDefinitionBuilder WithAllSideTexture(int u, int v)
        {
            var coords = new Vector2(u / 16f, v / 16f);
            _textures.SetLeft(coords);
            _textures.SetRight(coords);
            _textures.SetFront(coords);
            _textures.SetBack(coords);
            _textures.SetTop(coords);
            _textures.SetBottom(coords);


            _textureIndexes.SetLeft(new Int2(u, v));
            _textureIndexes.SetRight(new Int2(u, v));
            _textureIndexes.SetFront(new Int2(u, v));
            _textureIndexes.SetBack(new Int2(u, v));
            _textureIndexes.SetTop(new Int2(u, v));
            _textureIndexes.SetBottom(new Int2(u, v));

            return this;
        }

        public VoxelDefinition Build()
        {
            return new(_type, _textures, _textureIndexes, _lightEmitting,
                _isPermeable, _isSprite, _isBlock, _coverFlags, _dimFactors, _transmittanceQuarters);
        }

        public BlockDefinitionBuilder WithHeight(float f)
        {
            _height = f;
            _size = new Vector3(_size.X, f, _size.Z);
            return this;
        }

        public BlockDefinitionBuilder AsFluid()
        {
            _isBlock = false;
            _isFluid = true;
            return this;
        }

        public BlockDefinitionBuilder WithCoverFlags(VisibilityFlag value)
        {
            _coverFlags = value;
            return this;
        }

        public BlockDefinitionBuilder WithDimFactors(byte value)
        {
            _dimFactors = new[] { value, value, value, value, value, value };
            return this;
        }

        public BlockDefinitionBuilder WithDimFactors(byte[] dimFactors)
        {
            _dimFactors = dimFactors;
            return this;
        }

        public BlockDefinitionBuilder WithTransmittanceQuarters(TransmittanceFlags[] value)
        {
            _transmittanceQuarters = value;
            return this;
        }

        public BlockDefinitionBuilder WithTransmittanceQuarters(TransmittanceFlags value)
        {
            _transmittanceQuarters = new[] { value, value, value, value, value, value };
            return this;
        }
    }

    public static class VoxelDefinitionExtensions
    {
        [InlineMethod.Inline]
        public static bool IsFaceVisible(this VoxelDefinition current, VoxelDefinition neighbor, VisibilityFlag neighborFace)
            => current.IsBlock && (neighbor.CoverFlags & neighborFace) == 0;
    }
}
