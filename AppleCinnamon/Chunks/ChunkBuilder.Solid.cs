using System.Linq;
using System.Runtime.CompilerServices;
using AppleCinnamon.Helper;
using AppleCinnamon.Pipeline;
using AppleCinnamon.Settings;
using AppleCinnamon.Vertices;
using SharpDX;
using SharpDX.Direct3D11;

namespace AppleCinnamon
{
    public static partial class ChunkBuilder
    {
        private static BufferDefinition<VertexSolidBlock> BuildSolid(Chunk chunk, Device device)
        {
            var faces = GetChunkFaces(chunk);
            var visibleFacesCount = chunk.BuildingContext.Faces.Sum(s => s.VoxelCount);
            if (visibleFacesCount == 0)
            {
                return null;
            }

            var vertices = new VertexSolidBlock[visibleFacesCount * 4];

            var indexes = new uint[visibleFacesCount * 6];

            foreach (var visibilityFlag in chunk.BuildingContext.VisibilityFlags)
            {
                var flatIndex = visibilityFlag.Key;
                var index = flatIndex.ToIndex(chunk.CurrentHeight);

                var voxel = chunk.GetVoxel(flatIndex);
                var definition = voxel.GetDefinition();

                var voxelPositionOffset = definition.Offset + chunk.OffsetVector + new Vector3(index.X, index.Y, index.Z);


                foreach (var faceInfo in faces.Faces)
                {
                    if (((byte)visibilityFlag.Value & faceInfo.BuildInfo.DirectionFlag) == faceInfo.BuildInfo.DirectionFlag)
                    {
                        var neighbor = chunk.GetLocalWithNeighbor(index.X + faceInfo.BuildInfo.Direction.X, index.Y + faceInfo.BuildInfo.Direction.Y, index.Z + faceInfo.BuildInfo.Direction.Z);
                        AddSolidFace(faceInfo, voxel, index.X, index.Y, index.Z, vertices, indexes, definition, chunk, neighbor, voxelPositionOffset);
                    }
                }
            }

            return new BufferDefinition<VertexSolidBlock>(device, vertices, indexes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AddSolidFace(SolidFaceInfo faceInfo, Voxel voxel, int relativeIndexX, int relativeIndexY, int relativeIndexZ, VertexSolidBlock[] vertices,
            uint[] indexes, VoxelDefinition definition, Chunk chunk, Voxel neighbor, Vector3 voxelPositionOffset)
        {
            // Face specific base variables
            var textureUv = definition.TextureIndexes.Faces[(byte)faceInfo.BuildInfo.Face];
            var offset = faceInfo.Offset + faceInfo.ProcessedVoxels;
            var vertexIndex = offset * 4;
            var indexIndex = offset * 6;

            // Visit all ambient neighbors
            foreach (var vertexInfo in faceInfo.BuildInfo.VerticesInfo)
            {
                var position = new Vector3(
                    vertexInfo.Position.X * definition.Size.X + voxelPositionOffset.X,
                    vertexInfo.Position.Y * definition.Size.Y + voxelPositionOffset.Y,
                    vertexInfo.Position.Z * definition.Size.Z + voxelPositionOffset.Z);

                byte totalSunlight = 0;
                byte totalCustomLight = 0;
                var numberOfAmbientNeighbors = 0;

                foreach (var ambientIndex in vertexInfo.AmbientOcclusionNeighbors)
                {
                    var ambientNeighborVoxel = chunk.GetLocalWithNeighbor(relativeIndexX + ambientIndex.X, relativeIndexY + ambientIndex.Y, relativeIndexZ + ambientIndex.Z, out var addr);
                    var ambientNeighborDefinition = ambientNeighborVoxel.GetDefinition();

                    if (!ambientNeighborDefinition.IsBlock)
                    {
                        totalSunlight += ambientNeighborVoxel.Sunlight;
                        totalCustomLight += ambientNeighborVoxel.EmittedLight;
                    }
                    else if (ambientNeighborDefinition.IsUnitSized)
                    {
                        numberOfAmbientNeighbors++;
                    }
                }

                var hue = (definition.HueFaces & faceInfo.Direction) == faceInfo.Direction ? voxel.MetaData : (byte)0;

                vertices[vertexIndex + vertexInfo.Index] = new VertexSolidBlock(position, textureUv.X + vertexInfo.TextureIndex.X,
                    textureUv.Y + vertexInfo.TextureIndex.Y, neighbor.Sunlight, neighbor.EmittedLight, totalSunlight,
                    numberOfAmbientNeighbors, hue, totalCustomLight);
            }

            indexes[indexIndex] = (uint)vertexIndex;
            indexes[indexIndex + 1] = (uint)(vertexIndex + 2);
            indexes[indexIndex + 2] = (uint)(vertexIndex + 3);
            indexes[indexIndex + 3] = (uint)(vertexIndex + 0);
            indexes[indexIndex + 4] = (uint)(vertexIndex + 1);
            indexes[indexIndex + 5] = (uint)(vertexIndex + 2);
            faceInfo.ProcessedVoxels++;
        }

        private static Cube<SolidFaceInfo> GetChunkFaces(Chunk chunk)
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

            var result = new Cube<SolidFaceInfo>(
                new SolidFaceInfo(topOffset, topCount, FaceBuildInfo.Top, VisibilityFlag.Top),
                new SolidFaceInfo(botOffset, botCount, FaceBuildInfo.Bottom, VisibilityFlag.Bottom),
                new SolidFaceInfo(lefOffset, lefCount, FaceBuildInfo.Left, VisibilityFlag.Left),
                new SolidFaceInfo(rigOffset, rigCount, FaceBuildInfo.Right, VisibilityFlag.Right),
                new SolidFaceInfo(froOffset, froCount, FaceBuildInfo.Front, VisibilityFlag.Front),
                new SolidFaceInfo(bacOffset, bacCount, FaceBuildInfo.Back, VisibilityFlag.Back));

            return result;
        }

        private sealed class SolidFaceInfo
        {
            public readonly int Offset;
            public readonly FaceBuildInfo BuildInfo;
            public int ProcessedVoxels;
            public readonly VisibilityFlag Direction;

            public SolidFaceInfo(int offset, int count, FaceBuildInfo buildInfo, VisibilityFlag direction)
            {
                Offset = offset;
                BuildInfo = buildInfo;
                Direction = direction;
            }
        }
    }
}
