﻿using SharpDX.Direct3D11;

namespace AppleCinnamon
{
    public class FaceBuffer
    {
        public int IndexCount { get; }
        public Buffer VertexBuffer { get; }
        public Buffer IndexBuffer { get; }

        public FaceBuffer(int indexCount, Buffer vertexBuffer, Buffer indexBuffer)
        {
            IndexCount = indexCount;
            VertexBuffer = vertexBuffer;
            IndexBuffer = indexBuffer;
        }
    }
}