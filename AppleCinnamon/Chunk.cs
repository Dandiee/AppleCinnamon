using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using AppleCinnamon.System;
using SharpDX;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;
using Help = AppleCinnamon.System.Help;

namespace AppleCinnamon
{
    public sealed class Chunk
    {
        public const int SliceHeight = 16;
        public const int SizeXy = 16;
        public const int SliceArea = SizeXy * SizeXy * SliceHeight;

        public int CurrentHeight;

        public ChunkBuffer ChunkBuffer;

        private FaceBuffer _waterBuffer;
        public int VisibleFacesCount { get; set; }

        public Dictionary<int, byte> VisibilityFlags;
        public List<int> TopMostWaterVoxels;

        public List<int> PendingLeftVoxels;
        public List<int> PendingRightVoxels;
        public List<int> PendingFrontVoxels;
        public List<int> PendingBackVoxels;
        public List<int> LightPropagationVoxels;
        

        public Cube<int> VoxelCount { get; }

        public Int2 ChunkIndex { get; }
        public Vector3 OffsetVector { get; }
        public Int2 Offset { get; }
        public Voxel[] Voxels;
        public Memory<Voxel> Voxels2;
        public MemoryHandle Handle;
        public ConcurrentDictionary<Int2, Chunk> Neighbours { get; }
        public ChunkState State { get; set; }
        public BoundingBox BoundingBox;
        public Vector3 ChunkIndexVector { get; }
        public Vector3 Center { get; private set; }
        public Vector2 Center2d { get; private set; }

        // and still not as fast as direct array addressing :'(
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Voxel GetVoxel(IntPtr arrayPtr, int i, int j, int k, int height) =>
            *(Voxel*) IntPtr.Add(arrayPtr, (i + SizeXy * (j + height * k)) * 2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe Voxel GetVoxelUnsafe(int flatIndex) => *((Voxel*) Handle.Pointer + flatIndex);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Voxel GetVoxelUnsafe(Chunk chunk, int flatIndex) => *((Voxel*)chunk.Handle.Pointer + flatIndex);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Voxel GetVoxel(int flatIndex) => Voxels[flatIndex];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Voxel GetVoxel(Chunk chunk, int flatIndex) => chunk.Voxels[flatIndex];


        public void ExtendUpward(int heightToFit)
        {
            var expectedSlices = heightToFit / SliceHeight + 1;
            var expectedHeight = expectedSlices * SliceHeight;

            var newVoxels = new Voxel[SizeXy * expectedHeight * SizeXy];
            
            // In case of local sliced array addressing, a simple copy does the trick
            // Array.Copy(Voxels, newVoxels, Voxels.Length);
            var sw = Stopwatch.StartNew();
            for (var i = 0; i < SizeXy; i++)
            {
                for (var j = 0; j < CurrentHeight; j++)
                {
                    for (var k = 0; k < SizeXy; k++)
                    {
                        var oldFlatIndex = Help.GetFlatIndex(i, j, k, CurrentHeight);
                        var newFlatIndex = Help.GetFlatIndex(i, j, k, expectedHeight);
                        newVoxels[newFlatIndex] = Voxels[oldFlatIndex];
                    }
                }
            }

            VisibilityFlags =
                VisibilityFlags.ToDictionary(kvp => kvp.Key.ToIndex(CurrentHeight).ToFlatIndex(expectedHeight),
                    kvp => kvp.Value);

            TopMostWaterVoxels = TopMostWaterVoxels.Select(s => s.ToIndex(CurrentHeight).ToFlatIndex(expectedHeight))
                .ToList();
            sw.Stop();


            Voxels = newVoxels;

            for (var i = 0; i < SizeXy; i++)
            {
                for (var k = 0; k < SizeXy; k++)
                {
                    for (var j = expectedHeight - 1; j >= CurrentHeight; j--)
                    {
                        Voxels[Help.GetFlatIndex(i, j, k, expectedHeight)] = Voxel.Air;
                    }
                }
            }

            CurrentHeight = expectedHeight;
            UpdateBoundingBox();
        }

        private void UpdateBoundingBox()
        {
            var size = new Vector3(SizeXy, CurrentHeight, SizeXy) / 2f;
            var position = new Vector3(SizeXy / 2f - .5f + SizeXy * ChunkIndex.X, CurrentHeight / 2f - .5f, SizeXy / 2f - .5f + SizeXy * ChunkIndex.Y);

            BoundingBox = new BoundingBox(position - size, position + size);
            Center = position;
            Center2d = new Vector2(position.X, position.Z);
        }

        public Voxel GetLocalWithNeighbours(int i, int j, int k, out VoxelAddress address)
        {
            if (j < 0 || j >= CurrentHeight)
            {
                address = new VoxelAddress();
                return Voxel.One;
            }

            var cx = 0;
            var cy = 0;

            if (i < 0)
            {
                cx = -1;
                i = SizeXy - 1;
            }
            else if (i == SizeXy)
            {
                cx = 1;
                i = 0;
            }

            if (k < 0)
            {
                cy = -1;
                k = SizeXy - 1;
            }
            else if (k == SizeXy)
            {
                cy = 1;
                k = 0;
            }

            if (cx == 0 && cy == 0)
            {
                address = new VoxelAddress(Int2.Zero, new Int3(i, j, k));
                return CurrentHeight <= j
                    ? Voxel.Air
                    : Voxels[Help.GetFlatIndex(i, j, k, CurrentHeight)];
            }

            var neighbourIndex = new Int2(cx, cy);
            address = new VoxelAddress(neighbourIndex, new Int3(i, j, k));

            var chunk = Neighbours[new Int2(cx, cy)];
            return chunk.CurrentHeight <= j
                ? Voxel.Air
                : chunk.Voxels[Help.GetFlatIndex(i, j, k, chunk.CurrentHeight)];
        }

        public Voxel GetLocalWithNeighbours(int i, int j, int k)
        {
            if (j < 0)
            {
                return Voxel.One;
            }

            var cx = 0;
            var cy = 0;

            if (i < 0)
            {
                cx = -1;
                i = SizeXy - 1;
            }
            else if (i == SizeXy)
            {
                cx = 1;
                i = 0;
            }

            if (k < 0)
            {
                cy = -1;
                k = SizeXy - 1;
            }
            else if (k == SizeXy)
            {
                cy = 1;
                k = 0;
            }

            if (cx == 0 && cy == 0)
            {
                return CurrentHeight <= j
                    ? Voxel.Air
                    : Voxels[Help.GetFlatIndex(i, j, k, CurrentHeight)];
            }

            var chunk = Neighbours[new Int2(cx, cy)];
            return chunk.CurrentHeight <= j
                   ? Voxel.Air
                   : chunk.Voxels[Help.GetFlatIndex(i, j, k, chunk.CurrentHeight)];
        }

        public Chunk(
            Int2 chunkIndex,
            Voxel[] voxels)
        {
            Neighbours = new ConcurrentDictionary<Int2, Chunk>();
            VisibilityFlags = new Dictionary<int, byte>();

            Voxels2 = new Memory<Voxel>(voxels);
            Handle = Voxels2.Pin();

            VoxelCount = new Cube<int>();
            PendingLeftVoxels = new List<int>(1024);
            PendingRightVoxels = new List<int>(1024);
            PendingFrontVoxels = new List<int>(1024);
            PendingBackVoxels = new List<int>(1024);
            LightPropagationVoxels = new List<int>(1024);
            TopMostWaterVoxels = new List<int>(128);
            CurrentHeight = (voxels.Length / SliceArea) * SliceHeight;
            ChunkIndex = chunkIndex;
            Offset = chunkIndex * new Int2(SizeXy, SizeXy);
            OffsetVector = new Vector3(Offset.X, 0, Offset.Y);
            Voxels = voxels;
            State = ChunkState.WarmUp;
            ChunkIndexVector = BoundingBox.Center;

            UpdateBoundingBox();
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
            if (absoluteVoxelIndex.Y < 0)
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

            var voxelIndex = new Int3(absoluteVoxelIndex.X & SizeXy - 1, absoluteVoxelIndex.Y, absoluteVoxelIndex.Z & SizeXy - 1);
            return new VoxelAddress(chunkIndex.Value, voxelIndex);
        }

        public void DrawSmarter(Device device, Vector3 currentChunkIndexVector)
        {
            if (!Game.IsBackFaceCullingEnabled)
            {
                device.ImmediateContext.InputAssembler.SetVertexBuffers(0, ChunkBuffer.Binding);
                device.ImmediateContext.InputAssembler.SetIndexBuffer(ChunkBuffer.IndexBuffer, Format.R16_UInt, 0);
                device.ImmediateContext.DrawIndexed(VisibleFacesCount * 6, 0, 0);
            }
            else
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

                    device.ImmediateContext.DrawIndexed(offset.Value.Count * 6, offset.Value.Offset * 6, 0);
                }
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
