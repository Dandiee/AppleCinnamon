using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using AppleCinnamon.Helper;
using SharpDX;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;
using Help = AppleCinnamon.Helper.Help;

namespace AppleCinnamon
{
    public class RefInt
    {
        public int Value;
    }

    public sealed class Chunk
    {
        public bool IsLocallyFinished { get; set; }

        public const int SliceHeight = 16;
        public const int SizeXy = 16;
        public const int SliceArea = SizeXy * SizeXy * SliceHeight;

        public int CurrentHeight;

        public ChunkBuffer ChunkBuffer;

        private FaceBuffer _waterBuffer;
        private FaceBuffer _spriteBuffer;
        public int VisibleFacesCount { get; set; }


        public List<int> TopMostWaterVoxels = new();
        public List<int> TopMostLandVoxels = new();

        public readonly ChunkBuildingContext BuildingContext;

        public Int2 ChunkIndex { get; }
        public Vector3 OffsetVector { get; }
        public Int2 Offset { get; }
        public Voxel[] Voxels;
        public Chunk[] Neighbors { get; }

        public BoundingBox BoundingBox;
        public Vector3 ChunkIndexVector { get; }
        public Vector3 Center { get; private set; }
        public Vector2 Center2d { get; private set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe Voxel GetVoxel(int flatIndex) => Voxels[flatIndex];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void SetVoxel(int flatIndex, Voxel voxel) => Voxels[flatIndex] = voxel;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe Voxel GetVoxelNoInline(int flatIndex) => Voxels[flatIndex];

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void SetVoxelNoInline(int flatIndex, Voxel voxel) => Voxels[flatIndex] = voxel;


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
                        newVoxels[newFlatIndex] = GetVoxel(oldFlatIndex);
                    }
                }
            }

            BuildingContext.VisibilityFlags = BuildingContext.VisibilityFlags.ToDictionary(kvp => kvp.Key.ToIndex(CurrentHeight).ToFlatIndex(expectedHeight),kvp => kvp.Value);
            TopMostWaterVoxels = TopMostWaterVoxels.Select(s => s.ToIndex(CurrentHeight).ToFlatIndex(expectedHeight)).ToList();
            BuildingContext.SpriteBlocks = BuildingContext.SpriteBlocks.Select(s => s.ToIndex(CurrentHeight).ToFlatIndex(expectedHeight)).ToList();
            sw.Stop();


            Voxels = newVoxels;

            for (var i = 0; i < SizeXy; i++)
            {
                for (var k = 0; k < SizeXy; k++)
                {
                    for (var j = expectedHeight - 1; j >= CurrentHeight; j--)
                    {
                        SetVoxel(Help.GetFlatIndex(i, j, k, expectedHeight), Voxel.Air);
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

        public Voxel GetLocalWithneighbors(Int3 ijk, out VoxelAddress address) => GetLocalWithneighbors(ijk.X, ijk.Y, ijk.Z, out address);

        public Voxel GetLocalWithneighbors(int i, int j, int k, out VoxelAddress address)
        {
            if (j < 0 || j >= CurrentHeight)
            {
                address = VoxelAddress.Zero;
                return Voxel.One;
            }

            var cx = (int)(-((i & 0b10000000_00000000_00000000_00000000) >> 31) + ((i / SizeXy)));
            var cy = (int)(-((k & 0b10000000_00000000_00000000_00000000) >> 31) + ((k / SizeXy)));

            var chunk = Neighbors[Help.GetChunkFlatIndex(cx, cy)];
            address = new VoxelAddress(new Int2(cx, cy), new Int3(i & (SizeXy - 1), j, k & (SizeXy - 1)));

            return chunk.CurrentHeight <= j
                ? Voxel.Air
                : chunk.GetVoxel(Help.GetFlatIndex(address.RelativeVoxelIndex.X, j, address.RelativeVoxelIndex.Z, chunk.CurrentHeight));
        }

        public Voxel GetLocalWithneighbors(Int3 ijk) => GetLocalWithneighbors(ijk.X, ijk.Y, ijk.Z);

        //[MethodImpl(MethodImplOptions.NoInlining)]
        [InlineMethod.Inline]
        public Voxel GetLocalWithneighbors(int i, int j, int k)
        {
            if (j < 0)
            {
                return Voxel.One;
            }

            var chunk = Neighbors[Help.GetChunkFlatIndex(
                (int)(-((i & 0b10000000_00000000_00000000_00000000) >> 31) + ((i / SizeXy))),
                (int)(-((k & 0b10000000_00000000_00000000_00000000) >> 31) + ((k / SizeXy))))];

            return chunk.CurrentHeight <= j
                ? Voxel.Air
                : chunk.GetVoxel(Help.GetFlatIndex(i & (SizeXy - 1), j, k & (SizeXy - 1), chunk.CurrentHeight));
        }


        public Chunk(
            Int2 chunkIndex,
            Voxel[] voxels)
        {
            //neighbors = new Dictionary<Int2, Chunk>();
            Neighbors = new Chunk[9];

            Voxels = voxels;

            //VoxelCount = new Cube<RefInt>(new RefInt(), new RefInt(), new RefInt(), new RefInt(), new RefInt(), new RefInt());
            //PendingLeftVoxels = new List<int>(1024);
            //PendingRightVoxels = new List<int>(1024);
            //PendingFrontVoxels = new List<int>(1024);
            //PendingBackVoxels = new List<int>(1024);


            BuildingContext = new ChunkBuildingContext();
            CurrentHeight = (voxels.Length / SliceArea) * SliceHeight;
            ChunkIndex = chunkIndex;
            Offset = chunkIndex * new Int2(SizeXy, SizeXy);
            OffsetVector = new Vector3(Offset.X, 0, Offset.Y);
            ChunkIndexVector = BoundingBox.Center;

            UpdateBoundingBox();
        }

        public void SetBuffers(FaceBuffer waterBuffer, FaceBuffer spriteBuffer)
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


            if (_spriteBuffer != null)
            {
                if (!_spriteBuffer.IndexBuffer.IsDisposed)
                {
                    _spriteBuffer.IndexBuffer?.Dispose();
                }
                if (!_spriteBuffer.VertexBuffer.IsDisposed)
                {
                    _spriteBuffer.VertexBuffer?.Dispose();
                }
            }

            _spriteBuffer = spriteBuffer;
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
                device.ImmediateContext.InputAssembler.SetIndexBuffer(ChunkBuffer.IndexBuffer, Format.R32_UInt, 0);
                //var count = ChunkBuffer.Offsets[Int3.UnitY].Count;
                //var offset = ChunkBuffer.Offsets[Int3.UnitY].Offset;
                //device.ImmediateContext.DrawIndexed(count * 6, offset, 0);

                //foreach (var face in ChunkBuffer.Offsets)
                //{
                //    //if (face.Key == Int3.UnitX)
                //    {
                //        device.ImmediateContext.DrawIndexed(face.Value.Count * 6, face.Value.Offset, 0);
                //    }
                //}

                device.ImmediateContext.DrawIndexed(VisibleFacesCount * 6, 0, 0);

                //device.ImmediateContext.DrawIndexed(100 * 6, 0, 0);

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
                        device.ImmediateContext.InputAssembler.SetIndexBuffer(ChunkBuffer.IndexBuffer, Format.R32_UInt, 0);
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
                device.ImmediateContext.InputAssembler.SetIndexBuffer(_waterBuffer.IndexBuffer, Format.R32_UInt, 0);
                device.ImmediateContext.DrawIndexed(_waterBuffer.IndexCount, 0, 0);
            }
        }

        public void DrawSprite(Device device)
        {
            if (_spriteBuffer != null && _spriteBuffer.IndexCount > 0)
            {
                device.ImmediateContext.InputAssembler.SetVertexBuffers(0, _spriteBuffer.Binding);
                device.ImmediateContext.InputAssembler.SetIndexBuffer(_spriteBuffer.IndexBuffer, Format.R32_UInt, 0);
                device.ImmediateContext.DrawIndexed(_spriteBuffer.IndexCount, 0, 0);
            }
        }
    }
}
