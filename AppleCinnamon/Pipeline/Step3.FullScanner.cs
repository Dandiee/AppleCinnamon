using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AppleCinnamon.Helper;
using AppleCinnamon.Pipeline.Context;
using AppleCinnamon.Settings;

namespace AppleCinnamon.Pipeline
{
    public static class VisibilityFlagExtensions
    {
        private static readonly IReadOnlyDictionary<VisibilityFlag, VisibilityFlag> OppositeMapping =
            new Dictionary<VisibilityFlag, VisibilityFlag>
            {
                [VisibilityFlag.Top] = VisibilityFlag.Bottom,
                [VisibilityFlag.Bottom] = VisibilityFlag.Top,
                [VisibilityFlag.Left] = VisibilityFlag.Right,
                [VisibilityFlag.Right] = VisibilityFlag.Left,
                [VisibilityFlag.Front] = VisibilityFlag.Back,
                [VisibilityFlag.Back] = VisibilityFlag.Front,
            };

        public static VisibilityFlag GetOpposite(this VisibilityFlag flag)
            => OppositeMapping[flag];
    }

    [Flags]
    public enum TransmittanceFlags : byte
    {
        None = 0,
        Quarter1 = 1,
        Quarter2 = 2,
        Quarter3 = 4,
        Quarter4 = 8,

        Top = 3,
        Bottom = 12,
        All = 15
    }


    public sealed class FullScanner : TransformChunkPipelineBlock
    {
        public override Chunk Process(Chunk chunk)
        {

            var height = chunk.CurrentHeight;
            var voxels = chunk.Voxels;

            int i, j, k;
            for (k = 0; k < Chunk.SizeXy; k++)
            {
                for (j = 0; j < height; j++)
                {
                    for (i = 0; i < Chunk.SizeXy; i++)
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
                        BuildHorizontalFace(i < Chunk.SizeXy - 1, chunk.GetFlatIndex(i + 1, j, k), chunk, definition, flatIndex, ref visibilityFlag, ref voxelLight, chunk.BuildingContext.Right);
                        BuildHorizontalFace(k > 0, chunk.GetFlatIndex(i, j, k - 1), chunk, definition, flatIndex, ref visibilityFlag, ref voxelLight, chunk.BuildingContext.Front);
                        BuildHorizontalFace(k < Chunk.SizeXy - 1, chunk.GetFlatIndex(i, j, k + 1), chunk, definition, flatIndex, ref visibilityFlag, ref voxelLight, chunk.BuildingContext.Back);

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

            return chunk;
        }

        [InlineMethod.Inline]
        private void BuildHorizontalFace(bool isInChunk, int neighborFlatIndex, Chunk chunk, VoxelDefinition definition,
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

