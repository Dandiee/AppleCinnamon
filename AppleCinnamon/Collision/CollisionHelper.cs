﻿using System;
using System.Linq;
using AppleCinnamon.Extensions;
using AppleCinnamon.Options;
using SharpDX;

namespace AppleCinnamon.Collision;

public static class CollisionHelper
{
    public static VoxelRayCollisionResult GetCurrentSelection(Ray ray, ChunkManager chunkManager)
    {
        const int exitCounter = 64;

        var position = ray.Position;
        var direction = Int3.Zero;

        for (var i = 0; i < exitCounter; i++)
        {
            var index = position.Round();
            if (!chunkManager.TryGetVoxelAddress(index, out var address) || address.RelativeVoxelIndex.Y >= address.Chunk.CurrentHeight)
            {
                return null;
            }

            var voxel = address.Chunk.GetVoxel(address.RelativeVoxelIndex);
            if (voxel.BlockType > 0 && voxel.BlockType != VoxelDefinition.Water.Type)
            {
                var voxelDefinition = voxel.GetDefinition();
                if (voxelDefinition.IsUnitSized)
                {
                    return new VoxelRayCollisionResult(index, address, -direction, voxelDefinition, voxel);
                }
                else
                {
                    var voxelPosition = index.ToVector3();
                    var voxelBoundingBox = voxelDefinition.GetBoundingBox(ref voxelPosition, ref voxel);
                    var currentRay = new Ray(position, ray.Direction);

                    if (voxelBoundingBox.Intersects(ref currentRay))
                    {
                        return new VoxelRayCollisionResult(index, address, -direction, voxelDefinition, voxel, voxelBoundingBox);
                    }
                }
            }

            var xTarget = index.X + Math.Sign(ray.Direction.X) / 2f;
            var yTarget = index.Y + Math.Sign(ray.Direction.Y) / 2f;
            var zTarget = index.Z + Math.Sign(ray.Direction.Z) / 2f;

            var xDistance = xTarget.Distance(position.X);
            var yDistance = yTarget.Distance(position.Y);
            var zDistance = zTarget.Distance(position.Z);

            var impacts = new[]
            {
                new {Impact = xDistance / Math.Abs(ray.Direction.X), Direction = Int3.UnitX * Math.Sign(ray.Direction.X)},
                new {Impact = yDistance / Math.Abs(ray.Direction.Y), Direction = Int3.UnitY * Math.Sign(ray.Direction.Y)},
                new {Impact = zDistance / Math.Abs(ray.Direction.Z), Direction = Int3.UnitZ * Math.Sign(ray.Direction.Z)}
            };

            var firstImpact = impacts.Where(impact => !float.IsNaN(impact.Impact)).OrderBy(impact => impact.Impact).First();

            direction = firstImpact.Direction;
            position += ray.Direction * (firstImpact.Impact * 1.0005f);
        }

        return null;
    }

    public static void ApplyPlayerPhysics(Camera camera, ChunkManager chunkManager, float realElapsedTime)
    {
        var position = camera.Position;
        var velocity = camera.Velocity;


        var min = CameraOptions.PlayerMin + position; // + Vector3.UnitY * 0.05f;
        var max = CameraOptions.PlayerMax + position;

        var playerBoundingBox = new BoundingBox(min, max);

        var minInd = playerBoundingBox.Minimum.Round();
        var maxInd = playerBoundingBox.Maximum.Round();

        var totalPenetration = Vector3.Zero;
        var resultVelocity = velocity;

        for (var i = minInd.X; i <= maxInd.X; i++)
        {
            for (var j = minInd.Y; j <= maxInd.Y; j++)
            {
                for (var k = minInd.Z; k <= maxInd.Z; k++)
                {
                    var absoluteIndex = new Int3(i, j, k);
                    if (!chunkManager.TryGetVoxelAddress(absoluteIndex, out var address))
                    {
                        return;
                    }

                    if (j >= address.Chunk.CurrentHeight)
                    {
                        continue;
                    }

                    var voxel = address.Chunk.GetVoxel(address.RelativeVoxelIndex);

                    var voxelDefinition = voxel.GetDefinition();

                    if (!voxelDefinition.IsPermeable)
                    {
                        var voxelPosition = absoluteIndex.ToVector3() + voxelDefinition.Offset;
                        var voxelHalfSize = voxelDefinition.Size / 2f;

                        var voxelBoundingBox = new BoundingBox(voxelPosition - voxelHalfSize, voxelPosition + voxelHalfSize);

                        var penetration = GetFirstPenetration(ref absoluteIndex, ref playerBoundingBox, ref voxelBoundingBox, ref velocity, chunkManager, realElapsedTime * 5);
                        if (penetration != null)
                        {

                            if (Math.Abs(penetration.Value.X) > Math.Abs(totalPenetration.X))
                            {
                                totalPenetration = new Vector3(penetration.Value.X, totalPenetration.Y, totalPenetration.Z);
                                resultVelocity = new Vector3(0, resultVelocity.Y, resultVelocity.Z);
                            }

                            if (Math.Abs(penetration.Value.Y) > Math.Abs(totalPenetration.Y))
                            {
                                if (penetration.Value.Y < 0)
                                {
                                    camera.IsInAir = false;
                                }

                                totalPenetration = new Vector3(totalPenetration.X, penetration.Value.Y, totalPenetration.Z);
                                resultVelocity = new Vector3(resultVelocity.X, 0, resultVelocity.Z);
                            }

                            if (Math.Abs(penetration.Value.Z) > Math.Abs(totalPenetration.Z))
                            {
                                totalPenetration = new Vector3(totalPenetration.X, totalPenetration.Y, penetration.Value.Z);
                                resultVelocity = new Vector3(resultVelocity.X, resultVelocity.Y, 0);
                            }
                        }
                    }
                }
            }
        }

        camera.Position -= totalPenetration * 1.05f;
        camera.Velocity = resultVelocity;
    }

    private static Vector3? GetFirstPenetration(
        ref Int3 absoluteIndex, ref BoundingBox playerBoundingBox, ref BoundingBox voxelBoundingBox,
        ref Vector3 velocity, ChunkManager chunkManager, float realElapsedTime)
    {
        var earliestTimeOfImpact = float.MaxValue;
        Vector3? result = null;

        // LEFT
        if (playerBoundingBox.Minimum.X < voxelBoundingBox.Maximum.X &&
            playerBoundingBox.Minimum.X > voxelBoundingBox.Minimum.X &&
            !velocity.X.IsEpsilon())
        {
            var penetrationDepth = voxelBoundingBox.Maximum.X - playerBoundingBox.Minimum.X;
            var timeOfImpact = -penetrationDepth / velocity.X;

            if (timeOfImpact > 0 && timeOfImpact < realElapsedTime && earliestTimeOfImpact > timeOfImpact)
            {
                if (chunkManager.TryGetVoxel(absoluteIndex + Int3.UnitX, out var neighbor)
                    && neighbor.GetDefinition().IsPermeable)
                {
                    result = -Vector3.UnitX * penetrationDepth;
                    earliestTimeOfImpact = timeOfImpact;
                }
            }
        }

        // RIGHT
        if (playerBoundingBox.Maximum.X < voxelBoundingBox.Maximum.X &&
            playerBoundingBox.Maximum.X > voxelBoundingBox.Minimum.X &&
            !velocity.X.IsEpsilon())
        {
            var penetrationDepth = playerBoundingBox.Maximum.X - voxelBoundingBox.Minimum.X;
            var timeOfImpact = penetrationDepth / velocity.X;

            if (timeOfImpact > 0 && timeOfImpact < realElapsedTime && earliestTimeOfImpact > timeOfImpact)
            {
                if (chunkManager.TryGetVoxel(absoluteIndex - Int3.UnitX, out var neighbor)
                    && neighbor.GetDefinition().IsPermeable)
                {
                    result = Vector3.UnitX * penetrationDepth;
                }
            }
        }
        // FRONT
        if (playerBoundingBox.Minimum.Z < voxelBoundingBox.Maximum.Z &&
            playerBoundingBox.Minimum.Z > voxelBoundingBox.Minimum.Z &&
            !velocity.Z.IsEpsilon())
        {
            var penetrationDepth = voxelBoundingBox.Maximum.Z - playerBoundingBox.Minimum.Z;
            var timeOfImpact = -penetrationDepth / velocity.Z;

            if (timeOfImpact > 0 && timeOfImpact < realElapsedTime && earliestTimeOfImpact > timeOfImpact)
            {
                if (chunkManager.TryGetVoxel(absoluteIndex + Int3.UnitZ, out var neighbor)
                    && neighbor.GetDefinition().IsPermeable)
                {
                    result = -Vector3.UnitZ * penetrationDepth;
                }
            }
        }

        // BACK
        if (playerBoundingBox.Maximum.Z < voxelBoundingBox.Maximum.Z &&
            playerBoundingBox.Maximum.Z > voxelBoundingBox.Minimum.Z &&
            !velocity.Z.IsEpsilon())
        {
            var penetrationDepth = playerBoundingBox.Maximum.Z - voxelBoundingBox.Minimum.Z;
            var timeOfImpact = penetrationDepth / velocity.Z;

            if (timeOfImpact > 0 && timeOfImpact < realElapsedTime && earliestTimeOfImpact > timeOfImpact)
            {
                if (chunkManager.TryGetVoxel(absoluteIndex - Int3.UnitZ, out var neighbor)
                    && neighbor.GetDefinition().IsPermeable)
                {
                    result = Vector3.UnitZ * penetrationDepth;
                }
            }
        }

        // FEET
        if (playerBoundingBox.Minimum.Y < voxelBoundingBox.Maximum.Y &&
            playerBoundingBox.Minimum.Y > voxelBoundingBox.Minimum.Y &&
            !velocity.Y.IsEpsilon())
        {
            var penetrationDepth = voxelBoundingBox.Maximum.Y - playerBoundingBox.Minimum.Y;
            var timeOfImpact = -penetrationDepth / velocity.Y;

            if (timeOfImpact > 0 && timeOfImpact < realElapsedTime && earliestTimeOfImpact > timeOfImpact)
            {
                if (chunkManager.TryGetVoxel(absoluteIndex + Int3.UnitY, out var neighbor)
                    && neighbor.GetDefinition().IsPermeable)
                {
                    result = -Vector3.UnitY * penetrationDepth;
                }
            }
        }

        // HEAD
        if (playerBoundingBox.Maximum.Y < voxelBoundingBox.Maximum.Y &&
            playerBoundingBox.Maximum.Y > voxelBoundingBox.Minimum.Y &&
            !velocity.Y.IsEpsilon())
        {
            var penetrationDepth = playerBoundingBox.Maximum.Y - voxelBoundingBox.Minimum.Y;
            var timeOfImpact = penetrationDepth / velocity.Y;

            if (timeOfImpact > 0 && timeOfImpact < realElapsedTime && earliestTimeOfImpact > timeOfImpact)
            {
                if(chunkManager.TryGetVoxel(absoluteIndex - Int3.UnitY, out var neighbor) 
                   && neighbor.GetDefinition().IsPermeable)
                {
                    result = Vector3.UnitY * penetrationDepth;
                }
            }
        }

        return result;
    }
}