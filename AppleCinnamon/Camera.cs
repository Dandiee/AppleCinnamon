﻿using System;
using System.Collections.Generic;
using AppleCinnamon.Collision;
using AppleCinnamon.Settings;
using AppleCinnamon.System;
using SharpDX;
using SharpDX.DirectInput;

namespace AppleCinnamon
{
    public sealed class Camera
    {
        private readonly Graphics _graphics;
        private static readonly TimeSpan BuildCooldown = TimeSpan.FromMilliseconds(100);
        private DateTime _lastModification;

        public Vector2 Position2d { get; private set; }
        public Vector2 LookAt2d { get; private set; }

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

        private KeyboardState _currentKeyboardState;
        private KeyboardState _lastKeyboardState;
        private MouseState _currentMouseState;
        private MouseState _lastMouseState;
        public VoxelRayCollisionResult CurrentCursor { get; private set; }

        public static readonly IReadOnlyDictionary<Key, VoxelDefinition> KeyVoxelMapping = new Dictionary<Key, VoxelDefinition>
        {
            [Key.D1] = VoxelDefinition.Sand,
            [Key.D2] = VoxelDefinition.EmitterStone,
            [Key.D3] = VoxelDefinition.Snow,
            [Key.D4] = VoxelDefinition.Leaves,
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

            _currentKeyboardState = Keyboard.GetCurrentState();
            _currentMouseState = Mouse.GetCurrentState();

            CollisionHelper.ApplyPlayerPhysics(this, chunkManager, (float)gameTime.ElapsedGameTime.TotalSeconds);
            UpdateMove(gameTime, chunkManager);
            UpdateMatrices();

            UpdateCurrentCursor(boxDrawer, chunkManager);
            HandleDefaultInputs(chunkManager);

            _lastKeyboardState = _currentKeyboardState;
            _lastMouseState = _currentMouseState;
        }

        private void HandleDefaultInputs(ChunkManager chunkManager)
        {
            if (_lastMouseState == null)
            {
                return;
            }

            const int leftClickIndex = 0;
            const int rightClickIndex = 1;

            if (!_currentKeyboardState.IsPressed(Key.Escape) && _lastKeyboardState.IsPressed(Key.Escape))
            {
                IsPaused = !IsPaused;
            }

            if (!_currentKeyboardState.IsPressed(Key.F1) && _lastKeyboardState.IsPressed(Key.F1))
            {
                Game.IsBackFaceCullingEnabled = !Game.IsBackFaceCullingEnabled;
            }

            if (!_currentKeyboardState.IsPressed(Key.F2) && _lastKeyboardState.IsPressed(Key.F2))
            {
                Game.IsViewFrustumCullingEnabled = !Game.IsViewFrustumCullingEnabled;
            }

            if (!_currentKeyboardState.IsPressed(Key.F3) && _lastKeyboardState.IsPressed(Key.F3))
            {
                Game.ShowChunkBoundingBoxes = !Game.ShowChunkBoundingBoxes;
            }

            if (!_currentKeyboardState.IsPressed(Key.F4) && _lastKeyboardState.IsPressed(Key.F4))
            {
                Game.RenderWater = !Game.RenderWater;
            }

            if (!_currentKeyboardState.IsPressed(Key.F5) && _lastKeyboardState.IsPressed(Key.F5))
            {
                Game.RenderSolid = !Game.RenderSolid;
            }

            if (!_currentKeyboardState.IsPressed(Key.F12) && _lastKeyboardState.IsPressed(Key.F12))
            {
                Game.Debug = !Game.Debug;
            }

            foreach (var keyVoxel in KeyVoxelMapping)
            {
                if (_currentKeyboardState.IsPressed(keyVoxel.Key))
                {
                    VoxelInHand = keyVoxel.Value;
                }
            }

            if (DateTime.Now - _lastModification > BuildCooldown)
            {
                if (_currentMouseState.Buttons[leftClickIndex])
                {
                    if (CurrentCursor != null)
                    {
                        _lastModification = DateTime.Now;
                        chunkManager.SetBlock(CurrentCursor.AbsoluteVoxelIndex, 0);
                    }
                }

                if (_currentMouseState.Buttons[rightClickIndex])
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

            Yaw = MathUtil.Mod2PI(Yaw + _currentMouseState.X * -MouseSensitivity); // MathUtil.Mod2PI(
            Pitch = MathUtil.Clamp(Pitch + _currentMouseState.Y * -MouseSensitivity, -MathUtil.PiOverTwo,
                MathUtil.PiOverTwo);


            var direction = Vector3.TransformCoordinate(Vector3.UnitX, Matrix.RotationY(Yaw)).ToDouble3();
            var directionNormal = new Double3(-direction.Z, 0, direction.X);
            var translationVector = Double3.Zero;

            if (_currentKeyboardState.IsPressed(Key.W))
            {
                translationVector += direction;
            }

            if (_currentKeyboardState.IsPressed(Key.S))
            {
                translationVector -= direction;
            }

            if (_currentKeyboardState.IsPressed(Key.A))
            {
                translationVector -= directionNormal;
            }

            if (_currentKeyboardState.IsPressed(Key.D))
            {
                translationVector += directionNormal;
            }

            if (translationVector != Double3.Zero)
            {
                Velocity += Double3.Normalize(translationVector) * MovementSensitivity *
                            (_currentKeyboardState.IsPressed(Key.LeftShift) ? SprintSpeedFactor : 1);
            }


            if ((!IsInAir || IsInWater) && _currentKeyboardState.IsPressed(Key.Space))
            {
                IsInAir = true;
                Velocity = new Double3(Velocity.X,
                    JumpVelocity * (_currentKeyboardState.IsPressed(Key.LeftShift) ? SprintSpeedFactor : 1), Velocity.Z);
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
            LookAt2d = new Vector2((float)LookAt.X, (float)LookAt.Z);
            Position2d = new Vector2((float)Position.X, (float)Position.Z);
        }
    }
}
