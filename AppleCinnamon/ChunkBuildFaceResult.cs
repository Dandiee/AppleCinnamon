using System.Collections.Generic;
using AppleCinnamon.Vertices;
using SharpDX;

namespace AppleCinnamon
{
    public class ChunkBuildFaceResult
    {
        public VertexSolidBlock[] Vertices { get; }
        public ushort[] Indexes { get; }
        public Int3 Direction { get; }

        public ChunkBuildFaceResult(Int3 direction, int voxelCount)
        {
            Direction = direction;
            Vertices = new VertexSolidBlock[voxelCount * 4];
            Indexes = new ushort[voxelCount * 6];
        }

    }
}