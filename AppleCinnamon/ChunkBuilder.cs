using System.Runtime.CompilerServices;
using AppleCinnamon.Settings;
using AppleCinnamon.System;
using AppleCinnamon.Vertices;
using SharpDX;
using SharpDX.Direct3D11;

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

            var visibilityFlags = chunk.VisibilityFlags;
            foreach (var visibilityFlag in visibilityFlags)
            {

                var index = visibilityFlag.Key;

                var k = index / (Chunk.SizeXy * Chunk.Height);
                var j = (index - k * Chunk.SizeXy * Chunk.Height) / Chunk.SizeXy;
                var i = index - (k * Chunk.SizeXy * Chunk.Height + j * Chunk.SizeXy);

                var voxel = chunk.Voxels[index];
                var definition = VoxelDefinition.DefinitionByType[voxel.Block];
                var flag = visibilityFlag.Value;

                if ((flag & 1) == 1)
                {
                    var neighbor = chunk.GetLocalWithNeighbours(i, j + 1, k);
                    AddFace(Face.Top, i, j, k, verticesCube.Top, definition, chunk, neighbor);
                }

                if ((flag & 2) == 2)
                {
                    var neighbor = chunk.GetLocalWithNeighbours(i, j - 1, k);
                    AddFace(Face.Bottom, i, j, k, verticesCube.Bottom, definition, chunk, neighbor);
                }

                if ((flag & 4) == 4)
                {
                    var neighbor = chunk.GetLocalWithNeighbours(i - 1, j, k);
                    AddFace(Face.Left, i, j, k, verticesCube.Left, definition, chunk, neighbor);
                }

                if ((flag & 8) == 8)
                {
                    var neighbor = chunk.GetLocalWithNeighbours(i + 1, j, k);
                    AddFace(Face.Right, i, j, k, verticesCube.Right, definition, chunk, neighbor);
                }

                if ((flag & 16) == 16)
                {
                    var neighbor = chunk.GetLocalWithNeighbours(i, j, k - 1);
                    AddFace(Face.Front, i, j, k, verticesCube.Front, definition, chunk, neighbor);
                }

                if ((flag & 32) == 32)
                {
                    var neighbor = chunk.GetLocalWithNeighbours(i, j, k + 1);
                    AddFace(Face.Back, i, j, k, verticesCube.Back, definition, chunk, neighbor);
                }


            }




            var buffers = verticesCube.Transform(face =>
            {
                if (face.Indexes.Count > 0)
                {
                    return new FaceBuffer(
                        face.Indexes.Count,
                        Buffer.Create(device, BindFlags.VertexBuffer, face.Vertices.ToArray()),
                        Buffer.Create(device, BindFlags.IndexBuffer, face.Indexes.ToArray())
                    );
                }

                return null;
            });

            var waterBuffer = AddWaterFace(chunk, device);

            chunk.SetBuffers(buffers, waterBuffer);
        }

        private FaceBuffer AddWaterFace(Chunk chunk, Device device)
        {
            if (chunk.TopMostWaterVoxels.Count == 0)
            {
                return null;
            }

            var topOffsetVertices = FaceVertices[Face.Top];
            var vertices = new VertexSolidBlock[chunk.TopMostWaterVoxels.Count * 4];
            var indexes = new ushort[chunk.TopMostWaterVoxels.Count * 6];
            var waterTexture = VoxelDefinition.Water.Textures[Face.Top];

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
                    var textureUv = UvOffsets[m] + waterTexture;

                    vertices[vertexOffset + m] =
                        new VertexSolidBlock(position, textureUv, 0, light);
                }

                var indexOffset = n * 6;

                indexes[indexOffset + 0] = (ushort)(vertexOffset + 0);
                indexes[indexOffset + 1] = (ushort)(vertexOffset + 2);
                indexes[indexOffset + 2] = (ushort)(vertexOffset + 3);
                indexes[indexOffset + 3] = (ushort)(vertexOffset + 0);
                indexes[indexOffset + 4] = (ushort)(vertexOffset + 1);
                indexes[indexOffset + 5] = (ushort)(vertexOffset + 2);
            }

            return new FaceBuffer(
                indexes.Length, 
                Buffer.Create(device, BindFlags.VertexBuffer, vertices),
                Buffer.Create(device, BindFlags.IndexBuffer, indexes));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsFaceVisible(VoxelDefinition definition, VoxelDefinition neighbourDefinition)
        {
            return !definition.IsUnitSized || neighbourDefinition.IsTransparent || !neighbourDefinition.IsUnitSized;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddFace(Face face, int relativeIndexX, int relativeIndexY, int relativeIndexZ, ChunkBuildFaceResult faceResult, VoxelDefinition definition, Chunk chunk, Voxel neighbor)
        {
            var faceVertices = FaceVertices[face];
            var textureUv = definition.Textures[face];
            var aoIndexes = AmbientIndexes[face];
            var vertexIndex = faceResult.Vertices.Count - 1;

            for (var i = 0; i < 4; i++)
            {
                var position = (faceVertices[i] * definition.Size) + definition.Translation + chunk.OffsetVector +
                               new Vector3(relativeIndexX, relativeIndexY, relativeIndexZ);
                var uvCoordinate = UvOffsets[i] + textureUv;
                var light = neighbor.Lightness;
                var denominator = 1f;
                var ambientOcclusion = 0;

                foreach (var index in aoIndexes[i])
                {
                    var aoFriend = chunk.GetLocalWithNeighbours(relativeIndexX + index.X, relativeIndexY + index.Y, relativeIndexZ + index.Z);
                    var aoFriendDefinition = aoFriend.GetDefinition();

                    if (index.X != 0 && aoFriendDefinition.IsTransmittance.X || index.Y != 0 && aoFriendDefinition.IsTransmittance.Y)
                    {
                        light += aoFriend.Lightness;
                        denominator++;
                    }
                    else ambientOcclusion++;
                }

                faceResult.Vertices.Add(new VertexSolidBlock(position, uvCoordinate, ambientOcclusion, light / denominator));
            }

            faceResult.Indexes.Add((ushort)(vertexIndex + 1));
            faceResult.Indexes.Add((ushort)(vertexIndex + 3));
            faceResult.Indexes.Add((ushort)(vertexIndex + 4));
            faceResult.Indexes.Add((ushort)(vertexIndex + 1));
            faceResult.Indexes.Add((ushort)(vertexIndex + 2));
            faceResult.Indexes.Add((ushort)(vertexIndex + 3));
        }
    }
}
