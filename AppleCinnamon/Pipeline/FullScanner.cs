using System;
using System.Collections.Generic;
using System.Diagnostics;
using AppleCinnamon.Settings;
using AppleCinnamon.System;

namespace AppleCinnamon.Pipeline
{
    [Flags]
    public enum VisibilityFlag : byte
    {
        None = 0,
        Top = 1,
        Bottom = 2,
        Left = 4,
        Right = 8,
        Front = 16,
        Back = 32,
        All = 63
    }

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
                [VisibilityFlag.Back] = VisibilityFlag.Bottom,
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
        All = 15
    }


    public sealed class FullScanner
    {
        public DataflowContext<Chunk> Process(DataflowContext<Chunk> context)
        {
            var sw = Stopwatch.StartNew();
            var chunk = context.Payload;

            var height = chunk.CurrentHeight;
            var voxels = chunk.Voxels;

            int i, j, k;
            for (k = 0; k < Chunk.SizeXy; k++)
            {
                for (j = 0; j < height; j++)
                {
                    for (i = 0; i < Chunk.SizeXy; i++)
                    {
                        var flatIndex = Help.GetFlatIndex(i, j, k, chunk.CurrentHeight);
                        var voxel = voxels[flatIndex];

                        var definition = VoxelDefinition.DefinitionByType[voxel.Block];

                        if (!definition.IsBlock && voxel.Lightness == 15)
                        {
                            continue;
                        }

                        var visibilityFlag = VisibilityFlag.None;
                        var voxelLight = voxel.Lightness;


                        if (j < chunk.CurrentHeight - 1) // top
                        {
                            var neighbor = chunk.Voxels[Help.GetFlatIndex(i, j + 1, k, chunk.CurrentHeight)];
                            var neighborDefinition = VoxelDefinition.DefinitionByType[neighbor.Block];

                            if (definition.IsBlock)
                            {
                                if ((neighborDefinition.CoverFlags & VisibilityFlag.Bottom) == VisibilityFlag.None)
                                {
                                    visibilityFlag |= VisibilityFlag.Top;
                                    chunk.BuildingContext.Top.VoxelCount++;
                                }
                            }
                            else
                            {
                                var lightDrop = VoxelDefinition.GetBrightnessLoss(neighborDefinition, definition, Face.Bottom);
                                if (voxelLight < neighbor.Lightness - lightDrop)
                                {
                                    voxelLight = (byte)(neighbor.Lightness - lightDrop);
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
                            var neighbor = chunk.Voxels[Help.GetFlatIndex(i, j - 1, k, chunk.CurrentHeight)];
                            var neighborDefinition = VoxelDefinition.DefinitionByType[neighbor.Block];

                            if (definition.IsFaceVisible(neighborDefinition, VisibilityFlag.Top))
                            {
                                visibilityFlag |= VisibilityFlag.Bottom;
                                chunk.BuildingContext.Bottom.VoxelCount++;
                            }
                            else
                            {
                                var lightDrop = VoxelDefinition.GetBrightnessLoss(neighborDefinition, definition, Face.Top);
                                if (voxelLight < neighbor.Lightness - lightDrop)
                                {
                                    voxelLight = (byte)(neighbor.Lightness - lightDrop);
                                }
                            }
                        }

                        BuildHorizontalFace(i > 0,
                            Help.GetFlatIndex(i - 1, j, k, chunk.CurrentHeight), chunk, definition, flatIndex, ref visibilityFlag, ref voxelLight, chunk.BuildingContext.Left);
                        BuildHorizontalFace(i < Chunk.SizeXy - 1,
                            Help.GetFlatIndex(i + 1, j, k, chunk.CurrentHeight), chunk, definition, flatIndex, ref visibilityFlag, ref voxelLight, chunk.BuildingContext.Right);
                        BuildHorizontalFace(k > 0,
                            Help.GetFlatIndex(i, j, k - 1, chunk.CurrentHeight), chunk, definition, flatIndex, ref visibilityFlag, ref voxelLight, chunk.BuildingContext.Front);
                        BuildHorizontalFace(k < Chunk.SizeXy - 1,
                            Help.GetFlatIndex(i, j, k + 1, chunk.CurrentHeight), chunk, definition, flatIndex, ref visibilityFlag, ref voxelLight, chunk.BuildingContext.Back);

                        if (visibilityFlag != VisibilityFlag.None)
                        {
                            chunk.BuildingContext.VisibilityFlags[flatIndex] = visibilityFlag;
                        }

                        if (voxel.Lightness != voxelLight)
                        {
                            chunk.Voxels[flatIndex] = new Voxel(voxel.Block, voxelLight);
                            chunk.BuildingContext.LightPropagationVoxels.Enqueue(flatIndex);
                        }
                    }
                }
            }


            sw.Stop();

            return new DataflowContext<Chunk>(context, chunk, sw.ElapsedMilliseconds, nameof(FullScanner));
        }

        [InlineMethod.Inline]
        private void BuildHorizontalFace(bool isInChunk, int neighborFlatIndex, Chunk chunk, VoxelDefinition definition,
            int flatIndex, ref VisibilityFlag visibilityFlag, ref byte voxelLight, FaceBuildingContext context)
        {
            if (isInChunk)
            {
                var neighbor = chunk.Voxels[neighborFlatIndex];
                var neighborDefinition = VoxelDefinition.DefinitionByType[neighbor.Block];

                if (definition.IsBlock)
                {
                    if ((neighborDefinition.CoverFlags & context.OppositeDirection) == 0) 
                    {
                        visibilityFlag |= context.Direction;
                        context.VoxelCount++;
                    }
                }
                else
                {
                    var lightDrop = VoxelDefinition.GetBrightnessLoss(neighborDefinition, definition, context.OppositeFace);
                    if (voxelLight < neighbor.Lightness - lightDrop)
                    {
                        voxelLight = (byte)(neighbor.Lightness - lightDrop);
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

