using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using AppleCinnamon.Chunks;
using AppleCinnamon.Collision;
using AppleCinnamon.Extensions;
using AppleCinnamon.Helper;
using AppleCinnamon.Settings;
using SharpDX;
using SharpDX.DirectInput;

namespace AppleCinnamon
{
    public sealed class Camera
    {
        private readonly Graphics _graphics;
        private static readonly TimeSpan BuildCooldown = TimeSpan.FromMilliseconds(10);
        private DateTime _lastModification;

        public Vector2 Position2d { get; private set; }
        public Vector2 LookAt2d { get; private set; }

        public Vector3 Position { get; set; }
        public Vector3 LookAt { get; private set; }
        public Vector3 Velocity { get; set; }
        public Int2 CurrentChunkIndex { get; private set; }
        public Vector3 CurrentChunkIndexVector { get; private set; }
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
        public bool IsPaused { get; private set; }

        private KeyboardState _currentKeyboardState;
        private KeyboardState _lastKeyboardState;
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

            Mouse = new Mouse(directInput);
            Mouse.Properties.AxisMode = DeviceAxisMode.Relative;
            Mouse.Properties.BufferSize = 128;
            Mouse.Acquire();

            VoxelInHand = VoxelDefinition.Sand;
            _voxelDefinitionIndexInHand = VoxelDefinition.RegisteredDefinitions.IndexOf(VoxelInHand.Type);

            IsInAir = true;
        }

        public void UpdateCurrentCursor(ChunkManager chunkManager, World world)
        {
            CurrentCursor = CollisionHelper.GetCurrentSelection(new Ray(Position, LookAt), chunkManager);

            if (chunkManager.TryGetVoxel(Position.Round(), out var voxel) &&
                voxel.BlockType == VoxelDefinition.Water.Type)
            {
                IsInWater = true;
            }
            else
            {
                IsInWater = false;
            }
        }

        public void Update(GameTime gameTime, ChunkManager chunkManager, World world)
        {
            if (!chunkManager.IsInitialized)
            {
                return;
            }

            _currentKeyboardState = Keyboard.GetCurrentState();
            _currentMouseState = Mouse.GetCurrentState();

            if (!IsPaused)
            {

                CollisionHelper.ApplyPlayerPhysics(this, chunkManager, (float)gameTime.ElapsedGameTime.TotalSeconds);
                UpdateMove(gameTime, chunkManager, world);
                UpdateMatrices();

                UpdateCurrentCursor(chunkManager, world);
            }

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
                Game.RenderSolid = !Game.RenderSolid;
            }

            if (!_currentKeyboardState.IsPressed(Key.F2) && _lastKeyboardState.IsPressed(Key.F2))
            {
                Game.RenderWater = !Game.RenderWater;
            }

            if (!_currentKeyboardState.IsPressed(Key.F3) && _lastKeyboardState.IsPressed(Key.F3))
            {
                Game.RenderSprites = !Game.RenderSprites;
            }

            if (!_currentKeyboardState.IsPressed(Key.F4) && _lastKeyboardState.IsPressed(Key.F4))
            {
                Game.RenderBoxes = !Game.RenderBoxes;
            }

            if (!_currentKeyboardState.IsPressed(Key.F5) && _lastKeyboardState.IsPressed(Key.F5))
            {
                Game.ShowChunkBoundingBoxes = !Game.ShowChunkBoundingBoxes;
            }

            if (!_currentKeyboardState.IsPressed(Key.F6) && _lastKeyboardState.IsPressed(Key.F6))
            {
                Game.RenderSky = !Game.RenderSky;
            }

            if (!_currentKeyboardState.IsPressed(Key.F7) && _lastKeyboardState.IsPressed(Key.F7))
            {
                Game.ShowPipelineVisualization = !Game.ShowPipelineVisualization;
            }

            if (!_currentKeyboardState.IsPressed(Key.F12) && _lastKeyboardState.IsPressed(Key.F12))
            {
                Game.Debug = !Game.Debug;
            }

            if (!_currentKeyboardState.IsPressed(Key.P) && _lastKeyboardState.IsPressed(Key.P))
            {
                var chs = ChunkManager.Chunks.Select(s => s.Value).Where(s => s.State == ChunkState.Finished).ToList();
                List<Action> tasks = new List<Action>();
                foreach (var chk in chs)
                {
                    chk.BuildingContext.IsSpriteChanged = true;
                    chk.BuildingContext.IsWaterChanged = true;
                    chk.BuildingContext.IsSolidChanged = true;
                    tasks.Add(new Action(() =>
                    {
                        chk.Voxels = chk.Voxels.ToList().ToArray();
                        ChunkBuilder.BuildChunk(chk, _graphics.Device);
                    }));
                }

                Parallel.ForEach(tasks, tsk =>
                {
                    tsk.Invoke();
                });

            }

            if (!_currentKeyboardState.IsPressed(Key.Q) && _lastKeyboardState.IsPressed(Key.Q))
            {
                var chks = ChunkManager.Chunks.Where(c => (c.Key - CurrentChunkIndex).Length() > 6).Select(s => s.Value).ToList();
                foreach (var chk in chks)
                {
                    ChunkManager.Chunks.Remove(chk.ChunkIndex, out var _);
                    chk.Kill(_graphics.Device);
                    ChunkManager.Graveyard.Add(chk);
                }
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



        private void UpdateMove(GameTime gameTime, ChunkManager chunkManager, World world)
        {
            //MessageBox.Show(Thread.CurrentThread.ManagedThreadId.ToString());

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
            Pitch = MathUtil.Clamp(Pitch + _currentMouseState.Y * -MouseSensitivity, -MathUtil.PiOverTwo,
                MathUtil.PiOverTwo);


            var direction = Vector3.UnitX.Rotate(Vector3.UnitY, Yaw);
            var directionNormal = new Vector3(-direction.Z, 0, direction.X);
            var translationVector = Vector3.Zero;

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

            if (translationVector != Vector3.Zero)
            {
                Velocity += Vector3.Normalize(translationVector) * MovementSensitivity *
                            (_currentKeyboardState.IsPressed(Key.LeftShift) ? SprintSpeedFactor : 1);
            }

            const float lilStep = 0.001f;

            if (_currentKeyboardState.IsPressed(Key.NumberPad0)) Hofman.SunIntensity += lilStep;
            if (_currentKeyboardState.IsPressed(Key.NumberPad1)) Hofman.SunIntensity -= lilStep;

            if (_currentKeyboardState.IsPressed(Key.NumberPad2)) Hofman.Turbitity += lilStep;
            if (_currentKeyboardState.IsPressed(Key.NumberPad3)) Hofman.Turbitity -= lilStep;

            if (_currentKeyboardState.IsPressed(Key.NumberPad4)) Hofman.InscatteringMultiplier += lilStep;
            if (_currentKeyboardState.IsPressed(Key.NumberPad5)) Hofman.InscatteringMultiplier -= lilStep;

            if (_currentKeyboardState.IsPressed(Key.NumberPad6)) Hofman.BetaRayMultiplier += lilStep;
            if (_currentKeyboardState.IsPressed(Key.NumberPad7)) Hofman.BetaRayMultiplier -= lilStep;

            if (_currentKeyboardState.IsPressed(Key.NumberPad8)) Hofman.BetaMieMultiplier += lilStep / 100f;
            if (_currentKeyboardState.IsPressed(Key.NumberPad9)) Hofman.BetaMieMultiplier -= lilStep / 100f;

            if (_currentKeyboardState.IsPressed(Key.Up)) Hofman.SunDirection += lilStep / 1;
            if (_currentKeyboardState.IsPressed(Key.Down)) Hofman.SunDirection -= lilStep / 1;


            if (_currentKeyboardState.IsPressed(Key.Up)) world.IncreaseTime();
            if (_currentKeyboardState.IsPressed(Key.Down)) world.DecreaseTime();



            if ((!IsInAir || IsInWater) && _currentKeyboardState.IsPressed(Key.Space))
            {
                IsInAir = true;
                Velocity = new Vector3(Velocity.X,
                    JumpVelocity * (_currentKeyboardState.IsPressed(Key.LeftShift) ? SprintSpeedFactor : 1), Velocity.Z);
            }


            Velocity = new Vector3(Velocity.X * MovmentFriction, IsInWater ? Velocity.Y * MovmentFriction : Velocity.Y, Velocity.Z * MovmentFriction);
            Position = Position + Velocity * t;



            var currentBlock = new Int3((int)Math.Round(Position.X), (int)Math.Round(Position.Y), (int)Math.Round(Position.Z));
            if (ChunkManager.TryGetChunkIndexByAbsoluteVoxelIndex(currentBlock, out var chunkIndex))
            {
                CurrentChunkIndex = chunkIndex;
                var currentChunk = ChunkManager.Chunks[chunkIndex];
                CurrentChunkIndexVector = currentChunk.BoundingBox.Center;
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
            LookAt2d = new Vector2((float)LookAt.X, (float)LookAt.Z);
            Position2d = new Vector2((float)Position.X, (float)Position.Z);
        }
    }
}
