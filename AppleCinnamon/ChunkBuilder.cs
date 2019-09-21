using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AppleCinnamon.Settings;
using AppleCinnamon.System;
using AppleCinnamon.Vertices;
using SharpDX;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;

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
            var verticesCube = new Cube<ChunkBuildFaceResult>(
                new ChunkBuildFaceResult(Int3.UnitY, chunk.VoxelCount.Top),
                new ChunkBuildFaceResult(-Int3.UnitY, chunk.VoxelCount.Bottom),
                new ChunkBuildFaceResult(-Int3.UnitX, chunk.VoxelCount.Left),
                new ChunkBuildFaceResult(Int3.UnitX, chunk.VoxelCount.Right),
                new ChunkBuildFaceResult(-Int3.UnitZ, chunk.VoxelCount.Front),
                new ChunkBuildFaceResult(Int3.UnitZ, chunk.VoxelCount.Back));

            var visibleFaces = 0;
            var visibilityFlags = chunk.VisibilityFlags;

            var faces = ChunkFaceBuildInfo.GetFaces();

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

                foreach (var faceInfo in faces)
                {
                    if ((flag & faceInfo.DirectionFlag) == faceInfo.DirectionFlag)
                    {
                        visibleFaces++;
                        var neighbor = chunk.GetLocalWithNeighbours(i + faceInfo.Direction.X, j + faceInfo.Direction.Y, k + faceInfo.Direction.Z);
                        AddFace(
                            faceInfo,
                            i, j, k,
                            verticesCube[faceInfo.Face],
                            definition, chunk, neighbor, voxelPositionOffset);

                        faceInfo.ProcessedVoxels++;
                    }
                }

            }

            var chunkBuffer = new ChunkBuffer(device,
                new ChunkFaceVertex(verticesCube.Top.Vertices, verticesCube.Top.Indexes),
                new ChunkFaceVertex(verticesCube.Bottom.Vertices, verticesCube.Bottom.Indexes),
                new ChunkFaceVertex(verticesCube.Left.Vertices, verticesCube.Left.Indexes),
                new ChunkFaceVertex(verticesCube.Right.Vertices, verticesCube.Right.Indexes),
                new ChunkFaceVertex(verticesCube.Front.Vertices, verticesCube.Front.Indexes),
                new ChunkFaceVertex(verticesCube.Back.Vertices, verticesCube.Back.Indexes));

            chunk.ChunkBuffer = chunkBuffer;


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
        public void AddFace(ChunkFaceBuildInfo face, int relativeIndexX, int relativeIndexY, int relativeIndexZ, ChunkBuildFaceResult faceResult, VoxelDefinition definition, Chunk chunk, Voxel neighbor, Vector3 voxelPositionOffset)
        {
            var textureUv = definition.TextureIndexes[face.Face];
            var vertexIndex = face.ProcessedVoxels * 4;
            var indexIndex = face.ProcessedVoxels * 6;

            var firstNeighbourVoxel = chunk.GetLocalWithNeighbours(
                relativeIndexX + face.FirstNeighbourIndex.X,
                relativeIndexY + face.FirstNeighbourIndex.Y,
                relativeIndexZ + face.FirstNeighbourIndex.Z);

            var firstNeighbourDefinition = VoxelDefinition.DefinitionByType[firstNeighbourVoxel.Block];

            for (var i = 0; i < 4; i++)
            {
                var faceVertices = face.Vertices[i];
                var position = new Vector3(
                    faceVertices.X * definition.Size.X + voxelPositionOffset.X,
                    faceVertices.Y * definition.Size.Y + voxelPositionOffset.Y,
                    faceVertices.Z * definition.Size.Z + voxelPositionOffset.Z);

                var uvCoordinateOffset = UvOffsetIndexes[i];
                var light = neighbor.Lightness + firstNeighbourVoxel.Lightness;
                var denominator = firstNeighbourDefinition.IsTransparent ? 2f : 1f;
                var ambientOcclusion = firstNeighbourDefinition.IsTransparent ? 0 : 1;
                var vertexNeighbors = face.AmbientOcclusionNeighbors[i];

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

                faceResult.Vertices[vertexIndex + i] = vertex;
            }

            faceResult.Indexes[indexIndex] = (ushort)(vertexIndex + 0);
            faceResult.Indexes[indexIndex + 1] = (ushort)(vertexIndex + 2);
            faceResult.Indexes[indexIndex + 2] = (ushort)(vertexIndex + 3);
            faceResult.Indexes[indexIndex + 3] = (ushort)(vertexIndex + 0);
            faceResult.Indexes[indexIndex + 4] = (ushort)(vertexIndex + 1);
            faceResult.Indexes[indexIndex + 5] = (ushort)(vertexIndex + 2);
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

        public static readonly ChunkFaceBuildInfo[] Faces = { Top, Bottom, Left, Right, Front, Back };

        public readonly byte DirectionFlag;
        public readonly Face Face;
        public readonly Int3 Direction;
        public readonly Vector3[] Vertices;
        public readonly Int3 FirstNeighbourIndex;
        public readonly Int3[][] AmbientOcclusionNeighbors;
        public int ProcessedVoxels;

        private ChunkFaceBuildInfo(byte directionFlag, Face face, Int3 direction, Vector3[] vertices, Int3 firstNeighbourIndex, Int3[][] ambientOcclusionNeighbors)
        {
            DirectionFlag = directionFlag;
            Face = face;
            Direction = direction;
            Vertices = vertices;
            FirstNeighbourIndex = firstNeighbourIndex;
            AmbientOcclusionNeighbors = ambientOcclusionNeighbors;
        }

        public static ChunkFaceBuildInfo[] GetFaces()
        {
            return new[] {Top.Copy(), Bottom.Copy(), Left.Copy(), Right.Copy(), Front.Copy(), Back.Copy() };
        }

        public ChunkFaceBuildInfo Copy()
        {
            return new ChunkFaceBuildInfo(DirectionFlag, Face, Direction, Vertices, FirstNeighbourIndex, AmbientOcclusionNeighbors);
        }
    }

    public class ChunkBuffer
    {
        public readonly Buffer VertexBuffer;
        public readonly Buffer IndexBuffer;
        public readonly VertexBufferBinding Binding;
        public readonly IDictionary<Int3, OffsetData> Offsets;

        public ChunkBuffer(Device device,
            ChunkFaceVertex top,
            ChunkFaceVertex bot,
            ChunkFaceVertex lef,
            ChunkFaceVertex rig,
            ChunkFaceVertex fro,
            ChunkFaceVertex bac)
        {
            var topCount = top.Vertices.Length / 4;
            var botCount = bot.Vertices.Length / 4;
            var lefCount = lef.Vertices.Length / 4;
            var rigCount = rig.Vertices.Length / 4;
            var froCount = fro.Vertices.Length / 4;
            var bacCount = bac.Vertices.Length / 4;

            var allCount = topCount + botCount + lefCount + rigCount + froCount + bacCount;

            var topOffset = 0;
            var botOffset = topCount;
            var lefOffset = botOffset + botCount;
            var rigOffset = lefOffset + lefCount;
            var froOffset = rigOffset + rigCount;
            var bacOffset = froOffset + froCount;

            var vertices = new VertexSolidBlock[allCount * 4];
            Array.Copy(top.Vertices, 0, vertices, topOffset * 4, topCount * 4);
            Array.Copy(bot.Vertices, 0, vertices, botOffset * 4, botCount * 4);
            Array.Copy(lef.Vertices, 0, vertices, lefOffset * 4, lefCount * 4);
            Array.Copy(rig.Vertices, 0, vertices, rigOffset * 4, rigCount * 4);
            Array.Copy(fro.Vertices, 0, vertices, froOffset * 4, froCount * 4);
            Array.Copy(bac.Vertices, 0, vertices, bacOffset * 4, bacCount * 4);


            var indexes = new ushort[allCount * 6];
            Array.Copy(top.Indexes, 0, indexes, topOffset * 6, topCount * 6);
            Array.Copy(bot.Indexes, 0, indexes, botOffset * 6, botCount * 6);
            Array.Copy(lef.Indexes, 0, indexes, lefOffset * 6, lefCount * 6);
            Array.Copy(rig.Indexes, 0, indexes, rigOffset * 6, rigCount * 6);
            Array.Copy(fro.Indexes, 0, indexes, froOffset * 6, froCount * 6);
            Array.Copy(bac.Indexes, 0, indexes, bacOffset * 6, bacCount * 6);

            Offsets = new Dictionary<Int3, OffsetData>
            {
                [new Int3(+0, +1, +0)] = new OffsetData(topOffset, topCount),
                [new Int3(+0, -1, +0)] = new OffsetData(botOffset, botCount),
                [new Int3(-1, +0, +0)] = new OffsetData(lefOffset, lefCount),
                [new Int3(+1, +0, +0)] = new OffsetData(rigOffset, rigCount),
                [new Int3(+0, +0, -1)] = new OffsetData(froOffset, froCount),
                [new Int3(+0, +0, +1)] = new OffsetData(bacOffset, bacCount)
            };

            VertexBuffer = Buffer.Create(device, BindFlags.VertexBuffer, vertices);
            IndexBuffer = Buffer.Create(device, BindFlags.IndexBuffer, indexes);
            Binding = new VertexBufferBinding(VertexBuffer, VertexSolidBlock.Size, 0);

        }
    }

    public struct OffsetData
    {
        public readonly int Offset;
        public readonly int Count;

        public OffsetData(int offset, int count)
        {
            Offset = offset;
            Count = count;
        }
    }

    public class ChunkFaceVertex
    {
        public VertexSolidBlock[] Vertices { get; }
        public ushort[] Indexes { get; }

        public ChunkFaceVertex(VertexSolidBlock[] vertices, ushort[] indexes)
        {
            Vertices = vertices;
            Indexes = indexes;
        }
    }
}
