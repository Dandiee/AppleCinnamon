using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using AppleCinnamon.Helper;
using AppleCinnamon.Pipeline;
using AppleCinnamon.Settings;
using AppleCinnamon.Vertices;
using SharpDX;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

namespace AppleCinnamon
{
    public sealed class ChunkBuilder
    {
        private readonly Device _device;
        private static readonly Vector2[] WaterUvOffsets = { Vector2.Zero, new(1, 0), new(1, 1 / 32f), new(0, 1 / 32f) };

        public ChunkBuilder(Device device)
        {
            _device = device;
        }

        public void BuildChunk(Chunk chunk)
        {
            var faces = GetChunkFaces(chunk);
            var visibleFacesCount = chunk.BuildingContext.Faces.Sum(s => s.VoxelCount);
            if (visibleFacesCount == 0)
            {
                return;
            }

            var vertices = new VertexSolidBlock[visibleFacesCount * 4];

            var indexes = new uint[visibleFacesCount * 6];

            foreach (var visibilityFlag in chunk.BuildingContext.VisibilityFlags)
            {
                var flatIndex = visibilityFlag.Key;
                var index = flatIndex.ToIndex(chunk.CurrentHeight);

                var voxel = chunk.GetVoxel(flatIndex);
                var definition = VoxelDefinition.DefinitionByType[voxel.Block];

                var voxelPositionOffset = definition.Offset + chunk.OffsetVector + new Vector3(index.X, index.Y, index.Z);


                foreach (var faceInfo in faces.Faces)
                {
                    if (((byte)visibilityFlag.Value & faceInfo.BuildInfo.DirectionFlag) == faceInfo.BuildInfo.DirectionFlag)
                    {
                        var neighbor = chunk.GetLocalWithneighbors(index.X + faceInfo.BuildInfo.Direction.X, index.Y + faceInfo.BuildInfo.Direction.Y, index.Z + faceInfo.BuildInfo.Direction.Z);
                        AddFace(faceInfo, voxel, index.X, index.Y, index.Z, vertices, indexes, definition, chunk, neighbor, voxelPositionOffset);
                    }
                }
            }

            chunk.ChunkBuffer = new ChunkBuffer(_device, vertices, indexes, faces);

            chunk.VisibleFacesCount = visibleFacesCount;
            var waterBuffer = AddWaterFace(chunk, _device);
            var spriteBuffer = AddSpriteFace(chunk, _device);

            chunk.SetBuffers(waterBuffer, spriteBuffer);
        }

        private FaceBuffer AddWaterFace(Chunk chunk, Device device)
        {
            if (chunk.TopMostWaterVoxels.Count == 0)
            {
                return null;
            }

            var topOffsetVertices = FaceBuildInfo.FaceVertices.Top;
            var vertices = new VertexWater[chunk.TopMostWaterVoxels.Count * 4];
            var indexes = new uint[chunk.TopMostWaterVoxels.Count * 6 * 2];

            for (var n = 0; n < chunk.TopMostWaterVoxels.Count; n++)
            {
                var flatIndex = chunk.TopMostWaterVoxels[n];
                var index = flatIndex.ToIndex(chunk.CurrentHeight);

                var vertexOffset = n * 4;
                var positionOffset = new Vector3(index.X, index.Y - 0.1f, index.Z);
                var light = chunk.GetVoxel(flatIndex).Lightness;

                for (var m = 0; m < topOffsetVertices.Length; m++)
                {
                    var position = topOffsetVertices[m] + chunk.OffsetVector + positionOffset;
                    var textureUv = WaterUvOffsets[m];

                    vertices[vertexOffset + m] =
                        new VertexWater(position, textureUv, 0, light);
                }

                var indexOffset = n * 6 * 2;

                indexes[indexOffset + 0] = (uint)(vertexOffset + 0);
                indexes[indexOffset + 1] = (uint)(vertexOffset + 2);
                indexes[indexOffset + 2] = (uint)(vertexOffset + 3);
                indexes[indexOffset + 3] = (uint)(vertexOffset + 0);
                indexes[indexOffset + 4] = (uint)(vertexOffset + 1);
                indexes[indexOffset + 5] = (uint)(vertexOffset + 2);

                indexes[indexOffset + 6] = (uint)(vertexOffset + 2);
                indexes[indexOffset + 7] = (uint)(vertexOffset + 1);
                indexes[indexOffset + 8] = (uint)(vertexOffset + 0);
                indexes[indexOffset + 9] = (uint)(vertexOffset + 3);
                indexes[indexOffset + 10] = (uint)(vertexOffset + 2);
                indexes[indexOffset + 11] = (uint)(vertexOffset + 0);
            }

            return new FaceBuffer(
                indexes.Length,
                default(VertexWater).Size,
                Buffer.Create(device, BindFlags.VertexBuffer, vertices),
                Buffer.Create(device, BindFlags.IndexBuffer, indexes));
        }

        private FaceBuffer AddSpriteFace(Chunk chunk, Device device)
        {
            if (chunk.BuildingContext.SpriteBlocks.Count == 0) return null;
            var vertices = new VertexSprite[chunk.BuildingContext.SpriteBlocks.Count * 4 * 2];
            var indexes = new uint[chunk.BuildingContext.SpriteBlocks.Count * 6 * 2 * 2];

            var secondFaceOffset = chunk.BuildingContext.SpriteBlocks.Count;

            for (var n = 0; n < chunk.BuildingContext.SpriteBlocks.Count; n++)
            {
                var flatIndex = chunk.BuildingContext.SpriteBlocks[n];
                var index = flatIndex.ToIndex(chunk.CurrentHeight);

                var vertexOffset = n * 4;
                var positionOffset = new Vector3(index.X, index.Y, index.Z);
                var voxel = chunk.GetVoxel(flatIndex);
                var definition = VoxelDefinition.DefinitionByType[voxel.Block];

                AddSpriteFace(chunk, FaceBuildInfo.SpriteVertices.Left, positionOffset, voxel,
                    definition.TextureIndexes.Faces[(byte)Face.Left], vertices, indexes, vertexOffset, n, 0);

                AddSpriteFace(chunk, FaceBuildInfo.SpriteVertices.Right, positionOffset, voxel,
                    definition.TextureIndexes.Faces[(byte)Face.Right], vertices, indexes, vertexOffset, n, secondFaceOffset);
            }

            //return null;
            return new FaceBuffer(indexes.Length, default(VertexSprite).Size, Buffer.Create(device, BindFlags.VertexBuffer, vertices), Buffer.Create(device, BindFlags.IndexBuffer, indexes));
        }

        private static void AddSpriteFace(Chunk chunk, Vector3[] faceOffsetVertices, Vector3 positionOffset, Voxel voxel, Int2 textureIndicies,
            VertexSprite[] vertices, uint[] indexes, int vertexOffset, int vertexIndex, int faceOffset)
        {
            for (var m = 0; m < faceOffsetVertices.Length; m++)
            {
                var position = faceOffsetVertices[m] + chunk.OffsetVector + positionOffset;
                var textureOffset = FaceBuildInfo.UvOffsetIndexes[m];
                vertices[vertexOffset + m + faceOffset * 4] = new VertexSprite(position, textureIndicies.X + textureOffset.X, textureIndicies.Y + textureOffset.Y, voxel.Lightness, voxel.HueIndex);
            }

            var indexOffset = (vertexIndex * 6 * 2) + (faceOffset * 6 * 2);

            indexes[indexOffset + 0] = (uint)(vertexOffset + 0 + faceOffset * 4);
            indexes[indexOffset + 1] = (uint)(vertexOffset + 2 + faceOffset * 4);
            indexes[indexOffset + 2] = (uint)(vertexOffset + 3 + faceOffset * 4);
            indexes[indexOffset + 3] = (uint)(vertexOffset + 0 + faceOffset * 4);
            indexes[indexOffset + 4] = (uint)(vertexOffset + 1 + faceOffset * 4);
            indexes[indexOffset + 5] = (uint)(vertexOffset + 2 + faceOffset * 4);

            indexes[indexOffset + 6] = (uint)(vertexOffset + 2 + faceOffset * 4);
            indexes[indexOffset + 7] = (uint)(vertexOffset + 1 + faceOffset * 4);
            indexes[indexOffset + 8] = (uint)(vertexOffset + 0 + faceOffset * 4);
            indexes[indexOffset + 9] = (uint)(vertexOffset + 3 + faceOffset * 4);
            indexes[indexOffset + 10] = (uint)(vertexOffset + 2 + faceOffset * 4);
            indexes[indexOffset + 11] = (uint)(vertexOffset + 0 + faceOffset * 4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddFace(ChunkFace face, Voxel voxel, int relativeIndexX, int relativeIndexY, int relativeIndexZ, VertexSolidBlock[] vertices,
            uint[] indexes, VoxelDefinition definition, Chunk chunk, Voxel neighbor, Vector3 voxelPositionOffset)
        {
            // Face specific base variables
            var textureUv = definition.TextureIndexes.Faces[(byte)face.BuildInfo.Face];
            var offset = face.Offset + face.ProcessedVoxels;
            var vertexIndex = offset * 4;
            var indexIndex = offset * 6;

            

            // Visit all ambient neighbors
            foreach (var vertexInfo in face.BuildInfo.VerticesInfo)
            {
                var position = new Vector3(
                    vertexInfo.Position.X * definition.Size.X + voxelPositionOffset.X,
                    vertexInfo.Position.Y * definition.Size.Y + voxelPositionOffset.Y,
                    vertexInfo.Position.Z * definition.Size.Z + voxelPositionOffset.Z);

                byte totalNeighborLight = 0;
                var numberOfAmbientNeighbors = 0;

                foreach (var ambientIndex in vertexInfo.AmbientOcclusionNeighbors)
                {
                    var ambientNeighborVoxel = chunk.GetLocalWithneighbors(relativeIndexX + ambientIndex.X, relativeIndexY + ambientIndex.Y, relativeIndexZ + ambientIndex.Z, out var addr);
                    var ambientNeighborDefinition = VoxelDefinition.DefinitionByType[ambientNeighborVoxel.Block];

                    if (!ambientNeighborDefinition.IsBlock)
                    {
                        totalNeighborLight += ambientNeighborVoxel.Lightness;
                    }
                    else if (ambientNeighborDefinition.IsUnitSized)
                    {
                        numberOfAmbientNeighbors++;
                    }
                }

                var hue = (definition.HueFaces & face.Direction) == face.Direction ? voxel.HueIndex : (byte)0;

                    vertices[vertexIndex + vertexInfo.Index] = new VertexSolidBlock(position, textureUv.X + vertexInfo.TextureIndex.X,
                        textureUv.Y + vertexInfo.TextureIndex.Y, neighbor.Lightness, totalNeighborLight,
                        numberOfAmbientNeighbors, hue);
            }

            indexes[indexIndex] = (uint)vertexIndex;
            indexes[indexIndex + 1] = (uint)(vertexIndex + 2);
            indexes[indexIndex + 2] = (uint)(vertexIndex + 3);
            indexes[indexIndex + 3] = (uint)(vertexIndex + 0);
            indexes[indexIndex + 4] = (uint)(vertexIndex + 1);
            indexes[indexIndex + 5] = (uint)(vertexIndex + 2);

            face.ProcessedVoxels++;
        }

        private Cube<ChunkFace> GetChunkFaces(Chunk chunk)
        {
            var topCount = chunk.BuildingContext.Top.VoxelCount;
            var botCount = chunk.BuildingContext.Bottom.VoxelCount;
            var lefCount = chunk.BuildingContext.Left.VoxelCount;
            var rigCount = chunk.BuildingContext.Right.VoxelCount;
            var froCount = chunk.BuildingContext.Front.VoxelCount;
            var bacCount = chunk.BuildingContext.Back.VoxelCount;

            var topOffset = 0;
            var botOffset = topCount;
            var lefOffset = botOffset + botCount;
            var rigOffset = lefOffset + lefCount;
            var froOffset = rigOffset + rigCount;
            var bacOffset = froOffset + froCount;

            var result = new Cube<ChunkFace>(
                new ChunkFace(topOffset, topCount, FaceBuildInfo.Top, VisibilityFlag.Top),
                new ChunkFace(botOffset, botCount, FaceBuildInfo.Bottom, VisibilityFlag.Bottom),
                new ChunkFace(lefOffset, lefCount, FaceBuildInfo.Left, VisibilityFlag.Left),
                new ChunkFace(rigOffset, rigCount, FaceBuildInfo.Right, VisibilityFlag.Right),
                new ChunkFace(froOffset, froCount, FaceBuildInfo.Front, VisibilityFlag.Front),
                new ChunkFace(bacOffset, bacCount, FaceBuildInfo.Back, VisibilityFlag.Back));

            return result;
        }


        private Cube<ChunkFace> GetSolidTransparentChunkFaces(Chunk chunk)
        {
            var voxelCount = chunk.BuildingContext.TransparentBlocks.Count;

            var topOffset = 0;
            var botOffset = voxelCount;
            var lefOffset = botOffset + voxelCount;
            var rigOffset = lefOffset + voxelCount;
            var froOffset = rigOffset + voxelCount;
            var bacOffset = froOffset + voxelCount;

            var result = new Cube<ChunkFace>(
                new ChunkFace(topOffset, voxelCount, FaceBuildInfo.Top, VisibilityFlag.Top),
                new ChunkFace(botOffset, voxelCount, FaceBuildInfo.Bottom, VisibilityFlag.Bottom),
                new ChunkFace(lefOffset, voxelCount, FaceBuildInfo.Left, VisibilityFlag.Left),
                new ChunkFace(rigOffset, voxelCount, FaceBuildInfo.Right, VisibilityFlag.Right),
                new ChunkFace(froOffset, voxelCount, FaceBuildInfo.Front, VisibilityFlag.Front),
                new ChunkFace(bacOffset, voxelCount, FaceBuildInfo.Back, VisibilityFlag.Back));

            return result;
        }
    }

    public sealed class FaceBuildInfo
    {
        public static readonly Vector3 TopLefFro = new(-.5f, +.5f, -.5f);
        public static readonly Vector3 TopRigFro = new(+.5f, +.5f, -.5f);
        public static readonly Vector3 TopLefBac = new(-.5f, +.5f, +.5f);
        public static readonly Vector3 TopRigBac = new(+.5f, +.5f, +.5f);
        public static readonly Vector3 BotLefFro = new(-.5f, -.5f, -.5f);
        public static readonly Vector3 BotLefBac = new(-.5f, -.5f, +.5f);
        public static readonly Vector3 BotRigFro = new(+.5f, -.5f, -.5f);
        public static readonly Vector3 BotRigBac = new(+.5f, -.5f, +.5f);

        public static readonly Cube<Vector3[]> FaceVertices =
            new(
                new[] { TopLefFro, TopRigFro, TopRigBac, TopLefBac },
                new[] { BotRigFro, BotLefFro, BotLefBac, BotRigBac },
                new[] { TopLefFro, TopLefBac, BotLefBac, BotLefFro },
                new[] { TopRigBac, TopRigFro, BotRigFro, BotRigBac },
                new[] { TopRigFro, TopLefFro, BotLefFro, BotRigFro },
                new[] { TopLefBac, TopRigBac, BotRigBac, BotLefBac }
            );

        public static readonly Cube<Vector3[]> SpriteVertices =
            new(
                null,
                null,
                new[] { TopLefFro, TopRigBac, BotRigBac, BotLefFro },
                new[] { TopRigFro, TopLefBac, BotLefBac, BotRigFro, },
                null,
                null
            );

        public static readonly Cube<Int3> FirstAmbientIndexes = new(
            new Int3(-1, 1, 0),
            new Int3(1, -1, 0),
            new Int3(-1, 0, -1),
            new Int3(1, 0, 1),
            new Int3(1, 0, -1),
            new Int3(-1, 0, 1)
        );

        public static readonly Cube<Int3[][]> AmbientIndexes = new(
            new[]
            {
                new[] { new Int3(-1, 1, 0), new Int3(-1, 1, -1), new Int3(0, 1, -1)},
                new[] { new Int3(0, 1, -1), new Int3(1, 1, -1), new Int3(1, 1, 0)},
                new[] { new Int3(1, 1, 0), new Int3(1, 1, 1), new Int3(0, 1, 1)},
                new[] { new Int3(0, 1, 1), new Int3(-1, 1, 1), new Int3(-1, 1, 0) }
            },
            new[]
            {
                new[] { new Int3(1, -1, 0), new Int3(1, -1, -1), new Int3(0, -1, -1)},
                new[] { new Int3(0, -1, -1), new Int3(-1, -1, -1), new Int3(-1, -1, 0)},
                new[] { new Int3(-1, -1, 0), new Int3(-1, -1, 1), new Int3(0, -1, 1)},
                new[] { new Int3(0, -1, 1), new Int3(1, -1, 1), new Int3(1, -1, 0)}
            },
            new[]
            {
                new[] { new Int3(-1, 0, -1), new Int3(-1, 1, -1),  new Int3(-1, 1, 0)},
                new[] { new Int3(-1, 1, 0), new Int3(-1, 1, 1),   new Int3(-1, 0, 1)},
                new[] { new Int3(-1, 0, 1), new Int3(-1, -1, 1),  new Int3(-1, -1, 0)},
                new[] { new Int3(-1, -1, 0), new Int3(-1, -1, -1), new Int3(-1, 0, -1)}
            },
            new[]
            {
                new[] { new Int3(1, 0, 1), new Int3(1, 1, 1), new Int3(1, 1, 0)},
                new[] { new Int3(1, 1, 0), new Int3(1, 1, -1), new Int3(1, 0, -1)},
                new[] { new Int3(1, 0, -1), new Int3(1, -1, -1), new Int3(1, -1, 0)},
                new[] { new Int3(1, -1, 0), new Int3(1, -1, 1), new Int3(1, 0, 1) }
            },
            new[]
            {
                new[] { new Int3(1, 0, -1), new Int3(1, 1, -1), new Int3(0, 1, -1)},
                new[] { new Int3(0, 1, -1), new Int3(-1, 1, -1), new Int3(-1, 0, -1)},
                new[] { new Int3(-1, 0, -1), new Int3(-1, -1, -1), new Int3(0, -1, -1)},
                new[] { new Int3(0, -1, -1), new Int3(1, -1, -1), new Int3(1, 0, -1)}
            },
            new[]
            {
                new[] { new Int3(-1, 0, 1), new Int3(-1, 1, 1), new Int3(0, 1, 1)},
                new[] { new Int3(0, 1, 1), new Int3(1, 1, 1), new Int3(1, 0, 1)},
                new[] { new Int3(1, 0, 1), new Int3(1, -1, 1), new Int3(0, -1, 1)},
                new[] { new Int3(0, -1, 1), new Int3(-1, -1, 1), new Int3(-1, 0, 1)}
            }
        );

        public static readonly Dictionary<Face, Int3> FaceDirectionMapping = new()
        {
            { Face.Top, new Int3(0, 1, 0) },
            { Face.Bottom, new Int3(0, -1, 0) },
            { Face.Left, new Int3(-1, 0, 0) },
            { Face.Right, new Int3(1, 0, 0) },
            { Face.Front, new Int3(0, 0, -1) },
            { Face.Back, new Int3(0, 0, 1) },
        };

        public static readonly Dictionary<Face, byte> FaceVisibilityFlagMapping = new()
        {
            { Face.Top, 1 },
            { Face.Bottom, 2 },
            { Face.Left, 4 },
            { Face.Right, 8 },
            { Face.Front, 16 },
            { Face.Back, 32 },
        };

        public static readonly Int2[] UvOffsetIndexes = { new(0, 0), new(1, 0), new(1, 1), new(0, 1) };

        public static readonly FaceBuildInfo Top = new(Face.Top);
        public static readonly FaceBuildInfo Bottom = new(Face.Bottom);
        public static readonly FaceBuildInfo Left = new(Face.Left);
        public static readonly FaceBuildInfo Right = new(Face.Right);
        public static readonly FaceBuildInfo Front = new(Face.Front);
        public static readonly FaceBuildInfo Back = new(Face.Back);

        public readonly byte DirectionFlag;
        public readonly Face Face;
        public readonly Int3 Direction;
        //public readonly Int3 FirstneighborIndex;
        public readonly VertexBuildInfo[] VerticesInfo;

        private FaceBuildInfo(Face face)
        {

            DirectionFlag = FaceVisibilityFlagMapping[face];
            Face = face;
            Direction = FaceDirectionMapping[face];
            VerticesInfo = FaceVertices.Faces[(byte)face].Select((vector, index) =>
                new VertexBuildInfo(index, vector, AmbientIndexes.Faces[(byte)face][index], UvOffsetIndexes[index])).ToArray();
        }
    }

    public sealed class VertexBuildInfo
    {
        public readonly int Index;
        public readonly Vector3 Position;
        public readonly Int3[] AmbientOcclusionNeighbors;
        public readonly Int2 TextureIndex;

        public VertexBuildInfo(int index, Vector3 position, Int3[] ambientOcclusionNeighbors, Int2 textureIndex)
        {
            Index = index;
            Position = position;
            AmbientOcclusionNeighbors = ambientOcclusionNeighbors;
            TextureIndex = textureIndex;
        }

    }

    public sealed class ChunkBuffer
    {
        public readonly Buffer VertexBuffer;
        public readonly Buffer IndexBuffer;
        public readonly VertexBufferBinding Binding;
        public readonly IDictionary<Int3, ChunkFace> Offsets;

        public ChunkBuffer(Device device, VertexSolidBlock[] vertices, uint[] indexes, Cube<ChunkFace> offsets)
        {
            VertexBuffer = Buffer.Create(device, BindFlags.VertexBuffer, vertices, vertices.Length * default(VertexSolidBlock).Size, ResourceUsage.Immutable);
            IndexBuffer = Buffer.Create(device, BindFlags.IndexBuffer, indexes, indexes.Length * sizeof(uint), ResourceUsage.Immutable);
            Binding = new VertexBufferBinding(VertexBuffer, default(VertexSolidBlock).Size, 0);
            Offsets = new Dictionary<Int3, ChunkFace>
            {
                [offsets.Top.BuildInfo.Direction] = offsets.Top,
                [offsets.Bottom.BuildInfo.Direction] = offsets.Bottom,
                [offsets.Left.BuildInfo.Direction] = offsets.Left,
                [offsets.Right.BuildInfo.Direction] = offsets.Right,
                [offsets.Front.BuildInfo.Direction] = offsets.Front,
                [offsets.Back.BuildInfo.Direction] = offsets.Back,
            };

        }
    }

    public sealed class ChunkFace
    {
        public readonly int Offset;
        public readonly int Count;
        public readonly FaceBuildInfo BuildInfo;
        public int ProcessedVoxels;
        public readonly VisibilityFlag Direction;

        public ChunkFace(int offset, int count, FaceBuildInfo buildInfo, VisibilityFlag direction)
        {
            Offset = offset;
            Count = count;
            BuildInfo = buildInfo;
            Direction = direction;
        }
    }
}
