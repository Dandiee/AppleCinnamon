using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using AppleCinnamon.System;
using AppleCinnamon.Vertices;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;

namespace AppleCinnamon
{
    public class Chunk
    {
        public const int SizeXy = 16;
        public const int Height = 256;

        private Cube<FaceBuffer> _bufferCube;
        private KeyValuePair<Face, FaceBuffer>[] _buffers;

        private static readonly Cube<Vector3> Normals = new Cube<Vector3>(Vector3.UnitY, -Vector3.UnitY,
            -Vector3.UnitX, Vector3.UnitX, -Vector3.UnitZ, Vector3.UnitZ);

        public static readonly Int3 Size = new Int3(16, 256, 16);


        public readonly Dictionary<int, byte> VisibilityFlags;
        public readonly List<int> PendingLeftVoxels;
        public readonly List<int> PendingRightVoxels;
        public readonly List<int> PendingFrontVoxels;
        public readonly List<int> PendingBackVoxels;
        public readonly List<int> LightPropagationVoxels;

        public Int2 ChunkIndex { get; }
        public Vector3 OffsetVector { get; }
        public Int2 Offset { get; }
        public Voxel[] Voxels { get; }
        public ConcurrentDictionary<Int2, Chunk> Neighbours { get; }
        public ChunkState State { get; set; }
        public BoundingBox BoundingBox { get; }
        public Vector3 ChunkIndexVector { get; }

        public void SetLocalVoxel(int i, int j, int k, Voxel voxel) => Voxels[i + SizeXy * (j + Height * k)] = voxel;
        public void SetLocalVoxel(Int3 relativeIndex, Voxel voxel) => SetLocalVoxel(relativeIndex.X, relativeIndex.Y, relativeIndex.Z, voxel);
        public Voxel GetLocalVoxel(Int3 relativeIndex) => GetLocalVoxel(relativeIndex.X, relativeIndex.Y, relativeIndex.Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Voxel GetLocalVoxel(int i, int j, int k) => Voxels[i + SizeXy * (j + Height * k)];


        public Voxel GetLocalWithNeighbours(int i, int j, int k, out VoxelAddress address)
        {
            if (j < 0 || j > Size.Y - 1)
            {
                address = new VoxelAddress();
                return Voxel.One;
            }

            var cx = 0;
            var cy = 0;

            if (i < 0)
            {
                cx = -1;
                i = Size.X - 1;
            }
            else if (i == Size.X)
            {
                cx = 1;
                i = 0;
            }

            if (k < 0)
            {
                cy = -1;
                k = Size.Z - 1;
            }
            else if (k == Size.Z)
            {
                cy = 1;
                k = 0;
            }

            if (cx == 0 && cy == 0)
            {
                address = new VoxelAddress(Int2.Zero, new Int3(i, j, k));
                return Voxels[i + SizeXy * (j + Height * k)];
            }

            var neighbourIndex = new Int2(cx, cy);
            address = new VoxelAddress(neighbourIndex, new Int3(i, j, k));
            return Neighbours[new Int2(cx, cy)].Voxels[i + SizeXy * (j + Height * k)];
        }

        public Voxel GetLocalWithNeighbours(int i, int j, int k)
        {
            if (((ushort)j & 256) != 0)
            {
                return Voxel.One;
            }

            var cx = 0;
            var cy = 0;

            if (i < 0)
            {
                cx = -1;
                i = Size.X - 1;
            }
            else if (i == Size.X)
            {
                cx = 1;
                i = 0;
            }

            if (k < 0)
            {
                cy = -1;
                k = Size.Z - 1;
            }
            else if (k == Size.Z)
            {
                cy = 1;
                k = 0;
            }

            if (cx == 0 && cy == 0)
            {
                return Voxels[i + SizeXy * (j + Height * k)];
            }

            return Neighbours[new Int2(cx, cy)].Voxels[i + SizeXy * (j + Height * k)];
        }

        public Chunk(
            Int2 chunkIndex,
            Voxel[] voxels)
        {
            _bufferCube = new Cube<FaceBuffer>();
            Neighbours = new ConcurrentDictionary<Int2, Chunk>();
            VisibilityFlags = new Dictionary<int, byte>();

            PendingLeftVoxels = new List<int>(1024);
            PendingRightVoxels = new List<int>(1024);
            PendingFrontVoxels = new List<int>(1024);
            PendingBackVoxels = new List<int>(1024);
            LightPropagationVoxels = new List<int>(1024);

            ChunkIndex = chunkIndex;
            Offset = chunkIndex * new Int2(Size.X, Size.Z);
            OffsetVector = new Vector3(Offset.X, 0, Offset.Y);
            Voxels = voxels;
            State = ChunkState.WarmUp;

            var position = new Vector3(
                ChunkIndex.X * Size.X + Size.X / 2f - .5f,
                Size.Y / 2f,
                ChunkIndex.Y * Size.Z + Size.Z / 2f - .5f);

            var halfSize = new Vector3(Size.X / 2f, Size.Y / 2f, Size.Z / 2f);
            BoundingBox = new BoundingBox(position - halfSize, position + halfSize);
            ChunkIndexVector = BoundingBox.Center;
        }

        public void SetBuffers(Cube<FaceBuffer> buffers)
        {
            if (_bufferCube != null)
            {
                foreach (var face in _bufferCube.GetAll())
                {
                    if (face.Value != null)
                    {
                        if (!face.Value.IndexBuffer.IsDisposed)
                        {
                            face.Value.IndexBuffer?.Dispose();
                        }

                        if (!face.Value.VertexBuffer.IsDisposed)
                        {
                            face.Value.VertexBuffer?.Dispose();
                        }
                    }
                }
            }

            _bufferCube = buffers;
            _buffers = _bufferCube.GetAll().ToArray();
        }

        public static Int2? GetChunkIndex(Int3 absoluteVoxelIndex)
        {
            if (absoluteVoxelIndex.Y >= Size.Y || absoluteVoxelIndex.Y < 0)
            {
                return null;
            }

            return new Int2(
                absoluteVoxelIndex.X < 0
                    ? ((absoluteVoxelIndex.X + 1) / Size.X) - 1
                    : absoluteVoxelIndex.X / Size.X,
                absoluteVoxelIndex.Z < 0
                    ? ((absoluteVoxelIndex.Z + 1) / Size.Z) - 1
                    : absoluteVoxelIndex.Z / Size.Z);
        }

        public static VoxelAddress? GetVoxelAddress(Int3 absoluteVoxelIndex)
        {
            var chunkIndex = GetChunkIndex(absoluteVoxelIndex);
            if (!chunkIndex.HasValue)
            {
                return null;
            }

            var voxelIndex = new Int3(absoluteVoxelIndex.X & Size.X - 1, absoluteVoxelIndex.Y, absoluteVoxelIndex.Z & Size.Z - 1);
            return new VoxelAddress(chunkIndex.Value, voxelIndex);
        }

        public void Draw(Device device, Vector3 currentChunkIndexVector)
        {
            foreach (var bufferFace in _buffers)
            {
                var buffer = bufferFace.Value;
                var normal = Normals[bufferFace.Key];

                if (buffer == null || buffer.IndexCount == 0
                    || Vector3.Dot(ChunkIndexVector - currentChunkIndexVector, normal) > 0)
                {
                    continue;
                }
                
                device.ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(buffer.VertexBuffer, VertexSolidBlock.Size, 0));
                device.ImmediateContext.InputAssembler.SetIndexBuffer(buffer.IndexBuffer, Format.R16_UInt, 0);
                device.ImmediateContext.DrawIndexed(buffer.IndexCount, 0, 0);
            }
        }
    }
}
