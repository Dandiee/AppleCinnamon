using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using AppleCinnamon.Helper;
using AppleCinnamon.Settings;
using SharpDX;

namespace AppleCinnamon
{
    public sealed partial class Chunk : IDisposable
    {
        public const int SliceHeight = 16;
        public const int SliceArea = WorldSettings.ChunkSize * WorldSettings.ChunkSize * SliceHeight;

        public Voxel[] Voxels;
        public BoundingBox BoundingBox;
        public int CurrentHeight;

        public readonly ChunkBuildingContext BuildingContext;
        public readonly Int2 ChunkIndex;
        public readonly Vector3 OffsetVector;
        public readonly Int2 Offset;
        public Chunk[] Neighbors;
        public readonly Vector3 ChunkIndexVector;

        public bool IsMarkedForDelete { get; set; }
        public DateTime MarkedForDeleteAt { get; set; }

        public ChunkBuffers Buffers { get; set; }
        public Vector3 Center { get; private set; }
        public Vector2 Center2d { get; private set; }
        public bool IsRendered { get; set; }
        public int PipelineStep { get; set; }
        public bool IsFinalized { get; set; }
        public bool IsDebugHighlighted { get; set; }
        

        public Chunk(Int2 chunkIndex, Voxel[] voxels)
        {
            Neighbors = new Chunk[9];
            Voxels = voxels;
            BuildingContext = new ChunkBuildingContext();
            CurrentHeight = (voxels.Length / SliceArea) * SliceHeight;
            ChunkIndex = chunkIndex;
            Offset = chunkIndex * new Int2(WorldSettings.ChunkSize, WorldSettings.ChunkSize);
            OffsetVector = new Vector3(Offset.X, 0, Offset.Y);
            ChunkIndexVector = BoundingBox.Center;

            UpdateBoundingBox();
        }

       

        public void ExtendUpward(int heightToFit)
        {
            var expectedSlices = heightToFit / SliceHeight + 1;
            var expectedHeight = expectedSlices * SliceHeight;

            var newVoxels = new Voxel[WorldSettings.ChunkSize * expectedHeight * WorldSettings.ChunkSize];

            // In case of local sliced array addressing, a simple copy does the trick - Array.Copy(Voxels, newVoxels, Voxels.Length);
            for (var i = 0; i < WorldSettings.ChunkSize; i++)
            {
                for (var j = 0; j < CurrentHeight; j++)
                {
                    for (var k = 0; k < WorldSettings.ChunkSize; k++)
                    {
                        var oldFlatIndex = GetFlatIndex(i, j, k, CurrentHeight);
                        var newFlatIndex = GetFlatIndex(i, j, k, expectedHeight);
                        newVoxels[newFlatIndex] = GetVoxel(oldFlatIndex);
                    }
                }
            }

            BuildingContext.VisibilityFlags = BuildingContext.VisibilityFlags.ToDictionary(kvp => ConvertFlatIndex(kvp.Key, CurrentHeight, expectedHeight), kvp => kvp.Value);
            BuildingContext.TopMostWaterVoxels = BuildingContext.TopMostWaterVoxels.Select(s => ConvertFlatIndex(s, CurrentHeight, expectedHeight)).ToList();
            BuildingContext.SpriteBlocks = BuildingContext.SpriteBlocks.Select(s => ConvertFlatIndex(s, CurrentHeight, expectedHeight)).ToList();
            BuildingContext.SingleSidedSpriteBlocks = BuildingContext.SingleSidedSpriteBlocks.Select(s => ConvertFlatIndex(s, CurrentHeight, expectedHeight)).ToList();

            Voxels = newVoxels;
            var originalHeight = CurrentHeight;
            CurrentHeight = expectedHeight;

            for (var i = 0; i < WorldSettings.ChunkSize; i++)
            {
                for (var k = 0; k < WorldSettings.ChunkSize; k++)
                {
                    for (var j = expectedHeight - 1; j >= originalHeight; j--)
                    {
                        SetVoxel(i, j, k, Voxel.SunBlock);
                    }
                }
            }

            UpdateBoundingBox();
        }

        

        private void UpdateBoundingBox()
        {
            var size = new Vector3(WorldSettings.ChunkSize, CurrentHeight, WorldSettings.ChunkSize) / 2f;
            var position = new Vector3(WorldSettings.ChunkSize / 2f - .5f + WorldSettings.ChunkSize * ChunkIndex.X, CurrentHeight / 2f - .5f, WorldSettings.ChunkSize / 2f - .5f + WorldSettings.ChunkSize * ChunkIndex.Y);

            BoundingBox = new BoundingBox(position - size, position + size);
            Center = position;
            Center2d = new Vector2(position.X, position.Z);
        }


        private void DereferenceNeighbors()
        {
            foreach (var neighbor in Neighbors)
            {
                if (neighbor != null)
                {
                    for (var i = 0; i < neighbor.Neighbors.Length; i++)
                    {
                        if (neighbor.Neighbors[i] == this)
                        {
                            neighbor.Neighbors[i] = null;
                        }
                    }
                }
            }

            Neighbors = null;
        }

        public void Dispose()
        {
            Buffers?.Dispose();
        }

        public static volatile int WaitingForGc = 0;

        public bool CheckForValidity(Camera camera, DateTime now)
        {
            var distanceX = Math.Abs(camera.CurrentChunkIndex.X - ChunkIndex.X);
            var distanceY = Math.Abs(camera.CurrentChunkIndex.Y - ChunkIndex.Y);
            var maxDistance = Math.Max(distanceX, distanceY);

            if (maxDistance > Game.ViewDistance + Game.NumberOfPools)
            {
                if (IsMarkedForDelete)
                {
                    if (now - MarkedForDeleteAt > Game.ChunkDespawnCooldown)
                    {
                        Kill();
                        return false;
                    }
                }
                else
                {
                    MarkedForDeleteAt = now;
                    IsMarkedForDelete = true;
                }
            }
            else if (IsMarkedForDelete)
            {
                IsMarkedForDelete = false;
            }

            return true;
        }

        private void Kill()
        {
            DereferenceNeighbors();
            Dispose();

            Interlocked.Increment(ref WaitingForGc);
            Debug.WriteLine($"Chunk killed. {WaitingForGc}.");
        }

        ~Chunk()
        {
            Interlocked.Decrement(ref WaitingForGc);
            Debug.WriteLine($"Chunk collected. {WaitingForGc}.");
        }
    }
}
