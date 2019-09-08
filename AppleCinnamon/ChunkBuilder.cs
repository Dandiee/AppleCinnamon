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
        Chunk BuildChunk(Device device, Chunk chunk);
    }

    public sealed class ChunkBuilder : IChunkBuilder
    {
        private static readonly Vector3 TopLefFro = new Vector3(-.5f, +.5f, -.5f);
        private static readonly Vector3 TopRigFro = new Vector3(+.5f, +.5f, -.5f);
        private static readonly Vector3 TopLefBac = new Vector3(-.5f, +.5f, +.5f);
        private static readonly Vector3 TopRigBac = new Vector3(+.5f, +.5f, +.5f);
        private static readonly Vector3 BotLefFro = new Vector3(-.5f, -.5f, -.5f);
        private static readonly Vector3 BotLefBac = new Vector3(-.5f, -.5f, +.5f);
        private static readonly Vector3 BotRigFro = new Vector3(+.5f, -.5f, -.5f);
        private static readonly Vector3 BotRigBac = new Vector3(+.5f, -.5f, +.5f);

      
        public static readonly Cube<Int3> Directions =
            new Cube<Int3>(Int3.UnitY, -Int3.UnitY, -Int3.UnitX, Int3.UnitX, -Int3.UnitZ, Int3.UnitZ);

        public static readonly Cube<Vector3[]> FaceVertices =
            new Cube<Vector3[]>(
                new[] { TopLefFro, TopRigFro, TopRigBac, TopLefBac },
                new[] { BotRigFro, BotLefFro, BotLefBac, BotRigBac },
                new[] { TopLefFro, TopLefBac, BotLefBac, BotLefFro },
                new[] { TopRigBac, TopRigFro, BotRigFro, BotRigBac },
                new[] { TopRigFro, TopLefFro, BotLefFro, BotRigFro },
                new[] { TopLefBac, TopRigBac, BotRigBac, BotLefBac }
            );

        public const float UvStep = 1 / 16f;

        private static readonly Vector2[] UvOffsets = {Vector2.Zero, new Vector2(1 / 16f, 0), new Vector2(UvStep, UvStep), new Vector2(0, UvStep) };

        public static readonly Cube<Int3[][]> AmbientIndexes = new Cube<Int3[][]>(
            new[]
            {
                new[] {new Int3(0, 1, -1), new Int3(-1, 1, -1), new Int3(-1, 1, 0)},
                new[] {new Int3(1, 1, 0), new Int3(1, 1, -1), new Int3(0, 1, -1)},
                new[] {new Int3(0, 1, 1), new Int3(1, 1, 1), new Int3(1, 1, 0)},
                new[] {new Int3(-1, 1, 0), new Int3(-1, 1, 1), new Int3(0, 1, 1)}
            },
            new[]
            {
                new[] {new Int3(1, -1, 0), new Int3(1, -1, -1), new Int3(0, -1, -1)},
                new[] {new Int3(0, -1, -1), new Int3(-1, -1, -1), new Int3(-1, -1, 0)},
                new[] {new Int3(-1, -1, 0), new Int3(-1, -1, 1), new Int3(0, -1, 1)},
                new[] {new Int3(0, -1, 1), new Int3(1, -1, 1), new Int3(1, -1, 0)}
            },
            new[]
            {
                new[] {new Int3(-1, 0, -1), new Int3(-1, 1, -1), new Int3(-1, 1, 0)},
                new[] {new Int3(-1, 1, 0), new Int3(-1, 1, 1), new Int3(-1, 0, 1)},
                new[] {new Int3(-1, 0, 1), new Int3(-1, -1, 1), new Int3(-1, -1, 0)},
                new[] {new Int3(-1, -1, 0), new Int3(-1, -1, -1), new Int3(-1, 0, -1)}
            },
            new[]
            {
                new[] {new Int3(1, 1, 0), new Int3(1, 1, 1), new Int3(1, 0, 1)},
                new[] {new Int3(1, 0, -1), new Int3(1, 1, -1), new Int3(1, 1, 0)},
                new[] {new Int3(1, -1, 0), new Int3(1, -1, -1), new Int3(1, 0, -1)},
                new[] {new Int3(1, 0, 1), new Int3(1, -1, 1), new Int3(1, -1, 0)}
            },
            new[]
            {
                new[] {new Int3(0, 1, -1), new Int3(1, 1, -1), new Int3(1, 0, -1)},
                new[] {new Int3(-1, 0, -1), new Int3(-1, 1, -1), new Int3(0, 1, -1)},
                new[] {new Int3(0, -1, -1), new Int3(-1, -1, -1), new Int3(-1, 0, -1)},
                new[] {new Int3(1, 0, -1), new Int3(1, -1, -1), new Int3(0, -1, -1)}
            },
            new[]
            {
                new[] {new Int3(-1, 0, 1), new Int3(-1, 1, 1), new Int3(0, 1, 1)},
                new[] {new Int3(0, 1, 1), new Int3(1, 1, 1), new Int3(1, 0, 1)},
                new[] {new Int3(1, 0, 1), new Int3(1, -1, 1), new Int3(0, -1, 1)},
                new[] {new Int3(0, -1, 1), new Int3(-1, -1, 1), new Int3(-1, 0, 1)}
            }
        );

        public Chunk BuildChunk(Device device, Chunk chunk)
        {
            var verticesCube = Cube<ChunkBuildFaceResult>.CreateDefault(() => new ChunkBuildFaceResult());
            var vertices = verticesCube.GetAll().ToList();

            for (var i = 0; i != Chunk.Size.X; i++)
            {
                for (var j = 0; j != Chunk.Size.Y; j++)
                {
                    for (var k = 0; k != Chunk.Size.Z; k++)
                    {
                        var relativeIndex = new Int3(i, j, k);
                        var voxel = chunk.GetLocalVoxel(i, j, k);

                        if (voxel.Block > 0)
                        {
                            var definition = VoxelDefinition.DefinitionByType[voxel.Block];

                            foreach (var currentFace in vertices)
                            {
                                var face = currentFace.Key;
                                var dir = Directions[face];

                                var neighbor = chunk.GetLocalWithNeighbours(i + dir.X, j + dir.Y, k + dir.Z);
                                if (neighbor.Block == 0)
                                {
                                    AddFace(face, relativeIndex, currentFace.Value, definition, chunk, neighbor, dir);
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

            return chunk;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddFace(Face face, Int3 relativeIndex, ChunkBuildFaceResult faceResult, VoxelDefinition definition, Chunk chunk, Voxel neighbor, Int3 dir)
        {
            var faceVertices = FaceVertices[face];
            var textureUv = definition.Textures[face];
            var aoIndexes = AmbientIndexes[face];
            var vertexIndex = faceResult.Vertices.Count - 1;

            for (var i = 0; i < 4; i++)
            {
                var position = faceVertices[i].Add(relativeIndex) + chunk.OffsetVector;
                var uvCoordinate = UvOffsets[i] + textureUv;
                var light = neighbor.Lightness;
                var denominator = 1f;
                var ambientOcclusion = 0;

                foreach (var index in aoIndexes[i])
                {
                    var aoFriend = chunk.GetLocalWithNeighbours(relativeIndex.X + index.X, relativeIndex.Y + index.Y, relativeIndex.Z + index.Z);
                    if (aoFriend.Block == 0)
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
