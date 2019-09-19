using System;
using System.Collections.Concurrent;
using AppleCinnamon.Vertices;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace AppleCinnamon
{
    public sealed class BoxDrawer
    {
        private readonly Graphics _graphics;
        private Effect _effect;

        private static readonly Vector3 TopLefFro = new Vector3(-.5f, +.5f, -.5f);
        private static readonly Vector3 TopRigFro = new Vector3(+.5f, +.5f, -.5f);
        private static readonly Vector3 TopLefBac = new Vector3(-.5f, +.5f, +.5f);
        private static readonly Vector3 TopRigBac = new Vector3(+.5f, +.5f, +.5f);
        private static readonly Vector3 BotLefFro = new Vector3(-.5f, -.5f, -.5f);
        private static readonly Vector3 BotLefBac = new Vector3(-.5f, -.5f, +.5f);
        private static readonly Vector3 BotRigFro = new Vector3(+.5f, -.5f, -.5f);
        private static readonly Vector3 BotRigBac = new Vector3(+.5f, -.5f, +.5f);

        private ConcurrentDictionary<string, BoxDetails> _boxes;
        private Buffer _vertexBuffer;
        private int _verticesCount;
        private bool _isDirty;

        private object lockObj = new object();


        public BoxDrawer(Graphics graphics)
        {
            _graphics = graphics;
            _boxes = new ConcurrentDictionary<string, BoxDetails>();
            _effect = new Effect(_graphics.Device,
                ShaderBytecode.CompileFromFile("Content/Effect/BasicEffect.fx", "fx_5_0"));
        }

        public void Set(string key, BoxDetails box)
        {
            lock (lockObj)
            {
                _boxes.AddOrUpdate(key, box, (k, prevBox) => box);
                //_boxes[key] = box;
                _isDirty = true;
            }
        }

        public void Remove(string key)
        {
            lock (lockObj)
            {
                if (_boxes.ContainsKey(key))
                {
                    if (!_boxes.TryRemove(key, out _))
                    {
                        throw new Exception("dgsdf");
                    }
                }

                _isDirty = true;
            }
        }

        public void Update(Camera camera)
        {
            _effect.GetVariableByName("WorldViewProjection").AsMatrix().SetMatrix(camera.WorldViewProjection);
        }

        public void Draw()
        {
            if (_boxes.Count == 0)
            {
                return;
            }

            if (_isDirty)
            {
                UpdateVertexBuffer(_graphics.Device);
                _isDirty = false;
            }

            if (_vertexBuffer != null && _verticesCount > 0)
            {

                using (var inputLayout = new InputLayout(_graphics.Device,
                    _effect.GetTechniqueByIndex(0).GetPassByIndex(0).Description.Signature,
                    VertexPositionColor.InputElements))
                {

                    _graphics.Device.ImmediateContext.InputAssembler.InputLayout = inputLayout;
                    _graphics.Device.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.LineList;
                    _graphics.Device.ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_vertexBuffer, VertexPositionColor.Size, 0));

                    for (int i = 0; i != _effect.Description.TechniqueCount; i++)
                    {
                        var technique = _effect.GetTechniqueByIndex(i);
                        for (int j = 0; j != technique.Description.PassCount; j++)
                        {
                            var pass = technique.GetPassByIndex(j);
                            pass.Apply(_graphics.Device.ImmediateContext);
                            _graphics.Device.ImmediateContext.Draw(_verticesCount, 0);
                        }
                    }
                }
            }
        }



        private void UpdateVertexBuffer(Device device)
        {
            lock (lockObj)
            {
                _verticesCount = 24 * _boxes.Count;
                var vertices = new VertexPositionColor[_verticesCount];

                var counter = 0;

                foreach (var box in _boxes.Values)
                {
                    var offset = counter * 24;

                    vertices[offset + 0] = new VertexPositionColor(TopLefFro * box.Size + box.Position, box.Color);
                    vertices[offset + 1] = new VertexPositionColor(TopRigFro * box.Size + box.Position, box.Color);
                    vertices[offset + 2] = new VertexPositionColor(TopLefBac * box.Size + box.Position, box.Color);
                    vertices[offset + 3] = new VertexPositionColor(TopRigBac * box.Size + box.Position, box.Color);
                    vertices[offset + 4] = new VertexPositionColor(TopLefFro * box.Size + box.Position, box.Color);
                    vertices[offset + 5] = new VertexPositionColor(TopLefBac * box.Size + box.Position, box.Color);
                    vertices[offset + 6] = new VertexPositionColor(TopRigFro * box.Size + box.Position, box.Color);
                    vertices[offset + 7] = new VertexPositionColor(TopRigBac * box.Size + box.Position, box.Color);
                    vertices[offset + 8] = new VertexPositionColor(BotLefFro * box.Size + box.Position, box.Color);
                    vertices[offset + 9] = new VertexPositionColor(BotRigFro * box.Size + box.Position, box.Color);
                    vertices[offset + 10] = new VertexPositionColor(BotLefBac * box.Size + box.Position, box.Color);
                    vertices[offset + 11] = new VertexPositionColor(BotRigBac * box.Size + box.Position, box.Color);
                    vertices[offset + 12] = new VertexPositionColor(BotLefFro * box.Size + box.Position, box.Color);
                    vertices[offset + 13] = new VertexPositionColor(BotLefBac * box.Size + box.Position, box.Color);
                    vertices[offset + 14] = new VertexPositionColor(BotRigFro * box.Size + box.Position, box.Color);
                    vertices[offset + 15] = new VertexPositionColor(BotRigBac * box.Size + box.Position, box.Color);
                    vertices[offset + 16] = new VertexPositionColor(TopLefFro * box.Size + box.Position, box.Color);
                    vertices[offset + 17] = new VertexPositionColor(BotLefFro * box.Size + box.Position, box.Color);
                    vertices[offset + 18] = new VertexPositionColor(TopLefBac * box.Size + box.Position, box.Color);
                    vertices[offset + 19] = new VertexPositionColor(BotLefBac * box.Size + box.Position, box.Color);
                    vertices[offset + 20] = new VertexPositionColor(TopRigFro * box.Size + box.Position, box.Color);
                    vertices[offset + 21] = new VertexPositionColor(BotRigFro * box.Size + box.Position, box.Color);
                    vertices[offset + 22] = new VertexPositionColor(TopRigBac * box.Size + box.Position, box.Color);
                    vertices[offset + 23] = new VertexPositionColor(BotRigBac * box.Size + box.Position, box.Color);

                    counter++;
                }

                
                _vertexBuffer?.Dispose();
                _vertexBuffer = Buffer.Create(device, BindFlags.VertexBuffer, vertices); // VertexBuffer.Create(device, vertices);
                
            }
        }

    }
}
