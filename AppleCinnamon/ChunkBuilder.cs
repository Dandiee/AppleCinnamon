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
                var flag = visibilityFlag.Value;

                var voxelPositionOffset = definition.Translation + chunk.OffsetVector + new Vector3(i, j, k);

                foreach (var faceInfo in offsetIterator)
                {
                    if ((flag & faceInfo.BuildInfo.DirectionFlag) == faceInfo.BuildInfo.DirectionFlag)
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
            var textureUv = definition.TextureIndexes[face.BuildInfo.Face];
            var offset = face.Offset + face.ProcessedVoxels;
            var vertexIndex = offset * 4;
            var indexIndex = offset * 6;

            var firstNeighbourVoxel = chunk.GetLocalWithNeighbours(
                relativeIndexX + face.BuildInfo.FirstNeighbourIndex.X,
                relativeIndexY + face.BuildInfo.FirstNeighbourIndex.Y,
                relativeIndexZ + face.BuildInfo.FirstNeighbourIndex.Z);

            var firstNeighbourDefinition = VoxelDefinition.DefinitionByType[firstNeighbourVoxel.Block];

            for (var i = 0; i < 4; i++)
            {
                var faceVertices = face.BuildInfo.Vertices[i];
                var position = new Vector3(
                    faceVertices.X * definition.Size.X + voxelPositionOffset.X,
                    faceVertices.Y * definition.Size.Y + voxelPositionOffset.Y,
                    faceVertices.Z * definition.Size.Z + voxelPositionOffset.Z);

                var uvCoordinateOffset = UvOffsetIndexes[i];
                var light = neighbor.Lightness + firstNeighbourVoxel.Lightness;
                var denominator = firstNeighbourDefinition.IsTransparent ? 2f : 1f;
                var ambientOcclusion = firstNeighbourDefinition.IsTransparent ? 0 : 1;
                var vertexNeighbors = face.BuildInfo.AmbientOcclusionNeighbors[i];

                for (var j = 0; j < 2; j++)
                {
                    var currentNeighbourIndex = vertexNeighbors[j];
                    var currentNeighbourVoxel =
                        chunk.GetLocalWithNeighbours(
                            relativeIndexX + currentNeighbourIndex.X,
                            relativeIndexY + currentNeighbourIndex.Y,
                            relativeIndexZ + currentNeighbourIndex.Z);

                    var currentNeighbourDefinition = VoxelDefinition.DefinitionByType[currentNeighbourVoxel.Block];

                    if (currentNeighbourDefinition.IsTransparent)
                    {
                        light += currentNeighbourVoxel.Lightness;
                        denominator++;
                    }
                    else ambientOcclusion++;

                    if (j == 1)
                    {
                        firstNeighbourVoxel = currentNeighbourVoxel;
                        firstNeighbourDefinition = currentNeighbourDefinition;
                    }
                }

                var vertex = new VertexSolidBlock(position,
                    (byte)(textureUv.X + uvCoordinateOffset.X),
                    (byte)(textureUv.Y + uvCoordinateOffset.Y),
                    (byte)ambientOcclusion,
                    (byte)(light / denominator));

                vertices[vertexIndex + i] = vertex;
            }

            indexes[indexIndex] = (ushort)(vertexIndex + 0);
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

            var allCount = topCount + botCount + lefCount + rigCount + froCount + bacCount;

            var topOffset = 0;
            var botOffset = topCount;
            var lefOffset = botOffset + botCount;
            var rigOffset = lefOffset + lefCount;
            var froOffset = rigOffset + rigCount;
            var bacOffset = froOffset + froCount;

            var result = new Cube<OffsetData>(
                new OffsetData(topOffset, topCount, ChunkFaceBuildInfo.Top),
                new OffsetData(botOffset, botCount, ChunkFaceBuildInfo.Bottom),
                new OffsetData(lefOffset, lefCount, ChunkFaceBuildInfo.Left),
                new OffsetData(rigOffset, rigCount, ChunkFaceBuildInfo.Right),
                new OffsetData(froOffset, froCount, ChunkFaceBuildInfo.Front),
                new OffsetData(bacOffset, bacCount, ChunkFaceBuildInfo.Back));

            return result;
        }
    }

    public sealed class ChunkFaceBuildInfo
    {
        public static readonly ChunkFaceBuildInfo Top = new ChunkFaceBuildInfo(1, Face.Top, new Int3(0, 1, 0), ChunkBuilder.FaceVertices.Top, ChunkBuilder.FirstAmbientIndexes.Top, ChunkBuilder.AmbientIndexes.Top);
        public static readonly ChunkFaceBuildInfo Bottom = new ChunkFaceBuildInfo(2, Face.Bottom, new Int3(0, -1, 0), ChunkBuilder.FaceVertices.Bottom, ChunkBuilder.FirstAmbientIndexes.Bottom, ChunkBuilder.AmbientIndexes.Bottom);
        public static readonly ChunkFaceBuildInfo Left = new ChunkFaceBuildInfo(4, Face.Left, new Int3(-1, 0, 0), ChunkBuilder.FaceVertices.Left, ChunkBuilder.FirstAmbientIndexes.Left, ChunkBuilder.AmbientIndexes.Left);
        public static readonly ChunkFaceBuildInfo Right = new ChunkFaceBuildInfo(8, Face.Right, new Int3(1, 0, 0), ChunkBuilder.FaceVertices.Right, ChunkBuilder.FirstAmbientIndexes.Right, ChunkBuilder.AmbientIndexes.Right);
        public static readonly ChunkFaceBuildInfo Front = new ChunkFaceBuildInfo(16, Face.Front, new Int3(0, 0, -1), ChunkBuilder.FaceVertices.Front, ChunkBuilder.FirstAmbientIndexes.Front, ChunkBuilder.AmbientIndexes.Front);
        public static readonly ChunkFaceBuildInfo Back = new ChunkFaceBuildInfo(32, Face.Back, new Int3(0, 0, 1), ChunkBuilder.FaceVertices.Back, ChunkBuilder.FirstAmbientIndexes.Back, ChunkBuilder.AmbientIndexes.Back);

        public readonly byte DirectionFlag;
        public readonly Face Face;
        public readonly Int3 Direction;
        public readonly Vector3[] Vertices;
        public readonly Int3 FirstNeighbourIndex;
        public readonly Int3[][] AmbientOcclusionNeighbors;

        private ChunkFaceBuildInfo(byte directionFlag, Face face, Int3 direction, Vector3[] vertices, Int3 firstNeighbourIndex, Int3[][] ambientOcclusionNeighbors)
        {
            DirectionFlag = directionFlag;
            Face = face;
            Direction = direction;
            Vertices = vertices;
            FirstNeighbourIndex = firstNeighbourIndex;
            AmbientOcclusionNeighbors = ambientOcclusionNeighbors;
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
        public readonly ChunkFaceBuildInfo BuildInfo;
        public int ProcessedVoxels;

        public OffsetData(int offset, int count, ChunkFaceBuildInfo buildInfo)
        {
            Offset = offset;
            Count = count;
            BuildInfo = buildInfo;
        }
    }

    public class ChunkFaceVertex
    {
        public VertexSolidBlock Vertices { get; }
        public ushort Indexes { get; }

        public ChunkFaceVertex(VertexSolidBlock vertices, ushort indexes)
        {
            Vertices = vertices;
            Indexes = indexes;
        }
    }
}
