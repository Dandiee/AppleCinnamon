using AppleCinnamon.Common;
using AppleCinnamon.Settings;
using AppleCinnamon.Vertices;
using SharpDX;
using Device = SharpDX.Direct3D11.Device;

namespace AppleCinnamon.ChunkBuilders
{
    public static partial class ChunkDispatcher
    {
        public const float SingleSidedOffset = 0.1f;

        private static readonly Vector3[] SingleSidedOffsets =
        {
            Vector3.UnitY * SingleSidedOffset,
            Vector3.UnitY * -SingleSidedOffset,
            Vector3.UnitX * SingleSidedOffset,
            Vector3.UnitX * -SingleSidedOffset,
            Vector3.UnitZ * SingleSidedOffset,
            Vector3.UnitZ * -SingleSidedOffset,
        };

        private static BufferDefinition<VertexSprite> BuildSprite(Chunk chunk, Device device)
        {
            if (WorldSettings.IsChangeTrackingEnabled && !chunk.BuildingContext.IsSpriteChanged)
            {
                return chunk.Buffers.BufferSprite;
            }

            var numberOfFaces = chunk.BuildingContext.SpriteBlocks.Count * 2 + chunk.BuildingContext.SingleSidedSpriteBlocks.Count;
            if (numberOfFaces == 0) return null;

            var vertices = new VertexSprite[numberOfFaces * 4];
            var indexes = new uint[numberOfFaces * 12];

            var secondFaceOffset = chunk.BuildingContext.SpriteBlocks.Count;

            for (var n = 0; n < chunk.BuildingContext.SpriteBlocks.Count; n++)
            {
                var flatIndex = chunk.BuildingContext.SpriteBlocks[n];
                var index = chunk.FromFlatIndex(flatIndex);

                var vertexOffset = n * 4;
                var positionOffset = new Vector3(index.X, index.Y, index.Z);
                var voxel = chunk.GetVoxel(flatIndex);
                var definition = voxel.GetDefinition();

                AddSpriteFace(chunk, FaceBuildInfo.SpriteVertices.Left, positionOffset, voxel, definition.TextureIndexes.Faces[(byte)Face.Left], ref vertices, ref indexes, vertexOffset, n, 0);
                AddSpriteFace(chunk, FaceBuildInfo.SpriteVertices.Right, positionOffset, voxel, definition.TextureIndexes.Faces[(byte)Face.Right], ref vertices, ref indexes, vertexOffset, n, secondFaceOffset);
            }

            var thirdFaceOffset = secondFaceOffset * 2;

            for (var n = 0; n < chunk.BuildingContext.SingleSidedSpriteBlocks.Count; n++)
            {
                var flatIndex = chunk.BuildingContext.SingleSidedSpriteBlocks[n];
                var index = chunk.FromFlatIndex(flatIndex);

                var vertexOffset = n * 4;

                var voxel = chunk.GetVoxel(flatIndex);
                var positionOffset = new Vector3(index.X, index.Y, index.Z) + SingleSidedOffsets[(byte)voxel.Orientation];
                var definition = voxel.GetDefinition();
                var face = FaceBuildInfo.FaceVertices.Faces[(byte)voxel.Orientation];

                AddSpriteFace(chunk, face, positionOffset, voxel, definition.TextureIndexes.Faces[(byte)Face.Left], ref vertices, ref indexes, vertexOffset, n, thirdFaceOffset);
            }

            //return null;
            return new BufferDefinition<VertexSprite>(device, ref vertices, ref indexes);
        }

        private static void AddSpriteFace(Chunk chunk, Vector3[] faceOffsetVertices, Vector3 positionOffset, Voxel voxel, Int2 textureIndicies,
            ref VertexSprite[] vertices, ref uint[] indexes, int vertexOffset, int vertexIndex, int faceOffset)
        {
            for (var m = 0; m < faceOffsetVertices.Length; m++)
            {
                var position = faceOffsetVertices[m] + chunk.OffsetVector + positionOffset;
                var textureOffset = FaceBuildInfo.UvOffsetIndexes[m];
                vertices[vertexOffset + m + faceOffset * 4] = new VertexSprite(position, textureIndicies.X + textureOffset.X, textureIndicies.Y + textureOffset.Y, voxel.MetaData, voxel.CompositeLight);
            }

            var indexOffset = (vertexIndex * 6 * 2) + (faceOffset * 6 * 2);

            indexes[indexOffset + 0] = (uint)(vertexOffset + 0 + faceOffset * 4);
            indexes[indexOffset + 1] = (uint)(vertexOffset + 2 + faceOffset * 4);
            indexes[indexOffset + 2] = (uint)(vertexOffset + 3 + faceOffset * 4);
            indexes[indexOffset + 3] = (uint)(vertexOffset + 0 + faceOffset * 4);
            indexes[indexOffset + 4] = (uint)(vertexOffset + 1 + faceOffset * 4);
            indexes[indexOffset + 5] = (uint)(vertexOffset + 2 + faceOffset * 4);

            indexes[indexOffset + 6] = (uint)(vertexOffset + 2 + faceOffset * 4);
            indexes[indexOffset + 7] = (uint)(vertexOffset + 1 + faceOffset * 4);
            indexes[indexOffset + 8] = (uint)(vertexOffset + 0 + faceOffset * 4);
            indexes[indexOffset + 9] = (uint)(vertexOffset + 3 + faceOffset * 4);
            indexes[indexOffset + 10] = (uint)(vertexOffset + 2 + faceOffset * 4);
            indexes[indexOffset + 11] = (uint)(vertexOffset + 0 + faceOffset * 4);
        }
    }
}
