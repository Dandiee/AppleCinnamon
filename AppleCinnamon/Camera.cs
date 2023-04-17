using System;
using System.Runtime;
using AppleCinnamon.Collision;
using AppleCinnamon.Common;
using AppleCinnamon.Extensions;
using AppleCinnamon.Settings;
using SharpDX;
using SharpDX.DirectInput;

namespace AppleCinnamon
{
    public sealed partial class Camera
    {
        private readonly Graphics _graphics;
        private static readonly TimeSpan BuildCooldown = TimeSpan.FromMilliseconds(100);

        private DateTime _lastModification;

        public Vector2 Position2d { get; private set; }
        public Vector2 LookAt2d { get; private set; }

        public Vector3 Position { get; set; }
        public Vector3 LookAt { get; private set; }
        public Vector3 Velocity { get; set; }
        public Int2 CurrentChunkIndex { get; private set; }
        public Matrix View { get; private set; }
        public Matrix WorldViewProjection { get; private set; }
        public Matrix WorldView { get; private set; }
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

        public KeyboardState CurrentKeyboardState;
        public KeyboardState LastKeyboardState;
        public KeyboardState LastKeyboardStateButForRealz;
        private MouseState _currentMouseState;
        private MouseState _lastMouseState;
        public VoxelRayCollisionResult CurrentCursor { get; private set; }
        private int _voxelDefinitionIndexInHand;

        private readonly Vector3 InitialLookAt;

        public Camera(Graphics graphics)
        {
            _graphics = graphics;
            Position = new Vector3(Game.StartPosition.X, Game.StartPosition.Y, Game.StartPosition.Z);
            LookAt = Vector3.Normalize(new Vector3(1, 0, 0));
            InitialLookAt = LookAt;

            var directInput = new DirectInput();
            Keyboard = new Keyboard(directInput);
            Keyboard.Properties.BufferSize = 128;
            Keyboard.Acquire();

            LastKeyboardState = Keyboard.GetCurrentState();
            CurrentKeyboardState = Keyboard.GetCurrentState();
            LastKeyboardStateButForRealz = Keyboard.GetCurrentState();

            Mouse = new Mouse(directInput);
            Mouse.Properties.AxisMode = DeviceAxisMode.Relative;
            Mouse.Properties.BufferSize = 128;
            Mouse.Acquire();

            VoxelInHand = VoxelDefinition.Sand;
            _voxelDefinitionIndexInHand = VoxelDefinition.RegisteredDefinitions.IndexOf(VoxelInHand.Type);

            IsInAir = true;
        }

        public void UpdateCurrentCursor(ChunkManager chunkManager)
        {
            var result = CollisionHelper.GetCurrentSelection(new Ray(Position, LookAt), chunkManager);
            if (result != null && CurrentCursor != null)
            {
                if (result.AbsoluteVoxelIndex != CurrentCursor.AbsoluteVoxelIndex)
                {
                    CurrentCursor = result;
                }
            }
            else CurrentCursor = result;
            
            
            if (chunkManager.TryGetVoxel(Position.Round(), out var voxel) && voxel.BlockType == VoxelDefinition.Water.Type)
            {
                IsInWater = true;
            }
            else
            {
                IsInWater = false;
            }
        }

        public void Update(GameTime gameTime, ChunkManager chunkManager)
        {
            if (!chunkManager.IsInitialized)
            {
                return;
            }

            CurrentKeyboardState = Keyboard.GetCurrentState();
            _currentMouseState = Mouse.GetCurrentState();

            if (!GameOptions.IsPaused)
            {
                CollisionHelper.ApplyPlayerPhysics(this, chunkManager, (float)gameTime.ElapsedGameTime.TotalSeconds);
                UpdateMove(gameTime, chunkManager);
                UpdateMatrices();
                UpdateCurrentCursor(chunkManager);
            }

            HandleDefaultInputs(chunkManager);

            LastKeyboardStateButForRealz = LastKeyboardState;
            LastKeyboardState = CurrentKeyboardState;
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
            
            if (!CurrentKeyboardState.IsPressed(Key.P) && LastKeyboardState.IsPressed(Key.P))
            {
                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect(2, GCCollectionMode.Forced, true, true);
            }

            var delta = _currentMouseState.Z / 120;
            if (delta != 0)
            {
                _voxelDefinitionIndexInHand += delta;
                if (_voxelDefinitionIndexInHand < 0)
                {
                    _voxelDefinitionIndexInHand = VoxelDefinition.RegisteredDefinitions.Count - 1;
                }
                else
                {
                    _voxelDefinitionIndexInHand %= VoxelDefinition.RegisteredDefinitions.Count;
                }

                VoxelInHand = VoxelDefinition.DefinitionByType[VoxelDefinition.RegisteredDefinitions[_voxelDefinitionIndexInHand]];
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
                        chunkManager.TryGetVoxel(CurrentCursor.AbsoluteVoxelIndex, out var directTargetVoxel);
                        var targetBlock = directTargetVoxel.GetDefinition().IsSprite
                            ? CurrentCursor.AbsoluteVoxelIndex
                            : CurrentCursor.AbsoluteVoxelIndex + CurrentCursor.Direction;

                        var hasPositionConflict = false;
                        var min = (WorldSettings.PlayerMin + Position).Round();
                        var max = (WorldSettings.PlayerMax + Position).Round();
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
            const float SprintSpeedFactor = 10f;
            var prevPos = Position;
            var t = (float)gameTime.ElapsedGameTime.TotalSeconds;

            Velocity += WorldSettings.Gravity * t;

            if (IsInWater)
            {
                Velocity += WorldSettings.Gravity * -t;
            }

            Yaw = MathUtil.Mod2PI(Yaw + _currentMouseState.X * -MouseSensitivity); // MathUtil.Mod2PI(
            Pitch = MathUtil.Clamp(Pitch + _currentMouseState.Y * -MouseSensitivity, -MathUtil.PiOverTwo, MathUtil.PiOverTwo);

            var direction = Vector3.UnitX.Rotate(Vector3.UnitY, Yaw);
            var directionNormal = new Vector3(-direction.Z, 0, direction.X);
            var translationVector = Vector3.Zero;

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

            if (translationVector != Vector3.Zero)
            {
                Velocity += Vector3.Normalize(translationVector) * MovementSensitivity * (CurrentKeyboardState.IsPressed(Key.LeftShift) ? SprintSpeedFactor : 1);
            }

            if ((!IsInAir || IsInWater) && CurrentKeyboardState.IsPressed(Key.Space))
            {
                IsInAir = true;
                Velocity = new Vector3(Velocity.X, JumpVelocity * (CurrentKeyboardState.IsPressed(Key.LeftShift) ? SprintSpeedFactor : 1), Velocity.Z);
            }

            Velocity = new Vector3(Velocity.X * MovmentFriction, IsInWater ? Velocity.Y * MovmentFriction : Velocity.Y, Velocity.Z * MovmentFriction);
            Position += Velocity * t;

            var currentBlock = new Int3((int)Math.Round(Position.X), (int)Math.Round(Position.Y), (int)Math.Round(Position.Z));
            if (ChunkManager.TryGetChunkIndexByAbsoluteVoxelIndex(currentBlock, out var chunkIndex))
            {
                CurrentChunkIndex = chunkIndex;
            }
        }

        public void UpdateMatrices()
        {
            var rotationMatrix = Matrix.RotationYawPitchRoll(Yaw, 0, Pitch);
            LookAt = Vector3.Normalize(Vector3.Transform(InitialLookAt, rotationMatrix).ToVector3());
            View = Matrix.LookAtRH(Position, Position + LookAt, Vector3.TransformCoordinate(Vector3.UnitY, rotationMatrix));
            Projection = Matrix.PerspectiveFovRH(MathUtil.Pi / 2f, _graphics.RenderForm.Width / (float)_graphics.RenderForm.Height, 0.1f, 10000000000f);
            WorldView = World * View;
            WorldViewProjection = World * View * Projection;

            BoundingFrustum = new BoundingFrustum(View * Projection);
            LookAt2d = new Vector2(LookAt.X, LookAt.Z);
            Position2d = new Vector2(Position.X, Position.Z);
        }
    }
}
