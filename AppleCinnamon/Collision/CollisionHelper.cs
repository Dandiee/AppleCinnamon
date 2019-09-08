using System;
using System.Linq;
using AppleCinnamon.System;
using SharpDX;

namespace AppleCinnamon.Collision
{
    public static class CollisionHelper
    {
        public static VoxelRayCollisionResult GetCurrentSelection(Ray ray, ChunkManager chunkManager)
        {
            const int exitCounter = 64;

            var position = ray.Position;
            var direction = Int3.Zero;

            for (var i = 0; i < exitCounter; i++)
            {
                var index = new Int3(
                    (int)Math.Round(position.X),
                    (int)Math.Round(position.Y),
                    (int)Math.Round(position.Z));

                var voxel = chunkManager.GetBlock(index);
                if (!voxel.HasValue)
                {
                    return null;
                }

                if (voxel.Value.Block > 0)
                {
                    return new VoxelRayCollisionResult(index, -direction);
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

                var firstImpact = impacts.OrderBy(impact => impact.Impact).First();

                direction = firstImpact.Direction;
                position += ray.Direction * (firstImpact.Impact * 1.05f);
            }

            return null;
        }

        public static Vector3? GetFirstPenetration(Int3 absoluteIndex, BoundingBox playerBoundingBox, BoundingBox voxelBoundingBox,
            Vector3 velocity, ChunkManager chunkManager)
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

                if (timeOfImpact > 0 && earliestTimeOfImpact > timeOfImpact)
                {
                    if (chunkManager.GetBlock(absoluteIndex + Int3.UnitX).Value.Block == 0)
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

                if (timeOfImpact > 0 && earliestTimeOfImpact > timeOfImpact)
                {
                    if (chunkManager.GetBlock(absoluteIndex - Int3.UnitX).Value.Block == 0)
                    {
                        result = Vector3.UnitX * penetrationDepth;
                        earliestTimeOfImpact = timeOfImpact;
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

                if (timeOfImpact > 0 && earliestTimeOfImpact > timeOfImpact)
                {
                    if (chunkManager.GetBlock(absoluteIndex + Int3.UnitZ).Value.Block == 0)
                    {
                        result = -Vector3.UnitZ * penetrationDepth;
                        earliestTimeOfImpact = timeOfImpact;
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

                if (timeOfImpact > 0 && earliestTimeOfImpact > timeOfImpact)
                {
                    if (chunkManager.GetBlock(absoluteIndex - Int3.UnitZ).Value.Block == 0)
                    {
                        result = Vector3.UnitZ * penetrationDepth;
                        earliestTimeOfImpact = timeOfImpact;
                    }
                }
            }

            // BOTTOM
            if (playerBoundingBox.Minimum.Y < voxelBoundingBox.Maximum.Y && 
                playerBoundingBox.Minimum.Y > voxelBoundingBox.Minimum.Y &&
                !velocity.Y.IsEpsilon())
            {
                var penetrationDepth = voxelBoundingBox.Maximum.Y - playerBoundingBox.Minimum.Y;
                var timeOfImpact = -penetrationDepth / velocity.Y;

                if (timeOfImpact > 0 && earliestTimeOfImpact > timeOfImpact)
                {
                    if (chunkManager.GetBlock(absoluteIndex + Int3.UnitY).Value.Block == 0)
                    {
                        result = -Vector3.UnitY * penetrationDepth;
                        earliestTimeOfImpact = timeOfImpact;
                    }
                }
            }

            // TOP
            if (playerBoundingBox.Maximum.Y < voxelBoundingBox.Maximum.Y && 
                playerBoundingBox.Maximum.Y > voxelBoundingBox.Minimum.Y &&
                !velocity.Y.IsEpsilon())
            {
                var penetrationDepth = playerBoundingBox.Maximum.Y - voxelBoundingBox.Minimum.Y;
                var timeOfImpact = penetrationDepth / velocity.Y;

                if (timeOfImpact > 0 && earliestTimeOfImpact > timeOfImpact)
                {
                    if (chunkManager.GetBlock(absoluteIndex - Int3.UnitY).Value.Block == 0)
                    {
                        result = Vector3.UnitY * penetrationDepth;
                        earliestTimeOfImpact = timeOfImpact;
                    }
                }
            }

            return result;
        }
    }
}
