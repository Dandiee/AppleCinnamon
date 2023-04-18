using AppleCinnamon.Chunks;
using AppleCinnamon.Graphics;
using AppleCinnamon.Graphics.Verticies;
using SharpDX;
using Device = SharpDX.Direct3D11.Device;

namespace AppleCinnamon.ChunkBuilder
{
    public static partial class ChunkDispatcher
    {
        private static readonly Vector2[] WaterUvOffsets = { Vector2.Zero, new(1, 0), new(1, 1 / 32f), new(0, 1 / 32f) };

        private static BufferDefinition<VertexWater> BuildWater(Chunk chunk, Device device)
        {
            if (chunk.BuildingContext.TopMostWaterVoxels.Count == 0)
            {
                return null;
            }

            var topOffsetVertices = FaceBuildInfo.FaceVertices.Top;
            var vertices = new VertexWater[chunk.BuildingContext.TopMostWaterVoxels.Count * 4];
            var indexes = new uint[chunk.BuildingContext.TopMostWaterVoxels.Count * 6 * 2];

            for (var n = 0; n < chunk.BuildingContext.TopMostWaterVoxels.Count; n++)
            {
                var flatIndex = chunk.BuildingContext.TopMostWaterVoxels[n];
                var index = chunk.FromFlatIndex(flatIndex);

                var vertexOffset = n * 4;
                var positionOffset = new Vector3(index.X, index.Y - 0.1f, index.Z);
                var voxel = chunk.GetVoxel(flatIndex);

                for (var m = 0; m < topOffsetVertices.Length; m++)
                {
                    var position = topOffsetVertices[m] + chunk.OffsetVector + positionOffset;
                    var textureUv = WaterUvOffsets[m];

                    vertices[vertexOffset + m] = new VertexWater(position, textureUv, voxel.CompositeLight);
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

            return new BufferDefinition<VertexWater>(device, ref vertices, ref indexes);
        }
    }
}
