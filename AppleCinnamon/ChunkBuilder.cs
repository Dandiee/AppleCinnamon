using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using AppleCinnamon.Settings;
using AppleCinnamon.System;
using AppleCinnamon.Vertices;
using SharpDX;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

namespace AppleCinnamon
{
    public interface IChunkBuilder
    {
        void BuildChunk(Device device, Chunk chunk);
    }


    public sealed partial class ChunkBuilder : IChunkBuilder
    {
        public void BuildChunk(Device device, Chunk chunk)
        {
            var offsets = GetOffsetData(chunk);
            var visibleFacesCount = chunk.VoxelCount.Top + chunk.VoxelCount.Bottom + chunk.VoxelCount.Left +
                                    chunk.VoxelCount.Right + chunk.VoxelCount.Front + chunk.VoxelCount.Back;

            var vertices = new VertexSolidBlock[visibleFacesCount * 4];
            var indexes = new ushort[visibleFacesCount * 6];

            var visibleFaces = 0;
            var visibilityFlags = chunk.VisibilityFlags;

            var offsetIterator = offsets.GetAll().Select(s => s.Value).ToList();

            foreach (var visibilityFlag in visibilityFlags)
            {
                var index = visibilityFlag.Key;

                var k = index / (Chunk.SizeXy * Chunk.Height);
                var j = (index - k * Chunk.SizeXy * Chunk.Height) / Chunk.SizeXy;
                var i = index - (k * Chunk.SizeXy * Chunk.Height + j * Chunk.SizeXy);

                var voxel = chunk.Voxels[index];
                var definition = VoxelDefinition.DefinitionByType[voxel.Block];

                var voxelPositionOffset = definition.Translation + chunk.OffsetVector + new Vector3(i, j, k);

                foreach (var faceInfo in offsetIterator)
                {
                    if ((visibilityFlag.Value & faceInfo.BuildInfo.DirectionFlag) == faceInfo.BuildInfo.DirectionFlag)
                    {
                        var neighbor = chunk.GetLocalWithNeighbours(i + faceInfo.BuildInfo.Direction.X, j + faceInfo.BuildInfo.Direction.Y, k + faceInfo.BuildInfo.Direction.Z);
                        AddFace(faceInfo, i, j, k, vertices, indexes, definition, chunk, neighbor, voxelPositionOffset);
                    }
                }

            }

            chunk.ChunkBuffer = new ChunkBuffer(device, vertices, indexes, offsets);


            chunk.VisibleFacesCount = visibleFaces;
            var waterBuffer = AddWaterFace(chunk, device);

            chunk.SetBuffers(waterBuffer);
        }

        private FaceBuffer AddWaterFace(Chunk chunk, Device device)
        {
            if (chunk.TopMostWaterVoxels.Count == 0)
            {
                return null;
            }

            var topOffsetVertices = FaceVertices[Face.Top];
            var vertices = new VertexWater[chunk.TopMostWaterVoxels.Count * 4];
            var indexes = new ushort[chunk.TopMostWaterVoxels.Count * 6 * 2];

            for (var n = 0; n < chunk.TopMostWaterVoxels.Count; n++)
            {
                var index = chunk.TopMostWaterVoxels[n];

                var k = index / (Chunk.SizeXy * Chunk.Height);
                var j = (index - k * Chunk.SizeXy * Chunk.Height) / Chunk.SizeXy;
                var i = index - (k * Chunk.SizeXy * Chunk.Height + j * Chunk.SizeXy);

                var vertexOffset = n * 4;
                var positionOffset = new Vector3(i, j - 0.1f, k);
                var light = chunk.Voxels[index].Lightness;

                for (var m = 0; m < topOffsetVertices.Length; m++)
                {
                    var position = topOffsetVertices[m] + chunk.OffsetVector + positionOffset;
                    var textureUv = WaterUvOffsets[m];

                    vertices[vertexOffset + m] =
                        new VertexWater(position, textureUv, 0, light);
                }

                var indexOffset = n * 6 * 2;

                indexes[indexOffset + 0] = (ushort)(vertexOffset + 0);
                indexes[indexOffset + 1] = (ushort)(vertexOffset + 2);
                indexes[indexOffset + 2] = (ushort)(vertexOffset + 3);
                indexes[indexOffset + 3] = (ushort)(vertexOffset + 0);
                indexes[indexOffset + 4] = (ushort)(vertexOffset + 1);
                indexes[indexOffset + 5] = (ushort)(vertexOffset + 2);

                indexes[indexOffset + 6] = (ushort)(vertexOffset + 2);
                indexes[indexOffset + 7] = (ushort)(vertexOffset + 1);
                indexes[indexOffset + 8] = (ushort)(vertexOffset + 0);
                indexes[indexOffset + 9] = (ushort)(vertexOffset + 3);
                indexes[indexOffset + 10] = (ushort)(vertexOffset + 2);
                indexes[indexOffset + 11] = (ushort)(vertexOffset + 0);
            }

            return new FaceBuffer(
                indexes.Length,
                VertexWater.Size,
                Buffer.Create(device, BindFlags.VertexBuffer, vertices),
                Buffer.Create(device, BindFlags.IndexBuffer, indexes));
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddFace(OffsetData face, int relativeIndexX, int relativeIndexY, int relativeIndexZ, VertexSolidBlock[] vertices, ushort[] indexes, VoxelDefinition definition, Chunk chunk, Voxel neighbor, Vector3 voxelPositionOffset)
        {
            // Face specific base variables
            var textureUv = definition.TextureIndexes[face.BuildInfo.Face];
            var offset = face.Offset + face.ProcessedVoxels;
            var vertexIndex = offset * 4;
            var indexIndex = offset * 6;

            // Initialize ambient neighbours
            var ambientNeighbourIndex = face.BuildInfo.FirstNeighbourIndex;
            var ambientNeighbourVoxel = chunk.GetLocalWithNeighbours(relativeIndexX + ambientNeighbourIndex.X, relativeIndexY + ambientNeighbourIndex.Y, relativeIndexZ + ambientNeighbourIndex.Z);
            var ambientNeighbourDefinition = VoxelDefinition.DefinitionByType[ambientNeighbourVoxel.Block];

            // Visit all ambient neighbours
            foreach(var vertexInfo in face.BuildInfo.VerticesInfo)
            {
                var position = new Vector3(
                    vertexInfo.Position.X * definition.Size.X + voxelPositionOffset.X,
                    vertexInfo.Position.Y * definition.Size.Y + voxelPositionOffset.Y,
                    vertexInfo.Position.Z * definition.Size.Z + voxelPositionOffset.Z);

                var totalNeighbourLight = ambientNeighbourVoxel.Lightness;
                var numberOfAmbientNeighbours = ambientNeighbourDefinition.IsTransparent ? 0 : 1;

                for (var j = 0; j < 2; j++)
                {
                    ambientNeighbourIndex = vertexInfo.AmbientOcclusionNeighbors[j];
                    ambientNeighbourVoxel = chunk.GetLocalWithNeighbours(relativeIndexX + ambientNeighbourIndex.X, relativeIndexY + ambientNeighbourIndex.Y, relativeIndexZ + ambientNeighbourIndex.Z);
                    ambientNeighbourDefinition = VoxelDefinition.DefinitionByType[ambientNeighbourVoxel.Block];

                    if (ambientNeighbourDefinition.IsTransparent)
                    {
                        totalNeighbourLight += ambientNeighbourVoxel.Lightness;
                    }
                    else
                    {
                        numberOfAmbientNeighbours++;
                    }
                }

                vertices[vertexIndex + vertexInfo.Index] = new VertexSolidBlock(position, textureUv.X + vertexInfo.TextureIndex.X,
                    textureUv.Y + vertexInfo.TextureIndex.Y, neighbor.Lightness, totalNeighbourLight,
                    numberOfAmbientNeighbours);
            }

            indexes[indexIndex] = (ushort)vertexIndex;
            indexes[indexIndex + 1] = (ushort)(vertexIndex + 2);
            indexes[indexIndex + 2] = (ushort)(vertexIndex + 3);
            indexes[indexIndex + 3] = (ushort)(vertexIndex + 0);
            indexes[indexIndex + 4] = (ushort)(vertexIndex + 1);
            indexes[indexIndex + 5] = (ushort)(vertexIndex + 2);

            face.ProcessedVoxels++;
        }


        private Cube<OffsetData> GetOffsetData(Chunk chunk)
        {
            var topCount = chunk.VoxelCount.Top;
            var botCount = chunk.VoxelCount.Bottom;
            var lefCount = chunk.VoxelCount.Left;
            var rigCount = chunk.VoxelCount.Right;
            var froCount = chunk.VoxelCount.Front;
            var bacCount = chunk.VoxelCount.Back;

            var topOffset = 0;
            var botOffset = topCount;
            var lefOffset = botOffset + botCount;
            var rigOffset = lefOffset + lefCount;
            var froOffset = rigOffset + rigCount;
            var bacOffset = froOffset + froCount;

            var result = new Cube<OffsetData>(
                new OffsetData(topOffset, topCount, FaceBuildInfo.Top),
                new OffsetData(botOffset, botCount, FaceBuildInfo.Bottom),
                new OffsetData(lefOffset, lefCount, FaceBuildInfo.Left),
                new OffsetData(rigOffset, rigCount, FaceBuildInfo.Right),
                new OffsetData(froOffset, froCount, FaceBuildInfo.Front),
                new OffsetData(bacOffset, bacCount, FaceBuildInfo.Back));

            return result;
        }
    }

    public sealed class FaceBuildInfo
    {
        public static readonly Vector3 TopLefFro = new Vector3(-.5f, +.5f, -.5f);
        public static readonly Vector3 TopRigFro = new Vector3(+.5f, +.5f, -.5f);
        public static readonly Vector3 TopLefBac = new Vector3(-.5f, +.5f, +.5f);
        public static readonly Vector3 TopRigBac = new Vector3(+.5f, +.5f, +.5f);
        public static readonly Vector3 BotLefFro = new Vector3(-.5f, -.5f, -.5f);
        public static readonly Vector3 BotLefBac = new Vector3(-.5f, -.5f, +.5f);
        public static readonly Vector3 BotRigFro = new Vector3(+.5f, -.5f, -.5f);
        public static readonly Vector3 BotRigBac = new Vector3(+.5f, -.5f, +.5f);

        public static readonly Cube<Vector3[]> FaceVertices =
            new Cube<Vector3[]>(
                new[] { TopLefFro, TopRigFro, TopRigBac, TopLefBac },
                new[] { BotRigFro, BotLefFro, BotLefBac, BotRigBac },
                new[] { TopLefFro, TopLefBac, BotLefBac, BotLefFro },
                new[] { TopRigBac, TopRigFro, BotRigFro, BotRigBac },
                new[] { TopRigFro, TopLefFro, BotLefFro, BotRigFro },
                new[] { TopLefBac, TopRigBac, BotRigBac, BotLefBac }
            );

        public static readonly Cube<Int3> FirstAmbientIndexes = new Cube<Int3>(
            new Int3(-1, 1, 0),
            new Int3(1, -1, 0),
            new Int3(-1, 0, -1),
            new Int3(1, 0, 1),
            new Int3(1, 0, -1),
            new Int3(-1, 0, 1)
        );

        public static readonly Cube<Int3[][]> AmbientIndexes = new Cube<Int3[][]>(
            new[]
            {
                new[] {new Int3(-1, 1, -1), new Int3(0, 1, -1)},
                new[] {new Int3(1, 1, -1), new Int3(1, 1, 0)},
                new[] {new Int3(1, 1, 1), new Int3(0, 1, 1)},
                new[] {new Int3(-1, 1, 0), new Int3(-1, 1, 1)}
            },
            new[]
            {
                new[] {new Int3(1, -1, -1), new Int3(0, -1, -1)},
                new[] {new Int3(-1, -1, -1), new Int3(-1, -1, 0)},
                new[] {new Int3(-1, -1, 1), new Int3(0, -1, 1)},
                new[] {new Int3(1, -1, 1), new Int3(1, -1, 0)}
            },
            new[]
            {
                new[] {new Int3(-1, 1, -1), new Int3(-1, 1, 0)},
                new[] {new Int3(-1, 1, 1), new Int3(-1, 0, 1)},
                new[] {new Int3(-1, -1, 1), new Int3(-1, -1, 0)},
                new[] {new Int3(-1, -1, -1), new Int3(-1, 0, -1)}
            },
            new[]
            {
                new[] {new Int3(1, 1, 1), new Int3(1, 1, 0)},
                new[] {new Int3(1, 1, -1), new Int3(1, 0, -1)},
                new[] {new Int3(1, -1, -1), new Int3(1, -1, 0)},
                new[] {new Int3(1, 0, 1), new Int3(1, -1, 1), }
            },
            new[]
            {
                new[] {new Int3(1, 1, -1), new Int3(0, 1, -1)},
                new[] {new Int3(-1, 1, -1), new Int3(-1, 0, -1)},
                new[] {new Int3(-1, -1, -1), new Int3(0, -1, -1)},
                new[] {new Int3(1, 0, -1), new Int3(1, -1, -1)}
            },
            new[]
            {
                new[] {new Int3(-1, 1, 1), new Int3(0, 1, 1)},
                new[] {new Int3(1, 1, 1), new Int3(1, 0, 1)},
                new[] {new Int3(1, -1, 1), new Int3(0, -1, 1)},
                new[] {new Int3(-1, -1, 1), new Int3(-1, 0, 1)}
            }
        );

        public static readonly Dictionary<Face, Int3> FaceDirectionMapping = new Dictionary<Face, Int3>
        {
            {Face.Top, new Int3(0, 1, 0)},
            {Face.Bottom, new Int3(0, -1, 0)},
            {Face.Left, new Int3(-1, 0, 0)},
            {Face.Right, new Int3(1, 0, 0)},
            {Face.Front, new Int3(0, 0, -1)},
            {Face.Back, new Int3(0, 0, 1)},
        };

        public static readonly Dictionary<Face, byte> FaceVisibilityFlagMapping = new Dictionary<Face, byte>
        {
            {Face.Top, 1},
            {Face.Bottom, 2},
            {Face.Left, 4},
            {Face.Right, 8},
            {Face.Front, 16},
            {Face.Back, 32},
        };

        public static readonly Int2[] UvOffsetIndexes = { new Int2(0, 0), new Int2(1, 0), new Int2(1, 1), new Int2(0, 1) };

        public static readonly FaceBuildInfo Top = new FaceBuildInfo(Face.Top);
        public static readonly FaceBuildInfo Bottom = new FaceBuildInfo(Face.Bottom);
        public static readonly FaceBuildInfo Left = new FaceBuildInfo(Face.Left);
        public static readonly FaceBuildInfo Right = new FaceBuildInfo(Face.Right);
        public static readonly FaceBuildInfo Front = new FaceBuildInfo(Face.Front);
        public static readonly FaceBuildInfo Back = new FaceBuildInfo(Face.Back);

        public readonly byte DirectionFlag;
        public readonly Face Face;
        public readonly Int3 Direction;
        public readonly Int3 FirstNeighbourIndex;
        public readonly VertexBuildInfo[] VerticesInfo;

        private FaceBuildInfo(Face face)
        {
            
            DirectionFlag = FaceVisibilityFlagMapping[face];
            Face = face;
            Direction = FaceDirectionMapping[face]; ;
            VerticesInfo = FaceVertices[face].Select((vector, index) =>
                new VertexBuildInfo(index, vector, AmbientIndexes[face][index], UvOffsetIndexes[index])).ToArray();
            FirstNeighbourIndex = FirstAmbientIndexes[face];
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

    public class ChunkBuffer
    {
        public readonly Buffer VertexBuffer;
        public readonly Buffer IndexBuffer;
        public readonly VertexBufferBinding Binding;
        public readonly IDictionary<Int3, OffsetData> Offsets;

        public ChunkBuffer(Device device, VertexSolidBlock[] vertices, ushort[] indexes, Cube<OffsetData> offsets)
        {
            VertexBuffer = Buffer.Create(device, BindFlags.VertexBuffer, vertices);
            IndexBuffer = Buffer.Create(device, BindFlags.IndexBuffer, indexes);
            Binding = new VertexBufferBinding(VertexBuffer, VertexSolidBlock.Size, 0);
            Offsets = new Dictionary<Int3, OffsetData>
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

    public class OffsetData
    {
        public readonly int Offset;
        public readonly int Count;
        public readonly FaceBuildInfo BuildInfo;
        public int ProcessedVoxels;

        public OffsetData(int offset, int count, FaceBuildInfo buildInfo)
        {
            Offset = offset;
            Count = count;
            BuildInfo = buildInfo;
        }
    }
}
