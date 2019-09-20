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
                new ChunkBuildFaceResult(Int3.UnitY),
                new ChunkBuildFaceResult(-Int3.UnitY),
                new ChunkBuildFaceResult(-Int3.UnitX),
                new ChunkBuildFaceResult(Int3.UnitX),
                new ChunkBuildFaceResult(-Int3.UnitZ),
                new ChunkBuildFaceResult(Int3.UnitZ));

            var visibleFaces = 0;
            var visibilityFlags = chunk.VisibilityFlags;

            foreach (var visibilityFlag in visibilityFlags)
            {

                var index = visibilityFlag.Key;

                var k = index / (Chunk.SizeXy * Chunk.Height);
                var j = (index - k * Chunk.SizeXy * Chunk.Height) / Chunk.SizeXy;
                var i = index - (k * Chunk.SizeXy * Chunk.Height + j * Chunk.SizeXy);

                var voxel = chunk.Voxels[index];
                var definition = VoxelDefinition.DefinitionByType[voxel.Block];
                var flag = visibilityFlag.Value.VisibilityFlags;

                var voxelPositionOffset = definition.Translation + chunk.OffsetVector + new Vector3(i, j, k);

                

                if ((flag & 1) == 1)
                {
                    var neighbor = chunk.GetLocalWithNeighbours(i, j + 1, k);
                    AddFace(Face.Top, i, j, k, verticesCube.Top, definition, chunk, neighbor, voxelPositionOffset);
                    visibleFaces++;
                }

                if ((flag & 2) == 2)
                {
                    var neighbor = chunk.GetLocalWithNeighbours(i, j - 1, k);
                    AddFace(Face.Bottom, i, j, k, verticesCube.Bottom, definition, chunk, neighbor, voxelPositionOffset);
                    visibleFaces++;
                }

                if ((flag & 4) == 4)
                {
                    var neighbor = chunk.GetLocalWithNeighbours(i - 1, j, k);
                    AddFace(Face.Left, i, j, k, verticesCube.Left, definition, chunk, neighbor, voxelPositionOffset);
                    visibleFaces++;
                }

                if ((flag & 8) == 8)
                {
                    var neighbor = chunk.GetLocalWithNeighbours(i + 1, j, k);
                    AddFace(Face.Right, i, j, k, verticesCube.Right, definition, chunk, neighbor, voxelPositionOffset);
                    visibleFaces++;
                }

                if ((flag & 16) == 16)
                {
                    var neighbor = chunk.GetLocalWithNeighbours(i, j, k - 1);
                    AddFace(Face.Front, i, j, k, verticesCube.Front, definition, chunk, neighbor, voxelPositionOffset);
                    visibleFaces++;
                }

                if ((flag & 32) == 32)
                {
                    var neighbor = chunk.GetLocalWithNeighbours(i, j, k + 1);
                    AddFace(Face.Back, i, j, k, verticesCube.Back, definition, chunk, neighbor, voxelPositionOffset);
                    visibleFaces++;
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
        public void AddFace(Face face, int relativeIndexX, int relativeIndexY, int relativeIndexZ, ChunkBuildFaceResult faceResult, VoxelDefinition definition, Chunk chunk, Voxel neighbor, Vector3 voxelPositionOffset)
        {
            var faceVertices = FaceVertices[face];
            var textureUv = definition.TextureIndexes[face];
            var aoIndexes = AmbientIndexes[face];
            var vertexIndex = faceResult.Vertices.Count - 1;

            for (var i = 0; i < 4; i++)
            {
                var position = (faceVertices[i] * definition.Size) + voxelPositionOffset;
                //var uvCoordinate = UvOffsets[i] + textureUv;
                var uvCoordinateOffset = UvOffsetIndexes[i];
                var light = neighbor.Lightness;
                var denominator = 1f;
                var ambientOcclusion = 0;

                foreach (var index in aoIndexes[i])
                {
                    var aoFriend = chunk.GetLocalWithNeighbours(relativeIndexX + index.X, relativeIndexY + index.Y, relativeIndexZ + index.Z);
                    var aoFriendDefinition = VoxelDefinition.DefinitionByType[aoFriend.Block];

                    if (aoFriendDefinition.IsTransparent)
                    {
                        light += aoFriend.Lightness;
                        denominator++;
                    }

                    else ambientOcclusion++;
                }

                faceResult.Vertices.Add(new VertexSolidBlock(position, 
                    (byte)(textureUv.X + uvCoordinateOffset.X),
                    (byte)(textureUv.Y + uvCoordinateOffset.Y),
                    (byte)ambientOcclusion, 
                    (byte)(light/ denominator)));
            }

            faceResult.Indexes.Add((ushort)(vertexIndex + 1));
            faceResult.Indexes.Add((ushort)(vertexIndex + 3));
            faceResult.Indexes.Add((ushort)(vertexIndex + 4));
            faceResult.Indexes.Add((ushort)(vertexIndex + 1));
            faceResult.Indexes.Add((ushort)(vertexIndex + 2));
            faceResult.Indexes.Add((ushort)(vertexIndex + 3));
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
            var topCount = top.Vertices.Count / 4;
            var botCount = bot.Vertices.Count / 4;
            var lefCount = lef.Vertices.Count / 4;
            var rigCount = rig.Vertices.Count / 4;
            var froCount = fro.Vertices.Count / 4;
            var bacCount = bac.Vertices.Count / 4;

            var allCount = topCount + botCount + lefCount + rigCount + froCount + bacCount;

            var topOffset = 0;
            var botOffset = topCount;
            var lefOffset = botOffset + botCount;
            var rigOffset = lefOffset + lefCount;
            var froOffset = rigOffset + rigCount;
            var bacOffset = froOffset + froCount;

            var vertices = new VertexSolidBlock[allCount * 4];
            top.Vertices.CopyTo(0, vertices, topOffset * 4, topCount * 4);
            bot.Vertices.CopyTo(0, vertices, botOffset * 4, botCount * 4);
            lef.Vertices.CopyTo(0, vertices, lefOffset * 4, lefCount * 4);
            rig.Vertices.CopyTo(0, vertices, rigOffset * 4, rigCount * 4);
            fro.Vertices.CopyTo(0, vertices, froOffset * 4, froCount * 4);
            bac.Vertices.CopyTo(0, vertices, bacOffset * 4, bacCount * 4);

            var indexes = new ushort[allCount * 6];
            top.Indexes.CopyTo(0, indexes, topOffset * 6, topCount * 6);
            bot.Indexes.CopyTo(0, indexes, botOffset * 6, botCount * 6);
            lef.Indexes.CopyTo(0, indexes, lefOffset * 6, lefCount * 6);
            rig.Indexes.CopyTo(0, indexes, rigOffset * 6, rigCount * 6);
            fro.Indexes.CopyTo(0, indexes, froOffset * 6, froCount * 6);
            bac.Indexes.CopyTo(0, indexes, bacOffset * 6, bacCount * 6);


            // var indexes = new ushort[allCount * 6];
            // CopyWithOffset(top, indexes, topOffset);
            // CopyWithOffset(bot, indexes, botOffset);
            // CopyWithOffset(lef, indexes, lefOffset);
            // CopyWithOffset(rig, indexes, rigOffset);
            // CopyWithOffset(fro, indexes, froOffset);
            // CopyWithOffset(bac, indexes, bacOffset);


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

        private void CopyWithOffset(ChunkFaceVertex side, ushort[] indexes, int offset)
        {
            var indexOffset = offset * 6;

            for (var i = 0; i < side.Indexes.Count; i++)
            {
                indexes[i + indexOffset] = (ushort)(side.Indexes[i] + offset * 4);
            }
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
        public List<VertexSolidBlock> Vertices { get; }
        public List<ushort> Indexes { get; }

        public ChunkFaceVertex(List<VertexSolidBlock> vertices, List<ushort> indexes)
        {
            Vertices = vertices;
            Indexes = indexes;
        }
    }
}
