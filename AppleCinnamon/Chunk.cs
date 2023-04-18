using System;
using System.Linq;
using AppleCinnamon.ChunkBuilder;
using AppleCinnamon.Common;
using AppleCinnamon.Options;
using SharpDX;

namespace AppleCinnamon.Chunks
{
    public enum ChunkState
    {
        New,
        Killed,
        Finished,
    }

    public enum ChunkDeletionState
    {
        None,
        MarkedForDeletion,
        Deletion
    }

    public sealed partial class Chunk
    {
        public int Stage;
        public ChunkState State;
        public ChunkDeletionState Deletion;

        public Voxel[] Voxels;
        public BoundingBox BoundingBox;
        public int CurrentHeight;

        public readonly ChunkBuildingContext BuildingContext;
        public Int2 ChunkIndex;
        public Vector3 OffsetVector;
        public Int2 Offset;
        public Chunk[] Neighbors;
        public Vector3 ChunkIndexVector;

        public DateTime MarkedForDeleteAt { get; set; }



        public ChunkBuffers Buffers { get; set; }
        public Vector3 Center { get; private set; }
        public Vector2 Center2d { get; private set; }
        public bool IsRendered { get; set; }

        public Chunk Resurrect(Int2 chunkIndex)
        {
            Neighbors = new Chunk[9];
            SetNeighbor(0, 0, this);
            Stage = 0;
            State = ChunkState.New;
            BuildingContext.Clear();
            ChunkIndex = chunkIndex;
            Offset = chunkIndex * new Int2(GameOptions.ChunkSize, GameOptions.ChunkSize);
            OffsetVector = new Vector3(Offset.X, 0, Offset.Y);
            Deletion = ChunkDeletionState.None;
            State = 0;
            IsRendered = false;
            return this;
        }

        public Chunk(Int2 chunkIndex)
        {
            Neighbors = new Chunk[9];
            SetNeighbor(0, 0, this);
            BuildingContext = new ChunkBuildingContext();
            ChunkIndex = chunkIndex;
            Offset = chunkIndex * new Int2(GameOptions.ChunkSize, GameOptions.ChunkSize);
            OffsetVector = new Vector3(Offset.X, 0, Offset.Y);
        }

        public void ExtendUpward(int heightToFit)
        {
            var expectedSlices = heightToFit / GameOptions.SliceHeight + 1;
            var expectedHeight = expectedSlices * GameOptions.SliceHeight;

            var newVoxels = new Voxel[GameOptions.ChunkSize * expectedHeight * GameOptions.ChunkSize];

            // In case of local sliced array addressing, a simple copy does the trick - Array.Copy(Voxels, newVoxels, Voxels.Length);
            for (var i = 0; i < GameOptions.ChunkSize; i++)
            {
                for (var j = 0; j < CurrentHeight; j++)
                {
                    for (var k = 0; k < GameOptions.ChunkSize; k++)
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

            for (var i = 0; i < GameOptions.ChunkSize; i++)
            {
                for (var k = 0; k < GameOptions.ChunkSize; k++)
                {
                    for (var j = expectedHeight - 1; j >= originalHeight; j--)
                    {
                        SetVoxel(i, j, k, Voxel.SunBlock);
                    }
                }
            }

            UpdateBoundingBox();
        }



        public void UpdateBoundingBox()
        {
            var size = new Vector3(GameOptions.ChunkSize, CurrentHeight, GameOptions.ChunkSize) / 2f;
            var position = new Vector3(GameOptions.ChunkSize / 2f - .5f + GameOptions.ChunkSize * ChunkIndex.X, CurrentHeight / 2f - .5f, GameOptions.ChunkSize / 2f - .5f + GameOptions.ChunkSize * ChunkIndex.Y);

            BoundingBox = new BoundingBox(position - size, position + size);
            Center = position;
            Center2d = new Vector2(position.X, position.Z);
        }


        public void Kill()
        {
            Neighbors = new Chunk[9];
            Stage = 0;
            BuildingContext.Clear();
            IsRendered = false;
            State = ChunkState.Killed;

            if (Neighbors != null)
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
            }

            Buffers?.Dispose();
            Buffers = null;
        }

        public bool UpdateDeletion(Camera camera, DateTime now)
        {
            if (Deletion == ChunkDeletionState.Deletion) return true;

            var maxDistance = Math.Max(
                Math.Abs(camera.CurrentChunkIndex.X - ChunkIndex.X), 
                Math.Abs(camera.CurrentChunkIndex.Y - ChunkIndex.Y));

            if (maxDistance > GameOptions.ViewDistance + GameOptions.NumberOfPools)
            {
                if (Deletion == ChunkDeletionState.MarkedForDeletion)
                {
                    if (now - MarkedForDeleteAt > GameOptions.ChunkDespawnCooldown)
                    {
                        Deletion = ChunkDeletionState.Deletion;
                        return false;
                    }
                }
                else
                {
                    MarkedForDeleteAt = now;
                    Deletion = ChunkDeletionState.MarkedForDeletion;
                }
            }
            else if (Deletion == ChunkDeletionState.MarkedForDeletion)
            {
                Deletion = ChunkDeletionState.None;
            }

            return true;
        }

        
    }
}
