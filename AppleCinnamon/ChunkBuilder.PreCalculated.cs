using AppleCinnamon.System;
using SharpDX;

namespace AppleCinnamon
{
    public sealed partial class ChunkBuilder
    {
      
        private static readonly Vector3 TopLefFro = new Vector3(-.5f, +.5f, -.5f);
        private static readonly Vector3 TopRigFro = new Vector3(+.5f, +.5f, -.5f);
        private static readonly Vector3 TopLefBac = new Vector3(-.5f, +.5f, +.5f);
        private static readonly Vector3 TopRigBac = new Vector3(+.5f, +.5f, +.5f);
        private static readonly Vector3 BotLefFro = new Vector3(-.5f, -.5f, -.5f);
        private static readonly Vector3 BotLefBac = new Vector3(-.5f, -.5f, +.5f);
        private static readonly Vector3 BotRigFro = new Vector3(+.5f, -.5f, -.5f);
        private static readonly Vector3 BotRigBac = new Vector3(+.5f, -.5f, +.5f);

        private static readonly Int3 TopLefFroInd = new Int3(0, 1, 0);
        private static readonly Int3 TopRigFroInd = new Int3(1, 1, 0);
        private static readonly Int3 TopLefBacInd = new Int3(0, 1, 1);
        private static readonly Int3 TopRigBacInd = new Int3(1, 1, 1);
        private static readonly Int3 BotLefFroInd = new Int3(0, 0, 0);
        private static readonly Int3 BotLefBacInd = new Int3(0, 0, 1);
        private static readonly Int3 BotRigFroInd = new Int3(1, 0, 0);
        private static readonly Int3 BotRigBacInd = new Int3(1, 0, 1);

        public static readonly Cube<Vector3[]> FaceVertices =
            new Cube<Vector3[]>(
                new[] { TopLefFro, TopRigFro, TopRigBac, TopLefBac },
                new[] { BotRigFro, BotLefFro, BotLefBac, BotRigBac },
                new[] { TopLefFro, TopLefBac, BotLefBac, BotLefFro },
                new[] { TopRigBac, TopRigFro, BotRigFro, BotRigBac },
                new[] { TopRigFro, TopLefFro, BotLefFro, BotRigFro },
                new[] { TopLefBac, TopRigBac, BotRigBac, BotLefBac }
            );

        public static readonly Cube<Int3[]> FaceVerticesIndex =
            new Cube<Int3[]>(
                new[] { TopLefFroInd, TopRigFroInd, TopRigBacInd, TopLefBacInd },
                new[] { BotRigFroInd, BotLefFroInd, BotLefBacInd, BotRigBacInd },
                new[] { TopLefFroInd, TopLefBacInd, BotLefBacInd, BotLefFroInd },
                new[] { TopRigBacInd, TopRigFroInd, BotRigFroInd, BotRigBacInd },
                new[] { TopRigFroInd, TopLefFroInd, BotLefFroInd, BotRigFroInd },
                new[] { TopLefBacInd, TopRigBacInd, BotRigBacInd, BotLefBacInd }
            );

        public const float TextureThreshold = 0.0002f;
        public const float UvStep = 1 / 16f;
        private static readonly Vector2 UvOffset = new Vector2(TextureThreshold);
        private static readonly Vector2[] UvOffsets = { Vector2.Zero + UvOffset, new Vector2(UvStep - TextureThreshold, TextureThreshold), new Vector2(UvStep - TextureThreshold, UvStep - TextureThreshold), new Vector2(TextureThreshold, UvStep - TextureThreshold) };
        private static readonly Int2[] UvIndexOffsets = { new Int2(0,0), new Int2(1, 0), new Int2(1, 1), new Int2(0, 1) };
        private static readonly Vector2[] WaterUvOffsets = { Vector2.Zero, new Vector2(1, 0), new Vector2(1, 1 / 32f), new Vector2(0, 1 / 32f) };

        public static readonly Cube<Int3[][]> AmbientIndexes = new Cube<Int3[][]>(
            new[]
            {
                new[] {new Int3(0, 1, -1), new Int3(-1, 1, -1), new Int3(-1, 1, 0)},
                new[] {new Int3(1, 1, 0), new Int3(1, 1, -1), new Int3(0, 1, -1)},
                new[] {new Int3(0, 1, 1), new Int3(1, 1, 1), new Int3(1, 1, 0)},
                new[] {new Int3(-1, 1, 0), new Int3(-1, 1, 1), new Int3(0, 1, 1)}
            },
            new[]
            {
                new[] {new Int3(1, -1, 0), new Int3(1, -1, -1), new Int3(0, -1, -1)},
                new[] {new Int3(0, -1, -1), new Int3(-1, -1, -1), new Int3(-1, -1, 0)},
                new[] {new Int3(-1, -1, 0), new Int3(-1, -1, 1), new Int3(0, -1, 1)},
                new[] {new Int3(0, -1, 1), new Int3(1, -1, 1), new Int3(1, -1, 0)}
            },
            new[]
            {
                new[] {new Int3(-1, 0, -1), new Int3(-1, 1, -1), new Int3(-1, 1, 0)},
                new[] {new Int3(-1, 1, 0), new Int3(-1, 1, 1), new Int3(-1, 0, 1)},
                new[] {new Int3(-1, 0, 1), new Int3(-1, -1, 1), new Int3(-1, -1, 0)},
                new[] {new Int3(-1, -1, 0), new Int3(-1, -1, -1), new Int3(-1, 0, -1)}
            },
            new[]
            {
                new[] {new Int3(1, 1, 0), new Int3(1, 1, 1), new Int3(1, 0, 1)},
                new[] {new Int3(1, 0, -1), new Int3(1, 1, -1), new Int3(1, 1, 0)},
                new[] {new Int3(1, -1, 0), new Int3(1, -1, -1), new Int3(1, 0, -1)},
                new[] {new Int3(1, 0, 1), new Int3(1, -1, 1), new Int3(1, -1, 0)}
            },
            new[]
            {
                new[] {new Int3(0, 1, -1), new Int3(1, 1, -1), new Int3(1, 0, -1)},
                new[] {new Int3(-1, 0, -1), new Int3(-1, 1, -1), new Int3(0, 1, -1)},
                new[] {new Int3(0, -1, -1), new Int3(-1, -1, -1), new Int3(-1, 0, -1)},
                new[] {new Int3(1, 0, -1), new Int3(1, -1, -1), new Int3(0, -1, -1)}
            },
            new[]
            {
                new[] {new Int3(-1, 0, 1), new Int3(-1, 1, 1), new Int3(0, 1, 1)},
                new[] {new Int3(0, 1, 1), new Int3(1, 1, 1), new Int3(1, 0, 1)},
                new[] {new Int3(1, 0, 1), new Int3(1, -1, 1), new Int3(0, -1, 1)},
                new[] {new Int3(0, -1, 1), new Int3(-1, -1, 1), new Int3(-1, 0, 1)}
            }
        );
    }
}
