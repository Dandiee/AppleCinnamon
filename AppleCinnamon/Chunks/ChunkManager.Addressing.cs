using System.Collections.Generic;
using AppleCinnamon.Helper;
using SharpDX;

namespace AppleCinnamon
{
    public partial class ChunkManager
    {
        public bool TryGetVoxelAddress(Int3 absoluteVoxelIndex, out VoxelChunkAddress address)
        {
            if (!TryGetChunkIndexByAbsoluteVoxelIndex(absoluteVoxelIndex, out var chunkIndex))
            {
                address = VoxelChunkAddress.Zero;
                return false;
            }

            if (!TryGetChunk(chunkIndex, out var chunk))
            {
                address = VoxelChunkAddress.Zero;
                return false;
            }

            var voxelIndex = new Int3(absoluteVoxelIndex.X & Chunk.SizeXy - 1, absoluteVoxelIndex.Y, absoluteVoxelIndex.Z & Chunk.SizeXy - 1);
            address = new VoxelChunkAddress(chunk, voxelIndex);
            return true;
        }

        public static bool TryGetChunkIndexByAbsoluteVoxelIndex(Int3 absoluteVoxelIndex, out Int2 chunkIndex)
        {
            if (absoluteVoxelIndex.Y < 0)
            {
                chunkIndex = Int2.Zero;
                return false;
            }

            chunkIndex = new Int2(
                absoluteVoxelIndex.X < 0
                    ? ((absoluteVoxelIndex.X + 1) / Chunk.SizeXy) - 1
                    : absoluteVoxelIndex.X / Chunk.SizeXy,
                absoluteVoxelIndex.Z < 0
                    ? ((absoluteVoxelIndex.Z + 1) / Chunk.SizeXy) - 1
                    : absoluteVoxelIndex.Z / Chunk.SizeXy);
            return true;
        }

        public bool TryGetVoxel(Int3 absoluteIndex, out Voxel voxel)
        {
            if (!TryGetVoxelAddress(absoluteIndex, out var address))
            {
                voxel = Voxel.Bedrock;
                return false;
            }

            voxel = address.Chunk.CurrentHeight <= address.RelativeVoxelIndex.Y
                ? Voxel.SunBlock
                : address.Chunk.GetVoxel(address.RelativeVoxelIndex);
            return true;
        }

        public bool TryGetChunk(Int2 chunkIndex, out Chunk chunk)
        {
            if (Chunks.TryGetValue(chunkIndex, out var currentChunk))
            {
                chunk = currentChunk;
                return true;
            }


            chunk = null;
            return false;
        }

        public static IEnumerable<Int2> GetSurroundingChunks(int size)
        {
            yield return new Int2();

            for (var i = 1; i < size + 2; i++)
            {
                var cursor = new Int2(i * -1);

                foreach (var direction in AnnoyingMappings.ChunkManagerDirections)
                {
                    for (var j = 1; j < i * 2 + 1; j++)
                    {
                        cursor = cursor + direction;
                        yield return cursor;
                    }
                }
            }
        }

        public void SetBlock(Int3 absoluteIndex, byte voxel) => _chunkUpdater.SetVoxel(absoluteIndex, voxel);
    }
}
