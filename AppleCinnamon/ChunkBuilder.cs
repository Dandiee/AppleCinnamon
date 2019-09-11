using System.Linq;
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

            var vertices = verticesCube.GetAll().ToList();

            for (var i = 0; i != Chunk.SizeXy; i++)
            {
                for (var j = 0; j != Chunk.Height; j++)
                {
                    for (var k = 0; k != Chunk.SizeXy; k++)
                    {
                        var voxel = chunk.Voxels[i + Chunk.SizeXy * (j + Chunk.Height * k)];

                        if (voxel.Block > 0)
                        {
                            var definition = VoxelDefinition.DefinitionByType[voxel.Block];

                            foreach (var currentFace in vertices)
                            {
                                var face = currentFace.Key;

                                // TODO: this is the most expensive line in the code.
                                var neighbor = chunk.GetLocalWithNeighbours(i + currentFace.Value.Direction.X,
                                    j + currentFace.Value.Direction.Y, k + currentFace.Value.Direction.Z);

                                var neighborDefinition = VoxelDefinition.DefinitionByType[neighbor.Block];

                                if (IsFaceVisible(definition, neighborDefinition))
                                {
                                    AddFace(face, i, j, k, currentFace.Value, definition, chunk, neighbor);
                                }
                            }
                        }
                    }
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

            chunk.SetBuffers(buffers);
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
