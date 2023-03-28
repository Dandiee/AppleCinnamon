using System.Collections.Generic;
using System.Linq;
using AppleCinnamon.Helper;
using AppleCinnamon.Settings;
using SharpDX;
using Device = SharpDX.Direct3D11.Device;

namespace AppleCinnamon
{
    public static partial class ChunkBuilder
    {
        public static void BuildChunk(Chunk chunk, Device device)
        {
            if (!WorldSettings.IsChangeTrackingEnabled || chunk.BuildingContext.IsChanged)
            {
                if (chunk.Buffers == null)
                {
                    chunk.Buffers = new ChunkBuffers();
                }

                if (!WorldSettings.IsChangeTrackingEnabled || chunk.BuildingContext.IsSolidChanged)
                {
                    chunk.Buffers.BufferSolid?.Dispose(device);
                    chunk.Buffers.BufferSolid = BuildSolid(chunk, device);
                }

                if (!WorldSettings.IsChangeTrackingEnabled || chunk.BuildingContext.IsWaterChanged)
                {
                    chunk.Buffers.BufferWater?.Dispose(device);
                    chunk.Buffers.BufferWater = BuildWater(chunk, device);
                }

                if (!WorldSettings.IsChangeTrackingEnabled || chunk.BuildingContext.IsSpriteChanged)
                {
                    chunk.Buffers.BufferSprite?.Dispose(device);
                    chunk.Buffers.BufferSprite = BuildSprite(chunk, device);
                }
                
                chunk.BuildingContext.IsSpriteChanged = false;
                chunk.BuildingContext.IsWaterChanged = false;
                chunk.BuildingContext.IsSolidChanged = false;
            }
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
}
