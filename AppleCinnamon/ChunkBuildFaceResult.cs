using System.Collections.Generic;
using AppleCinnamon.Vertices;
using SharpDX;

namespace AppleCinnamon
{
    public class ChunkBuildFaceResult
    {
        public List<VertexSolidBlock> Vertices { get; }
        public List<ushort> Indexes { get; }
        public Int3 Direction { get; }

        public VertexSolidBlock[] VertexArray { get; private set; }
        public ushort[] IndexArray { get; private set; }

        public ChunkBuildFaceResult(Int3 direction)
        {
            Direction = direction;
            Vertices = new List<VertexSolidBlock>();
            Indexes = new List<ushort>();
        }

        public void ToArray()
        {
            VertexArray = Vertices.ToArray();
            IndexArray = Indexes.ToArray();
        }
    }
}