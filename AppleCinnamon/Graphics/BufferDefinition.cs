using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using AppleCinnamon.Vertices;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

namespace AppleCinnamon
{
    public sealed class BufferDefinition<TVertex>
        where TVertex : struct, IVertex
    {
        public readonly bool IsValid;
        public readonly int IndexCount;
        public Buffer VertexBuffer;
        public Buffer IndexBuffer;
        public VertexBufferBinding Binding;

        public BufferDefinition(Device device, ref TVertex[] vertices, ref uint[] indexes)
        {
            IsValid = indexes.Length > 0;
            IndexCount = indexes.Length;
            VertexBuffer = Buffer.Create(device, BindFlags.VertexBuffer, vertices, vertices.Length * default(TVertex).Size, ResourceUsage.Immutable);
            IndexBuffer = Buffer.Create(device, BindFlags.IndexBuffer, indexes, indexes.Length * sizeof(uint), ResourceUsage.Immutable);
            Binding = new VertexBufferBinding(VertexBuffer, default(TVertex).Size, 0);
            vertices = null;
            indexes = null;
        }

        public void Draw(Device device)
        {
            if (IsValid)
            {
                device.ImmediateContext.InputAssembler.SetVertexBuffers(0, Binding);
                device.ImmediateContext.InputAssembler.SetIndexBuffer(IndexBuffer, Format.R32_UInt, 0);
                device.ImmediateContext.DrawIndexed(IndexCount, 0, 0);
            }
        }

        public void Dispose(Device device)
        {
            //var q = Binding.Buffer == VertexBuffer;

            //IndexBuffer = Buffer.Create<uint>(device, BindFlags.VertexBuffer, Array.Empty<uint>());
            //VertexBuffer = Buffer.Create<TVertex>(device, BindFlags.IndexBuffer, Array.Empty<TVertex>());
            //Binding = new VertexBufferBinding(VertexBuffer, default(TVertex).Size, 0);

            

            IndexBuffer.Dispose();
            Utilities.Dispose(ref IndexBuffer);

            //Utilities.Dispose(ref VertexBuffer);

            VertexBuffer.Dispose();
            Utilities.Dispose(ref VertexBuffer);
            //device.ImmediateContext.ClearState();
            //device.ImmediateContext.Flush();
            //IndexBuffer?.Dispose(device);
            //VertexBuffer?.Dispose(device);

            IndexBuffer = null;
            VertexBuffer = null;

            Asd.counter2++;
        }

        public static int Lofasz;
        ~BufferDefinition()
        {
            Interlocked.Increment(ref Lofasz);
            //Debug.WriteLine(Lofasz.ToString());
        }
    }

    public static class Asd
    {
        public static int counter1 = 0;
        public static int counter2 = 0;
    }
}