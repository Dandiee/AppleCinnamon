using SharpDX.Direct3D11;

namespace AppleCinnamon
{
    public sealed class FaceBuffer
    {
        public int IndexCount { get; }
        public Buffer VertexBuffer { get; }
        public Buffer IndexBuffer { get; }
        public VertexBufferBinding Binding { get; }

        public FaceBuffer(int indexCount, int stride, Buffer vertexBuffer, Buffer indexBuffer)
        {
            IndexCount = indexCount;
            VertexBuffer = vertexBuffer;
            IndexBuffer = indexBuffer;
            Binding = new VertexBufferBinding(vertexBuffer, stride, 0);
        }
    }
}