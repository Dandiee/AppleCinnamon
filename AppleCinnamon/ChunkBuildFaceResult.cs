using System.Collections.Generic;
using AppleCinnamon.Vertices;

namespace AppleCinnamon
{
    public class ChunkBuildFaceResult
    {
        public List<VertexSolidBlock> Vertices { get; }
        public List<ushort> Indexes { get; }

        public ChunkBuildFaceResult()
        {
            Vertices = new List<VertexSolidBlock>();
            Indexes = new List<ushort>();
        }
    }
}