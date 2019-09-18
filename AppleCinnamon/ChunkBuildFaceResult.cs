using System.Collections.Generic;
using SharpDX;

namespace AppleCinnamon
{
    public class ChunkBuildFaceResult
    {
        //public List<VertexSolidBlock> Vertices { get; }
        public List<uint> Vertices { get; }
        public List<ushort> Indexes { get; }
        public Int3 Direction { get; }

        public ChunkBuildFaceResult(Int3 direction)
        {
            Direction = direction;
            //Vertices = new List<VertexSolidBlock>();
            Vertices = new List<uint>();
            Indexes = new List<ushort>();
        }
    }
}