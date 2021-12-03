using System;
using AppleCinnamon.System;
using SharpDX;

namespace AppleCinnamon.Settings
{
    public sealed class VoxelDefinition
    {
        private bool Equals(VoxelDefinition other)
        {
            return Type == other.Type;
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is VoxelDefinition other && Equals(other);
        }

        public override int GetHashCode() => Type;

        public readonly byte Type;
        public readonly Cube<Vector2> Textures;
        public readonly Cube<Int2> TextureIndexes;
        public readonly byte LightEmitting;
        public readonly Bool3 IsTransmittance;
        public readonly byte TransmittanceBytes;

        public readonly bool IsTransparent;

        public readonly bool IsPermeable;
        public readonly bool IsSprite;
        public readonly Vector3 Size;
        public readonly Vector3 Translation;
        public readonly bool IsUnitSized;

        public readonly float Height;




        public static readonly VoxelDefinition[] DefinitionByType = new VoxelDefinition[255];

        public static readonly VoxelDefinition Air = new VoxelDefinition(0, null, null, 0, Bool3.True, true, true, Vector3.One, Vector3.Zero, false, 1);
        public static readonly VoxelDefinition Water = new BlockDefinitionBuilder(1).WithAllSideTexture(13, 12).AsSprite().AsPermeable().Build();
        public static readonly VoxelDefinition Leaves = new BlockDefinitionBuilder(2).WithAllSideTexture(15, 0).AsTransparent().Build();
        public static readonly VoxelDefinition Lava = new BlockDefinitionBuilder(3).WithAllSideTexture(15, 15).AsTransparent().Build();
        

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
        public static readonly VoxelDefinition EmitterStone = new BlockDefinitionBuilder(25).WithAllSideTexture(1, 0).AsPermeable().AsTransmittance().WithLightEmitting(6).WithTransmittance(true, true, true).WithSize(.2f, 1, .2f).Build();
        public static readonly VoxelDefinition Sand = new BlockDefinitionBuilder(26).WithAllSideTexture(1, 0).Build();

        //public static readonly VoxelDefinition Snow = new BlockDefinitionBuilder(4).WithAllSideTexture(2, 4).WithSize(1, 0.2f, 1).WithTranslation(0, -0.4f, 0).WithTransmittance(true, true, true).AsPermeable().Build();

        public static bool operator ==(VoxelDefinition lhs, VoxelDefinition rhs) => lhs.Type == rhs.Type;
        public static bool operator !=(VoxelDefinition lhs, VoxelDefinition rhs) => lhs.Type != rhs.Type;

        public VoxelDefinition(byte type, Cube<Vector2> textures, Cube<Int2> textureIndexes, byte lightEmitting, Bool3 isTransmittance, 
            bool isTransparent, bool isPermeable, Vector3 size, Vector3 translation, bool isSprite, float height)
        {
            Type = type;
            Textures = textures;
            TextureIndexes = textureIndexes;
            LightEmitting = lightEmitting;
            IsTransmittance = isTransmittance;
            IsTransparent = isTransparent;
            IsPermeable = isPermeable;
            Size = size;
            DefinitionByType[type] = this;
            IsUnitSized = size == Vector3.One && translation == Vector3.Zero;
            Translation = translation;
            TransmittanceBytes = IsTransmittance.Bytes;
            IsSprite = isSprite;
            Height = height;
        }
    }


    public sealed class BlockDefinitionBuilder
    {
        private readonly byte _type;
        private byte _lightEmitting;
        private Bool3 _isTransmittance;
        private bool _isTransparent;
        private bool _isPermeable;
        private bool _isSprite;
        private Vector3 _size;
        private Vector3 _translation;

        private readonly Cube<Vector2> _textures;
        private readonly Cube<Int2> _textureIndexes;

        private float _height = 1;

        public BlockDefinitionBuilder(byte type)
        {
            _type = type;
            _textures = new Cube<Vector2>();
            _textureIndexes = new Cube<Int2>();
            _size = Vector3.One;
        }

        public BlockDefinitionBuilder WithTransmittance(bool x, bool y, bool z)
        {
            _isTransmittance = new Bool3(x, y, z);
            return this;
        }
        
        public BlockDefinitionBuilder WithTranslation(float x, float y, float z)
        {
            _translation = new Vector3(x, y, z);
            return this;
        }

        public BlockDefinitionBuilder WithSize(float x, float y, float z)
        {
            _size = new Vector3(x, y, z);

            return this;
        }

        public BlockDefinitionBuilder AsSprite()
        {
            _isSprite = true;
            _isTransparent = true;
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

        public BlockDefinitionBuilder AsTransmittance()
        {
            _isTransmittance = Bool3.True;
            return this;
        }

        public BlockDefinitionBuilder AsTransmittance(bool x, bool y, bool z)
        {
            _isTransmittance = new Bool3(x, y, z);
            return this;
        }

        public BlockDefinitionBuilder AsTransparent()
        {
            _isTransparent = true;
            return this;
        }



        public BlockDefinitionBuilder WithTopTexture(Int2 uv) => WithTopTexture(uv.X, uv.Y);
        public BlockDefinitionBuilder WithTopTexture(int u, int v)
        {
            _textures.Top = new Vector2(u / 16f, v / 16f);
            _textureIndexes.Top = new Int2(u, v);
            return this;
        }

        public BlockDefinitionBuilder WithBottomTexture(Int2 uv) => WithBottomTexture(uv.X, uv.Y);
        public BlockDefinitionBuilder WithBottomTexture(int u, int v)
        {
            _textures.Bottom = new Vector2(u / 16f, v / 16f);
            _textureIndexes.Bottom = new Int2(u, v);
            return this;
        }


        public BlockDefinitionBuilder WithSideTexture(Int2 uv) => WithSideTexture(uv.X, uv.Y);
        public BlockDefinitionBuilder WithSideTexture(int u, int v)
        {
            var coords = new Vector2(u / 16f, v / 16f);
            _textures.Left = coords;
            _textures.Right = coords;
            _textures.Front = coords;
            _textures.Back = coords;


            _textureIndexes.Left = new Int2(u, v);
            _textureIndexes.Right = new Int2(u, v);
            _textureIndexes.Front = new Int2(u, v);
            _textureIndexes.Back = new Int2(u, v);

            return this;
        }

        public BlockDefinitionBuilder WithAllSideTexture(Int2 uv) => WithAllSideTexture(uv.X, uv.Y);
        public BlockDefinitionBuilder WithAllSideTexture(int u, int v)
        {
            var coords = new Vector2(u / 16f, v / 16f);
            _textures.Left = coords;
            _textures.Right = coords;
            _textures.Front = coords;
            _textures.Back = coords;
            _textures.Top = coords;
            _textures.Bottom = coords;


            _textureIndexes.Left = new Int2(u, v);
            _textureIndexes.Right = new Int2(u, v);
            _textureIndexes.Front = new Int2(u, v);
            _textureIndexes.Back = new Int2(u, v);
            _textureIndexes.Top = new Int2(u, v);
            _textureIndexes.Bottom = new Int2(u, v);

            return this;
        }

        public VoxelDefinition Build()
        {
            return new VoxelDefinition(_type, _textures, _textureIndexes, _lightEmitting, _isTransmittance, _isTransparent,
                _isPermeable, _size, _translation, _isSprite, _height);
        }

        public BlockDefinitionBuilder WithHeight(float f)
        {
            _height = f;
            _size = new Vector3(_size.X, f, _size.Z);
            return this;
        }
    }
}
