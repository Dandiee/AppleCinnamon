using System.Collections.Generic;
using AppleCinnamon.Vertices;
using SharpDX;

namespace AppleCinnamon
{
    public class ChunkBuildFaceResult
    {
        //public List<VertexSolidBlock> Vertices { get; }
        public List<TinySolidBlock> Vertices { get; }
        public List<ushort> Indexes { get; }
        public Int3 Direction { get; }

        public ChunkBuildFaceResult(Int3 direction)
        {
            Direction = direction;
            //Vertices = new List<VertexSolidBlock>();
            Vertices = new List<TinySolidBlock>();
            Indexes = new List<ushort>();
        }
    }
}