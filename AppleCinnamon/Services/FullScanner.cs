using AppleCinnamon.Helper;
using AppleCinnamon.Settings;

namespace AppleCinnamon
{
    public static class FullScanner
    {
        public static void FullScan(Chunk chunk)
        {

            var height = chunk.CurrentHeight;
            var voxels = chunk.Voxels;

            int i, j, k;
            for (k = 0; k < WorldSettings.ChunkSize; k++)
            {
                for (j = 0; j < height; j++)
                {
                    for (i = 0; i < WorldSettings.ChunkSize; i++)
                    {
                        var flatIndex = chunk.GetFlatIndex(i, j, k);
                        var voxel = voxels[flatIndex];

                        var definition = voxel.GetDefinition();

                        if (!definition.IsBlock && voxel.CompositeLight == 15)
                        {
                            continue;
                        }

                        var visibilityFlag = VisibilityFlag.None;
                        var voxelLight = voxel.CompositeLight;


                        if (j < chunk.CurrentHeight - 1) // top
                        {
                            var neighbor = chunk.GetVoxel(i, j + 1, k);
                            var neighborDefinition = neighbor.GetDefinition();

                            if (definition.IsBlock)
                            {
                                if ((neighborDefinition.CoverFlags & VisibilityFlag.Bottom) == 0 || (definition.CoverFlags & VisibilityFlag.Top) == 0)
                                {
                                    visibilityFlag |= VisibilityFlag.Top;
                                    chunk.BuildingContext.Top.VoxelCount++;
                                }
                            }
                            else
                            {
                                var lightDrop = VoxelDefinition.GetBrightnessLoss(neighborDefinition, definition, Face.Bottom);
                                if (voxelLight < neighbor.CompositeLight - lightDrop)
                                {
                                    voxelLight = (byte)(neighbor.CompositeLight - lightDrop);
                                }
                            }
                        }
                        else if (definition.IsBlock)
                        {
                            visibilityFlag += 1;
                            chunk.BuildingContext.Top.VoxelCount++;
                        }


                       

                        if (j > 0) // bottom
                        {
                            var neighbor = chunk.GetVoxel(i, j - 1, k);
                            var neighborDefinition = neighbor.GetDefinition();

                            if (definition.IsFaceVisible(neighborDefinition, VisibilityFlag.Top, VisibilityFlag.Bottom))
                            {
                                visibilityFlag |= VisibilityFlag.Bottom;
                                chunk.BuildingContext.Bottom.VoxelCount++;
                            }
                            else
                            {
                                var lightDrop = VoxelDefinition.GetBrightnessLoss(neighborDefinition, definition, Face.Top);
                                if (voxelLight < neighbor.CompositeLight - lightDrop)
                                {
                                    voxelLight = (byte)(neighbor.CompositeLight - lightDrop);
                                }
                            }
                        }

                        BuildHorizontalFace(i > 0, chunk.GetFlatIndex(i - 1, j, k), chunk, definition, flatIndex, ref visibilityFlag, ref voxelLight, chunk.BuildingContext.Left);
                        BuildHorizontalFace(i < WorldSettings.ChunkSize - 1, chunk.GetFlatIndex(i + 1, j, k), chunk, definition, flatIndex, ref visibilityFlag, ref voxelLight, chunk.BuildingContext.Right);
                        BuildHorizontalFace(k > 0, chunk.GetFlatIndex(i, j, k - 1), chunk, definition, flatIndex, ref visibilityFlag, ref voxelLight, chunk.BuildingContext.Front);
                        BuildHorizontalFace(k < WorldSettings.ChunkSize - 1, chunk.GetFlatIndex(i, j, k + 1), chunk, definition, flatIndex, ref visibilityFlag, ref voxelLight, chunk.BuildingContext.Back);

                        if (visibilityFlag != VisibilityFlag.None)
                        {
                            chunk.BuildingContext.VisibilityFlags[flatIndex] = visibilityFlag;
                        }

                        if (voxel.CompositeLight != voxelLight)
                        {
                            chunk.Voxels[flatIndex] = voxel.SetSunlight(voxelLight);
                            chunk.BuildingContext.LightPropagationVoxels.Enqueue(flatIndex);
                        }
                    }
                }
            }
        }

        [InlineMethod.Inline]
        private static void BuildHorizontalFace(bool isInChunk, int neighborFlatIndex, Chunk chunk, VoxelDefinition definition,
            int flatIndex, ref VisibilityFlag visibilityFlag, ref byte voxelLight, FaceBuildingContext context)
        {
            if (isInChunk)
            {
                var neighbor = chunk.Voxels[neighborFlatIndex];
                var neighborDefinition = neighbor.GetDefinition();

                if (definition.IsBlock)
                {
                    if ((neighborDefinition.CoverFlags & context.OppositeDirection) == 0 || (definition.CoverFlags & context.Direction) == 0) 
                    {
                        visibilityFlag |= context.Direction;
                        context.VoxelCount++;
                    }
                }
                else
                {
                    var lightDrop = VoxelDefinition.GetBrightnessLoss(neighborDefinition, definition, context.OppositeFace);
                    if (voxelLight < neighbor.CompositeLight - lightDrop)
                    {
                        voxelLight = (byte)(neighbor.CompositeLight - lightDrop);
                    }
                }
            }
            else if (definition.IsBlock)
            {
                context.PendingVoxels.Add(flatIndex);
            }
        }

        
    }


}

