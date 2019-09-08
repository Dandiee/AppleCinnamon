using System;
using System.Collections.Generic;
using System.Linq;
using AppleCinnamon.Collision;
using AppleCinnamon.Settings;
using AppleCinnamon.System;
using SharpDX;
using SharpDX.DirectInput;
using SharpDX.Windows;

namespace AppleCinnamon
{
    public class Camera
    {
        private readonly BoxDrawer _boxDrawer;
        public Double3 Position { get; private set; }
        public Double3 LookAt { get; private set; }
        public Double3 Velocity { get; set; }
        public Int2 CurrentChunkIndex { get; private set; }
        public Vector3 CurrentChunkIndexVector { get; private set; }
        public Matrix View { get; private set; }
        public Matrix WorldViewProjection { get; private set; }
        public Matrix Projection { get; private set; }

        public Matrix World => Matrix.Identity;
        public BoundingFrustum BoundingFrustum { get; private set; }

        
        public bool IsInAir { get; set; }
        public bool IsSprinting { get; set; }
        public bool IsSneaking { get; set; }
        public Keyboard Keyboard { get; set; }
        public Mouse Mouse { get; set; }

        protected KeyboardState CurrentKeyboardState { get; private set; }
        protected KeyboardState LastKeyboardState { get; private set; }
        protected MouseState CurrentMouseState { get; private set; }
        protected MouseState LastMouseState { get; private set; }
        public VoxelRayCollisionResult CurrentCursor { get; protected set; }
        public byte BlockMode { get; set; }

        public Camera(BoxDrawer boxDrawer)
        {
            _boxDrawer = boxDrawer;
            Position = new Double3(Game.StartPosition.X, Game.StartPosition.Y, Game.StartPosition.Z);
            LookAt = Double3.Normalize(new Double3(0.5, -0.5, 0.5));


            var directInput = new DirectInput();
            Keyboard = new Keyboard(directInput);
            Keyboard.Properties.BufferSize = 128;
            Keyboard.Acquire();

            Mouse = new Mouse(directInput);
            Mouse.Properties.AxisMode = DeviceAxisMode.Relative;
            Mouse.Properties.BufferSize = 128;
            Mouse.Acquire();

            IsInAir = true;
        }

        public void UpdateCurrentCursor(RenderForm renderForm, ChunkManager chunkManager)
        {
            var ray = new Ray(Position.ToVector3(), LookAt.ToVector3());
            CurrentCursor = CollisionHelper.GetCurrentSelection(ray, chunkManager);

            if (CurrentCursor == null)
            {
                _boxDrawer.Remove("cursor");
                
            }
            else
            {
                _boxDrawer.Set("cursor",
                    new BoxDetails(Vector3.One * 1.05f, CurrentCursor.AbsoluteVoxelIndex.ToVector3(), Color.Yellow.ToColor3()));
            }
        }


        public void Move(GameTime gameTime, ChunkManager chunkManager)
        {
            var t = (float)gameTime.ElapsedGameTime.TotalSeconds;

            ApplyPhysics(chunkManager);
            UpdateMove(gameTime);
            
        }


        public void Update(GameTime gameTime, RenderForm renderForm, ChunkManager chunkManager)
        {
            // renderForm.Cursor = Cursor.Current..; renderForm.PointToScreen(new Point(0, 0));
            if (!chunkManager.Benchmark.IsEmpty)
            {
                var aggregate = new Dictionary<string, long>(chunkManager.Benchmark.First());
                foreach (var measure in chunkManager.Benchmark.Skip(1))
                {
                    foreach (var kvp in measure)
                    {
                        aggregate[kvp.Key] += kvp.Value;
                    }
                }
            }

            if (!chunkManager.IsFirstChunkInitialized)
            {
                return;
            }

            CurrentKeyboardState = Keyboard.GetCurrentState();
            CurrentMouseState = Mouse.GetCurrentState();

            Move(gameTime, chunkManager);
            UpdateMatrices(renderForm);

            UpdateCurrentCursor(renderForm, chunkManager);
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
            const int middleClickIndex = 2;

            if (!CurrentMouseState.Buttons[leftClickIndex] && LastMouseState.Buttons[leftClickIndex])
            {
                if (CurrentCursor != null)
                {
                    chunkManager.SetBlock(CurrentCursor.AbsoluteVoxelIndex, 0);
                }
            }

            if (!CurrentMouseState.Buttons[rightClickIndex] && LastMouseState.Buttons[rightClickIndex])
            {
                if (CurrentCursor != null)
                {
                    var direction = CurrentCursor.Direction;
                    var targetBlock = CurrentCursor.AbsoluteVoxelIndex + direction;
                    chunkManager.SetBlock(targetBlock, (byte)(CurrentKeyboardState.IsPressed(Key.LeftControl) ? 2 : 1));
                }
            }
        }

        public void UpdateMatrices(RenderForm renderForm)
        {
            View = Matrix.LookAtRH(Position.ToVector3(), Position.ToVector3() + LookAt.ToVector3(), Vector3.UnitY);
            Projection = Matrix.PerspectiveFovRH(MathUtil.PiOverTwo, renderForm.Width / (float) renderForm.Height, 0.1f, 100000f);
            WorldViewProjection = World * View * Projection;
            BoundingFrustum = new BoundingFrustum(View * Projection);
        }

        private void UpdateMove(GameTime gameTime)
        {
            const float MouseSensitivity = 2f;
            const float MovementSensitivity = 2f;
            const float JumpVelocity = 12;
            const float MovmentFriction = 0.8f;
            const float SprintSpeedFactor = 3f;

            var t = 1 / 1000f;

            //Velocity *= MovmentFriction;
            Velocity += WorldSettings.Gravity * t;

            LookAt = Double3.Normalize(
                Vector3.Transform(LookAt.ToVector3(), Matrix.RotationY(CurrentMouseState.X * -MouseSensitivity * t))
                    .ToVector3().ToDouble3());
            
            // LookAt = new Vector3(horizontalLookAt.X, LookAt.Y, horizontalLookAt.Z);

            LookAt = Double3.Normalize(Vector3.Transform(LookAt.ToVector3(),
                Matrix.RotationAxis(Vector3.Normalize(new Vector3(-(float) LookAt.Z, 0, (float) LookAt.X)),
                    CurrentMouseState.Y * -MouseSensitivity * t)).ToVector3().ToDouble3());

            var direction = Double3.Normalize(new Double3(LookAt.X, 0, LookAt.Z));
            var directionNormal = new Double3(-LookAt.Z, 0, LookAt.X);
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


            if (!IsInAir && CurrentKeyboardState.IsPressed(Key.Space))
            {
                IsInAir = true;
                Velocity = new Double3(Velocity.X, JumpVelocity * (CurrentKeyboardState.IsPressed(Key.LeftShift) ? SprintSpeedFactor : 1), Velocity.Z);
            }

            Velocity = new Double3(Velocity.X * MovmentFriction, Velocity.Y, Velocity.Z * MovmentFriction);
            Position = Position + Velocity * t;

            var currentBlock = new Int3((int) Math.Round(Position.X), (int) Math.Round(Position.Y),
                (int) Math.Round(Position.Z));
            var chunkIndex = Chunk.GetChunkIndex(currentBlock);

            if (chunkIndex.HasValue)
            {
                CurrentChunkIndex = chunkIndex.Value;

                var position = new Vector3(
                    CurrentChunkIndex.X * Chunk.Size.X + Chunk.Size.X / 2f - .5f,
                    Chunk.Size.Y / 2f,
                    CurrentChunkIndex.Y * Chunk.Size.Z + Chunk.Size.Z / 2f - .5f);
                var halfSize = new Vector3(Chunk.Size.X / 2f, Chunk.Size.Y / 2f, Chunk.Size.Z / 2f);

                var bb = new BoundingBox(position - halfSize, position + halfSize);
                CurrentChunkIndexVector = bb.Center;
            }
        }

        public static Int3 GetAbsoluteIndex(Vector3 point) =>
            new Int3(
                (int)Math.Round(point.X),
                (int)Math.Round(point.Y),
                (int)Math.Round(point.Z));

        private void ApplyPhysics(ChunkManager chunkManager)
        {
            ApplyFiziks(chunkManager);
        }

        private void ApplyFiziks(ChunkManager chunkManager)
        {
            var min = WorldSettings.PlayerMin + Position.ToVector3();
            var max = WorldSettings.PlayerMax + Position.ToVector3();
            var playerBoundingBox = new BoundingBox(min, max);

            var minInd = GetAbsoluteIndex(playerBoundingBox.Minimum);
            var maxInd = GetAbsoluteIndex(playerBoundingBox.Maximum);

            var totalPenetration = Vector3.Zero;
            var resultVelocity = Velocity.ToVector3();

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
                        var voxel = chunkManager.GetBlock(absoluteIndex);

                        if (voxel == null)
                        {
                            return;
                        }

                        if (voxel.Value.Block > 0)
                        {
                            var absoluteCoordinate = absoluteIndex.ToVector3();
                            var voxelBoundingBox = new BoundingBox(absoluteCoordinate - new Vector3(.5f),
                                absoluteCoordinate + new Vector3(.5f));

                            var penetration = CollisionHelper.GetFirstPenetration(absoluteIndex, playerBoundingBox, voxelBoundingBox, Velocity.ToVector3(), chunkManager);
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
                                        IsInAir = false;
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

            Position -= totalPenetration.ToDouble3() * 1.05f;
            Velocity = resultVelocity.ToDouble3();
        }
    }
}
