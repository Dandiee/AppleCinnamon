using System;
using AppleCinnamon.Graphics.Verticies;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

namespace AppleCinnamon.Graphics;

public sealed class BufferDefinition<TVertex> : IDisposable
    where TVertex : struct, IVertex
{
    public readonly bool IsValid;
    public readonly int IndexCount;
    public readonly int VertexCount;
    public Buffer VertexBuffer;
    public Buffer IndexBuffer;
    public VertexBufferBinding Binding;

    public BufferDefinition(Device device, ref TVertex[] vertices, ref uint[] indexes)
    {
        IsValid = indexes.Length > 0;
        IndexCount = indexes.Length;
        VertexCount = vertices.Length;
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

    public void Dispose()
    {
        IndexBuffer.Dispose();
        VertexBuffer.Dispose();
        IndexBuffer = null;
        VertexBuffer = null;
    }
}