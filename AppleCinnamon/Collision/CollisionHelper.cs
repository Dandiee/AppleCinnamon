using System;
using System.Linq;
using AppleCinnamon.Settings;
using AppleCinnamon.System;
using SharpDX;

namespace AppleCinnamon.Collision
{
    public static class CollisionHelper
    {
        public static VoxelRayCollisionResult GetCurrentSelection(Ray ray, ChunkManager chunkManager)
        {
            const int exitCounter = 128;

            var position = ray.Position;
            var direction = Int3.Zero;

            for (var i = 0; i < exitCounter; i++)
            {
                var index = position.Round();
                var voxel = chunkManager.GetVoxel(index);
                
                if (!voxel.HasValue)
                {
                    return null;
                }

                

                if (voxel.Value.Block > 0)
                {
                    var voxelDefinition = voxel.Value.GetDefinition();
                    if (voxelDefinition.IsUnitSized)
                    {
                        return new VoxelRayCollisionResult(index, -direction, voxelDefinition);
                    }
                    else
                    {
                        var voxelPosition = index.ToVector3();
                        var voxelBoundingBox = new BoundingBox(voxelPosition - voxelDefinition.Size/2f + voxelDefinition.Translation, voxelPosition + voxelDefinition.Size / 2f + voxelDefinition.Translation);
                        var currentRay = new Ray(position, ray.Direction);
                        if (voxelBoundingBox.Intersects(ref currentRay))
                        {
                            return new VoxelRayCollisionResult(index, -direction, voxelDefinition);
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

                var firstImpact = impacts.OrderBy(impact => impact.Impact).First();

                direction = firstImpact.Direction;
                position += ray.Direction * (firstImpact.Impact * 1.05f);
            }

            return null;
        }

        public static void ApplyPlayerPhysics(Camera camera, ChunkManager chunkManager)
        {
            var position = camera.Position.ToVector3();
            var velocity = camera.Velocity.ToVector3();


            var min = WorldSettings.PlayerMin + position;
            var max = WorldSettings.PlayerMax + position;

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
                        if (j >= 256)
                        {
                            continue;
                        }

                        var absoluteIndex = new Int3(i, j, k);
                        var voxel = chunkManager.GetVoxel(absoluteIndex);

                        if (voxel == null)
                        {
                            return;
                        }

                        var voxelDefinition = voxel.Value.GetDefinition();
                        // if (voxel.Value.Block > 0)
                        if (!voxelDefinition.IsPermeable)
                        {
                            var absoluteCoordinate = absoluteIndex.ToVector3();
                            var voxelBoundingBox = new BoundingBox(absoluteCoordinate - new Vector3(.5f),
                                absoluteCoordinate + new Vector3(.5f));

                            var penetration = GetFirstPenetration(absoluteIndex, playerBoundingBox, voxelBoundingBox, velocity, chunkManager);
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

            camera.Position -= totalPenetration.ToDouble3() * 1.05f;
            camera.Velocity = resultVelocity.ToDouble3();
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
                    var neighbour = chunkManager.GetVoxel(absoluteIndex + Int3.UnitX);
                    if (neighbour.HasValue && neighbour.Value.GetDefinition().IsPermeable)
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
                    var neighbour = chunkManager.GetVoxel(absoluteIndex - Int3.UnitX);
                    if (neighbour.HasValue && neighbour.Value.GetDefinition().IsPermeable)
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
                    var neighbour = chunkManager.GetVoxel(absoluteIndex + Int3.UnitZ);
                    if (neighbour.HasValue && neighbour.Value.GetDefinition().IsPermeable)
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
                    var neighbour = chunkManager.GetVoxel(absoluteIndex - Int3.UnitZ);
                    if (neighbour.HasValue && neighbour.Value.GetDefinition().IsPermeable)
                    {
                        result = Vector3.UnitZ * penetrationDepth;
                        earliestTimeOfImpact = timeOfImpact;
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

                if (timeOfImpact > 0 && earliestTimeOfImpact > timeOfImpact)
                {
                    var neighbour = chunkManager.GetVoxel(absoluteIndex + Int3.UnitY);
                    if (neighbour.HasValue && neighbour.Value.GetDefinition().IsPermeable)
                    {
                        result = -Vector3.UnitY * penetrationDepth;
                        earliestTimeOfImpact = timeOfImpact;
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

                if (timeOfImpact > 0 && earliestTimeOfImpact > timeOfImpact)
                {
                    var neighbour = chunkManager.GetVoxel(absoluteIndex - Int3.UnitY);
                    if (neighbour.HasValue && neighbour.Value.GetDefinition().IsPermeable)
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
