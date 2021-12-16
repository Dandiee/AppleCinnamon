using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppleCinnamon.Helper;

namespace AppleCinnamon
{
    public static class AnnoyingMappings
    {
        public static readonly IReadOnlyCollection<Int2> ChunkManagerDirections = new[]
        {
            new Int2(1, 0),
            new Int2(0, 1),
            new Int2(-1, 0),
            new Int2(0, -1)
        };

        public static readonly IReadOnlyDictionary<int, Face[]> GlobalLightFinalizerCornerMapping =
            new Dictionary<int, Face[]>
            {
                [Chunk.GetChunkFlatIndex(-1, -1)] = new[] { Face.Right, Face.Back },
                [Chunk.GetChunkFlatIndex(1, -1)] = new[] { Face.Left, Face.Back },
                [Chunk.GetChunkFlatIndex(1, 1)] = new[] { Face.Left, Face.Front },
                [Chunk.GetChunkFlatIndex(-1, 1)] = new[] { Face.Right, Face.Front },
            };
    }
}
