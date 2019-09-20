using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AppleCinnamon.System;
using SharpDX;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;

namespace AppleCinnamon
{
    public class Chunk
    {
        public const int SizeXy = 64;
        public const int Height = 256;

        public ChunkBuffer ChunkBuffer;

        private FaceBuffer _waterBuffer;
        public int VisibleFacesCount { get; set; }

        private static readonly Cube<Vector3> Normals = new Cube<Vector3>(Vector3.UnitY, -Vector3.UnitY,
            -Vector3.UnitX, Vector3.UnitX, -Vector3.UnitZ, Vector3.UnitZ);

        public readonly Dictionary<int, byte> VisibilityFlags;
        public List<int> PendingLeftVoxels;
        public List<int> PendingRightVoxels;
        public List<int> PendingFrontVoxels;
        public List<int> PendingBackVoxels;
        public List<int> LightPropagationVoxels;
        public readonly List<int> TopMostWaterVoxels;

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
            if (j < 0 || j > Height- 1)
            {
                address = new VoxelAddress();
                return Voxel.One;
            }

            var cx = 0;
            var cy = 0;

            if (i < 0)
            {
                cx = -1;
                i = SizeXy- 1;
            }
            else if (i == SizeXy)
            {
                cx = 1;
                i = 0;
            }

            if (k < 0)
            {
                cy = -1;
                k = SizeXy- 1;
            }
            else if (k == SizeXy)
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
                i = SizeXy- 1;
            }
            else if (i == SizeXy)
            {
                cx = 1;
                i = 0;
            }

            if (k < 0)
            {
                cy = -1;
                k = SizeXy- 1;
            }
            else if (k == SizeXy)
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
            Neighbours = new ConcurrentDictionary<Int2, Chunk>();
            VisibilityFlags = new Dictionary<int, byte>();

            PendingLeftVoxels = new List<int>(1024);
            PendingRightVoxels = new List<int>(1024);
            PendingFrontVoxels = new List<int>(1024);
            PendingBackVoxels = new List<int>(1024);
            LightPropagationVoxels = new List<int>(1024);
            TopMostWaterVoxels = new List<int>(128);

            ChunkIndex = chunkIndex;
            Offset = chunkIndex * new Int2(SizeXy, SizeXy);
            OffsetVector = new Vector3(Offset.X, 0, Offset.Y);
            Voxels = voxels;
            State = ChunkState.WarmUp;

            var position = new Vector3(
                ChunkIndex.X * SizeXy+ SizeXy/ 2f - .5f,
                Height/ 2f,
                ChunkIndex.Y * SizeXy+ SizeXy/ 2f - .5f);

            var halfSize = new Vector3(SizeXy/ 2f, Height/ 2f, SizeXy/ 2f);
            BoundingBox = new BoundingBox(position - halfSize, position + halfSize);
            ChunkIndexVector = BoundingBox.Center;
        }

        public void SetBuffers(FaceBuffer waterBuffer)
        {

            if (_waterBuffer != null)
            {
                if (!_waterBuffer.IndexBuffer.IsDisposed)
                {
                    _waterBuffer.IndexBuffer?.Dispose();
                }
                if (!_waterBuffer.VertexBuffer.IsDisposed)
                {
                    _waterBuffer.VertexBuffer?.Dispose();
                }
            }

            _waterBuffer = waterBuffer;
        }


        public static Int2? GetChunkIndex(Int3 absoluteVoxelIndex)
        {
            if (absoluteVoxelIndex.Y >= Height|| absoluteVoxelIndex.Y < 0)
            {
                return null;
            }

            return new Int2(
                absoluteVoxelIndex.X < 0
                    ? ((absoluteVoxelIndex.X + 1) / SizeXy) - 1
                    : absoluteVoxelIndex.X / SizeXy,
                absoluteVoxelIndex.Z < 0
                    ? ((absoluteVoxelIndex.Z + 1) / SizeXy) - 1
                    : absoluteVoxelIndex.Z / SizeXy);
        }

        public static VoxelAddress? GetVoxelAddress(Int3 absoluteVoxelIndex)
        {
            var chunkIndex = GetChunkIndex(absoluteVoxelIndex);
            if (!chunkIndex.HasValue)
            {
                return null;
            }

            var voxelIndex = new Int3(absoluteVoxelIndex.X & SizeXy- 1, absoluteVoxelIndex.Y, absoluteVoxelIndex.Z & SizeXy- 1);
            return new VoxelAddress(chunkIndex.Value, voxelIndex);
        }

        public void DrawSmarter(Device device, Vector3 currentChunkIndexVector)
        {
            var vbSet = false;

            foreach (var offset in ChunkBuffer.Offsets)
            {
                if (offset.Value.Count == 0)
                {
                    continue;
                }

                if (Vector3.Dot(ChunkIndexVector - currentChunkIndexVector, offset.Key.ToVector3()) > 0)
                {
                    continue;
                }

                if (!vbSet)
                {
                    device.ImmediateContext.InputAssembler.SetVertexBuffers(0, ChunkBuffer.Binding);
                    device.ImmediateContext.InputAssembler.SetIndexBuffer(ChunkBuffer.IndexBuffer, Format.R16_UInt, 0);
                    vbSet = true;
                }

                device.ImmediateContext.DrawIndexed(offset.Value.Count * 6, offset.Value.Offset * 6, offset.Value.Offset * 4);
            }
        }

        public void DrawWater(Device device)
        {
            if (_waterBuffer != null && _waterBuffer.IndexCount > 0)
            {
                device.ImmediateContext.InputAssembler.SetVertexBuffers(0, _waterBuffer.Binding);
                device.ImmediateContext.InputAssembler.SetIndexBuffer(_waterBuffer.IndexBuffer, Format.R16_UInt, 0);
                device.ImmediateContext.DrawIndexed(_waterBuffer.IndexCount, 0, 0);
            }
        }
    }
}
