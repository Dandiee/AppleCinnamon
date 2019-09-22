﻿using System;
using System.Collections.Generic;
using AppleCinnamon.Collision;
using AppleCinnamon.Settings;
using AppleCinnamon.System;
using SharpDX;
using SharpDX.DirectInput;

namespace AppleCinnamon
{
    public class Camera
    {
        private readonly Graphics _graphics;
        private static readonly TimeSpan BuildCooldown = TimeSpan.FromMilliseconds(100);
        private DateTime _lastModification;

        public Double3 Position { get; set; }
        public Double3 LookAt { get; private set; }
        public Double3 Velocity { get; set; }
        public Int2 CurrentChunkIndex { get; private set; }
        public Vector3 CurrentChunkIndexVector { get; private set; }
        public Matrix View { get; private set; }
        public Matrix WorldViewProjection { get; private set; }
        public Matrix Projection { get; private set; }

        public Matrix World => Matrix.Identity * 2;
        public BoundingFrustum BoundingFrustum;
        public VoxelDefinition VoxelInHand { get; private set; }

        public bool IsInAir { get; set; }
        public bool IsInWater { get; set; }

        public Keyboard Keyboard { get; set; }
        public Mouse Mouse { get; set; }

        public float Yaw { get; private set; }
        public float Pitch { get; private set; }
        public Vector3 Orientation { get; private set; }
        public bool IsPaused { get; private set; }

        protected KeyboardState CurrentKeyboardState { get; private set; }
        protected KeyboardState LastKeyboardState { get; private set; }
        protected MouseState CurrentMouseState { get; private set; }
        protected MouseState LastMouseState { get; private set; }
        public VoxelRayCollisionResult CurrentCursor { get; protected set; }

        public static readonly IReadOnlyDictionary<Key, VoxelDefinition> KeyVoxelMapping = new Dictionary<Key, VoxelDefinition>
        {
            [Key.F1] = VoxelDefinition.Sand,
            [Key.F2] = VoxelDefinition.EmitterStone,
            [Key.F3] = VoxelDefinition.Snow
        };

        public Camera(Graphics graphics)
        {
            _graphics = graphics;
            Orientation = Vector3.UnitX;
            Position = new Double3(Game.StartPosition.X, Game.StartPosition.Y, Game.StartPosition.Z);
            // LookAt = Double3.Normalize(new Double3(0.5, -0.5, 0.5));

            var directInput = new DirectInput();
            Keyboard = new Keyboard(directInput);
            Keyboard.Properties.BufferSize = 128;
            Keyboard.Acquire();

            Mouse = new Mouse(directInput);
            Mouse.Properties.AxisMode = DeviceAxisMode.Relative;
            Mouse.Properties.BufferSize = 128;
            Mouse.Acquire();

            VoxelInHand = VoxelDefinition.Sand;

            IsInAir = true;
        }

        public void UpdateCurrentCursor(BoxDrawer boxDrawer, ChunkManager chunkManager)
        {
            CurrentCursor = CollisionHelper.GetCurrentSelection(new Ray(Position.ToVector3(), LookAt.ToVector3()), chunkManager);

            if (VoxelDefinition.Water.Type == chunkManager.GetVoxel(Position.ToVector3().Round())?.Block)
            {
                IsInWater = true;
            }
            else
            {
                IsInWater = false;
            }
        }

        public void Update(GameTime gameTime, ChunkManager chunkManager, BoxDrawer boxDrawer)
        {
            if (!chunkManager.IsInitialized)
            {
                return;
            }

            CurrentKeyboardState = Keyboard.GetCurrentState();
            CurrentMouseState = Mouse.GetCurrentState();

            CollisionHelper.ApplyPlayerPhysics(this, chunkManager, (float)gameTime.ElapsedGameTime.TotalSeconds);
            UpdateMove(gameTime, chunkManager);
            UpdateMatrices();

            UpdateCurrentCursor(boxDrawer, chunkManager);
            HandleDefaultInputs(chunkManager);

            LastKeyboardState = CurrentKeyboardState;
            LastMouseState = CurrentMouseState;
        }

        private void HandleDefaultInputs(ChunkManager chunkManager)
        {
            if (LastMouseState == null)
            {
                return;
            }

            const int leftClickIndex = 0;
            const int rightClickIndex = 1;

            if (!CurrentKeyboardState.IsPressed(Key.Escape) && LastKeyboardState.IsPressed(Key.Escape))
            {
                IsPaused = !IsPaused;
            }

            if (!CurrentKeyboardState.IsPressed(Key.F1) && LastKeyboardState.IsPressed(Key.F1))
            {
                Game.IsBackFaceCullingEnabled = !Game.IsBackFaceCullingEnabled;
            }

            if (!CurrentKeyboardState.IsPressed(Key.F2) && LastKeyboardState.IsPressed(Key.F2))
            {
                Game.IsViewFrustumCullingEnabled = !Game.IsViewFrustumCullingEnabled;
            }

            if (!CurrentKeyboardState.IsPressed(Key.F3) && LastKeyboardState.IsPressed(Key.F3))
            {
                Game.ShowChunkBoundingBoxes = !Game.ShowChunkBoundingBoxes;
            }

            foreach (var keyVoxel in KeyVoxelMapping)
            {
                if (CurrentKeyboardState.IsPressed(keyVoxel.Key))
                {
                    VoxelInHand = keyVoxel.Value;
                }
            }

            if (DateTime.Now - _lastModification > BuildCooldown)
            {
                if (CurrentMouseState.Buttons[leftClickIndex])
                {
                    if (CurrentCursor != null)
                    {
                        _lastModification = DateTime.Now;
                        chunkManager.SetBlock(CurrentCursor.AbsoluteVoxelIndex, 0);
                    }
                }

                if (CurrentMouseState.Buttons[rightClickIndex])
                {
                    if (CurrentCursor != null)
                    {
                        var direction = CurrentCursor.Direction;
                        var targetBlock = CurrentCursor.AbsoluteVoxelIndex + direction;


                        var hasPositionConflict = false;
                        var min = (WorldSettings.PlayerMin + Position.ToVector3()).Round();
                        var max = (WorldSettings.PlayerMax + Position.ToVector3()).Round();
                        for (var i = min.X; i <= max.X && !hasPositionConflict; i++)
                        {
                            for (var j = min.Y; j <= max.Y && !hasPositionConflict; j++)
                            {
                                for (var k = min.Z; k <= max.Z && !hasPositionConflict; k++)
                                {
                                    hasPositionConflict = targetBlock == new Int3(i, j, k);
                                }
                            }
                        }

                        if (!hasPositionConflict)
                        {
                            _lastModification = DateTime.Now;
                            chunkManager.SetBlock(targetBlock, VoxelInHand.Type);
                        }
                    }
                }
            }
        }



        private void UpdateMove(GameTime gameTime, ChunkManager chunkManager)
        {
            const float MouseSensitivity = .01f;
            const float MovementSensitivity = 2f;
            const float JumpVelocity = 12;
            const float MovmentFriction = 0.8f;
            const float SprintSpeedFactor = 3f;
            var prevPos = Position;
            var t = (float)gameTime.ElapsedGameTime.TotalSeconds;

            Velocity += WorldSettings.Gravity * t;

            if (IsInWater)
            {
                Velocity += WorldSettings.Gravity * -t;
            }

            Yaw = MathUtil.Mod2PI(Yaw + CurrentMouseState.X * -MouseSensitivity); // MathUtil.Mod2PI(
            Pitch = MathUtil.Clamp(Pitch + CurrentMouseState.Y * -MouseSensitivity, -MathUtil.PiOverTwo,
                MathUtil.PiOverTwo);


            var direction = Vector3.TransformCoordinate(Vector3.UnitX, Matrix.RotationY(Yaw)).ToDouble3();
            var directionNormal = new Double3(-direction.Z, 0, direction.X);
            var translationVector = Double3.Zero;

            if (CurrentKeyboardState.IsPressed(Key.W))
            {
                translationVector += direction;
            }

            if (CurrentKeyboardState.IsPressed(Key.S))
            {
                translationVector -= direction;
            }

            if (CurrentKeyboardState.IsPressed(Key.A))
            {
                translationVector -= directionNormal;
            }

            if (CurrentKeyboardState.IsPressed(Key.D))
            {
                translationVector += directionNormal;
            }

            if (translationVector != Double3.Zero)
            {
                Velocity += Double3.Normalize(translationVector) * MovementSensitivity *
                            (CurrentKeyboardState.IsPressed(Key.LeftShift) ? SprintSpeedFactor : 1);
            }


            if ((!IsInAir || IsInWater) && CurrentKeyboardState.IsPressed(Key.Space))
            {
                IsInAir = true;
                Velocity = new Double3(Velocity.X,
                    JumpVelocity * (CurrentKeyboardState.IsPressed(Key.LeftShift) ? SprintSpeedFactor : 1), Velocity.Z);
            }


            Velocity = new Double3(Velocity.X * MovmentFriction, IsInWater ? Velocity.Y * MovmentFriction : Velocity.Y, Velocity.Z * MovmentFriction);
            Position = Position + Velocity * t;

            

            var currentBlock = new Int3((int)Math.Round(Position.X), (int)Math.Round(Position.Y),
                (int)Math.Round(Position.Z));
            var chunkIndex = Chunk.GetChunkIndex(currentBlock);

            if (chunkIndex.HasValue)
            {
                CurrentChunkIndex = chunkIndex.Value;
                var currentChunk = chunkManager.Chunks[chunkIndex.Value];
                CurrentChunkIndexVector = currentChunk.BoundingBox.Center;
            }
        }

        public void UpdateMatrices()
        {
            var rotationMatrix = Matrix.RotationYawPitchRoll(Yaw, 0, Pitch);
            LookAt = Vector3.Normalize(Vector3.Transform(Vector3.UnitX, rotationMatrix).ToVector3()).ToDouble3();
            View = Matrix.LookAtRH(Position.ToVector3(), Position.ToVector3() + LookAt.ToVector3(),
                Vector3.TransformCoordinate(Vector3.UnitY, rotationMatrix));
            Projection = Matrix.PerspectiveFovRH(MathUtil.PiOverTwo,
                _graphics.RenderForm.Width / (float) _graphics.RenderForm.Height, 0.1f, 100000f);
            WorldViewProjection = World * View * Projection;
            
            BoundingFrustum = new BoundingFrustum(View * Projection);
        }
    }
}
