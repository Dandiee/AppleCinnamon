using System;
using System.Collections.Generic;
using System.Linq;
using AppleCinnamon.Vertices;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

namespace AppleCinnamon
{
    public sealed class BoxDrawer
    {
        private readonly Graphics _graphics;
        private readonly Effect _effect;
        private readonly EffectPass _pass;

        private Buffer _vertexBuffer;
        private Buffer _indexBuffer;
        private int _boxesCount;

        public BoxDrawer(Graphics graphics)
        {
            _graphics = graphics;
            _effect = new Effect(_graphics.Device,
                ShaderBytecode.CompileFromFile("Content/Effect/BasicEffect.fx", "fx_5_0"));

            _pass = _effect.GetTechniqueByIndex(0).GetPassByIndex(0);
        }

        public void Update(Camera camera)
        {
            _effect.GetVariableByName("WorldViewProjection").AsMatrix().SetMatrix(camera.WorldViewProjection);
        }

        public void Draw(ChunkManager chunkManager, Camera camera)
        {
            if (!chunkManager.IsInitialized)
            {
                return;
            }

            UpdateBuffers(_graphics.Device, chunkManager, camera);

            if (_boxesCount > 0)
            {
                using (var inputLayout = new InputLayout(_graphics.Device, _pass.Description.Signature,
                    VertexPositionColor.InputElements))
                {

                    _graphics.Device.ImmediateContext.InputAssembler.InputLayout = inputLayout;
                    _graphics.Device.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.LineList;
                    _graphics.Device.ImmediateContext.InputAssembler.SetIndexBuffer(_indexBuffer, Format.R16_UInt, 0);
                    _graphics.Device.ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_vertexBuffer, VertexPositionColor.Size, 0));
                    _pass.Apply(_graphics.Device.ImmediateContext);

                    _graphics.Device.ImmediateContext.DrawIndexed(_boxesCount * 24, 0, 0);
                }
            }
        }


        private void UpdateBuffers(Device device, ChunkManager chunkManager, Camera camera)
        {
            if (!_indexBuffer?.IsDisposed ?? false)
            {
                _indexBuffer.Dispose();
            }

            if (!_vertexBuffer?.IsDisposed ?? false)
            {
                _vertexBuffer.Dispose();
            }


            var boundingBoxes = new List<(BoundingBox box, Color color)>();

            if (Game.ShowChunkBoundingBoxes)
            {
                boundingBoxes.AddRange(chunkManager.Chunks.Values.Select(s =>
                    new ValueTuple<BoundingBox, Color>(s.BoundingBox, Color.Red)));
            }

            if (camera.CurrentCursor != null)
            {
                boundingBoxes.Add(new ValueTuple<BoundingBox, Color>(camera.CurrentCursor.BoundingBox, Color.Yellow));
            }

            _boxesCount = boundingBoxes.Count;
            if (_boxesCount == 0)
            {
                return;
            }

            var vertices = new VertexPositionColor[_boxesCount * 8];
            var indexes = new ushort[_boxesCount * 24];

            var boxOffset = 0;
            foreach (var box in boundingBoxes)
            {
                AddBox(vertices, indexes, box.box, box.color, ref boxOffset);
            }

            _indexBuffer = Buffer.Create(device, BindFlags.IndexBuffer, indexes);
            _vertexBuffer = Buffer.Create(device, BindFlags.VertexBuffer, vertices);
        }

        private void AddBox(VertexPositionColor[] vertices, ushort[] indexes, BoundingBox box, Color color, ref int boxOffset)
        {
            var vertexOffset = boxOffset * 8;
            var indexOffset = boxOffset * 24;

            vertices[vertexOffset + 0] = new VertexPositionColor(new Vector3(box.Minimum.X, box.Maximum.Y, box.Minimum.Z), color.ToColor3());
            vertices[vertexOffset + 1] = new VertexPositionColor(new Vector3(box.Maximum.X, box.Maximum.Y, box.Minimum.Z), color.ToColor3());
            vertices[vertexOffset + 2] = new VertexPositionColor(new Vector3(box.Minimum.X, box.Maximum.Y, box.Maximum.Z), color.ToColor3());
            vertices[vertexOffset + 3] = new VertexPositionColor(new Vector3(box.Maximum.X, box.Maximum.Y, box.Maximum.Z), color.ToColor3());

            vertices[vertexOffset + 4] = new VertexPositionColor(new Vector3(box.Minimum.X, box.Minimum.Y, box.Minimum.Z), color.ToColor3());
            vertices[vertexOffset + 5] = new VertexPositionColor(new Vector3(box.Minimum.X, box.Minimum.Y, box.Maximum.Z), color.ToColor3());
            vertices[vertexOffset + 6] = new VertexPositionColor(new Vector3(box.Maximum.X, box.Minimum.Y, box.Minimum.Z), color.ToColor3());
            vertices[vertexOffset + 7] = new VertexPositionColor(new Vector3(box.Maximum.X, box.Minimum.Y, box.Maximum.Z), color.ToColor3());

            indexes[indexOffset + 0] = (ushort)(vertexOffset + 0);
            indexes[indexOffset + 1] = (ushort)(vertexOffset + 1);
            indexes[indexOffset + 2] = (ushort)(vertexOffset + 1);
            indexes[indexOffset + 3] = (ushort)(vertexOffset + 3);
            indexes[indexOffset + 4] = (ushort)(vertexOffset + 3);
            indexes[indexOffset + 5] = (ushort)(vertexOffset + 2);
            indexes[indexOffset + 6] = (ushort)(vertexOffset + 2);
            indexes[indexOffset + 7] = (ushort)(vertexOffset + 0);

            indexes[indexOffset + 8] =  (ushort)(vertexOffset + 4);
            indexes[indexOffset + 9] =  (ushort)(vertexOffset + 6);
            indexes[indexOffset + 10] = (ushort)(vertexOffset + 6);
            indexes[indexOffset + 11] = (ushort)(vertexOffset + 7);
            indexes[indexOffset + 12] = (ushort)(vertexOffset + 7);
            indexes[indexOffset + 13] = (ushort)(vertexOffset + 5);
            indexes[indexOffset + 14] = (ushort)(vertexOffset + 5);
            indexes[indexOffset + 15] = (ushort)(vertexOffset + 4);
                                        
            indexes[indexOffset + 16] = (ushort)(vertexOffset + 0);
            indexes[indexOffset + 17] = (ushort)(vertexOffset + 4);
            indexes[indexOffset + 18] = (ushort)(vertexOffset + 1);
            indexes[indexOffset + 19] = (ushort)(vertexOffset + 6);
            indexes[indexOffset + 20] = (ushort)(vertexOffset + 2);
            indexes[indexOffset + 21] = (ushort)(vertexOffset + 5);
            indexes[indexOffset + 22] = (ushort)(vertexOffset + 3);
            indexes[indexOffset + 23] = (ushort)(vertexOffset + 7);

            boxOffset++;
        }

    }
}
