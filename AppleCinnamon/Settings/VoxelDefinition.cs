﻿using System;
using AppleCinnamon.System;
using SharpDX;

namespace AppleCinnamon.Settings
{
    public sealed class VoxelDefinition
    {
        public byte Type;
        public Cube<Vector2> Textures;
        public byte LightEmitting;
        public Bool3 IsTransmittance;
        public byte TransmittanceBytes;
        public bool IsTransparent;
        public bool IsPermeable;
        public Vector3 Size;
        public Vector3 Translation;
        public bool IsUnitSized;

        public static readonly VoxelDefinition[] DefinitionByType = new VoxelDefinition[255];

        public static readonly VoxelDefinition Air = new VoxelDefinition(0, null, 0, Bool3.True, true, true, Vector3.One, Vector3.Zero);

        public static readonly VoxelDefinition Grass = new BlockDefinitionBuilder(1).WithBottomTexture(2, 0)
            .WithTopTexture(0, 0).WithSideTexture(3, 0).Build();

        public static readonly VoxelDefinition EmitterStone = new BlockDefinitionBuilder(2).WithAllSideTexture(1, 0)
            .AsPermeable().AsTransmittance().WithLightEmitting(6).WithTransmittance(true, true, true)
            .WithSize(.2f, 1, .2f).Build();

        public static readonly VoxelDefinition Sand = new BlockDefinitionBuilder(3).WithAllSideTexture(2, 1)
            .Build();

        public static readonly VoxelDefinition Snow = new BlockDefinitionBuilder(4).WithAllSideTexture(2, 4)
            .WithSize(1, 0.2f, 1).WithTranslation(0, -0.4f, 0).WithTransmittance(true, true, true).AsPermeable().Build();

        public VoxelDefinition(byte type, Cube<Vector2> textures, byte lightEmitting, Bool3 isTransmittance, bool isTransparent, bool isPermeable, Vector3 size, Vector3 translation)
        {
            Type = type;
            Textures = textures;
            LightEmitting = lightEmitting;
            IsTransmittance = isTransmittance;
            IsTransparent = isTransparent;
            IsPermeable = isPermeable;
            Size = size;
            DefinitionByType[type] = this;
            IsUnitSized = size == Vector3.One && translation == Vector3.Zero;
            Translation = translation;
            TransmittanceBytes = IsTransmittance.Bytes;
        }
    }


    public sealed class BlockDefinitionBuilder
    {
        private readonly byte _type;
        private byte _lightEmitting;
        private Bool3 _isTransmittance;
        private bool _isTransparent;
        private bool _isPermeable;
        private Vector3 _size;
        private Vector3 _translation;

        private readonly Cube<Vector2> _textures;

        public BlockDefinitionBuilder(byte type)
        {
            _type = type;
            _textures = new Cube<Vector2>();
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
            return this;
        }

        public BlockDefinitionBuilder WithBottomTexture(Int2 uv) => WithBottomTexture(uv.X, uv.Y);
        public BlockDefinitionBuilder WithBottomTexture(int u, int v)
        {
            _textures.Bottom = new Vector2(u / 16f, v / 16f);
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

            return this;
        }

        public VoxelDefinition Build()
        {
            return new VoxelDefinition(_type, _textures, _lightEmitting, _isTransmittance, _isTransparent,
                _isPermeable, _size, _translation);
        }
    }
}
