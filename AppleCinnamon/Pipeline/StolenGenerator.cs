using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AppleCinnamon.Settings;
using SharpDX;
using AppleCinnamon.System;

namespace AppleCinnamon.Pipeline
{
    public abstract class IMapGenerator
    {

        public abstract string GeneratorName { get; }

        public abstract Voxel[] Generate();

        public virtual void ApplyEnv() { }

        public string CurrentState;

        public float CurrentProgress;

        public bool Done = false;

        public volatile Voxel[] Blocks;

        public int Width, Height, Length, Seed;

        public void GenerateAsync(Game game)
        {
            Thread thread = new Thread(DoGenerate);
            thread.IsBackground = true;
            thread.Name = "IMapGenerator.GenAsync()";
            thread.Start();
        }

        void DoGenerate()
        {
            Blocks = Generate();
            Done = true;
        }
    }


    public sealed class ImprovedNoise
    {

        public ImprovedNoise(JavaRandom rnd)
        {
            // shuffle randomly using fisher-yates
            for (int i = 0; i < 256; i++)
                p[i] = (byte)i;

            for (int i = 0; i < 256; i++)
            {
                int j = rnd.Next(i, 256);
                byte temp = p[i]; p[i] = p[j]; p[j] = temp;
            }
            for (int i = 0; i < 256; i++)
                p[i + 256] = p[i];
        }

        public double Compute(double x, double y)
        {
            int xFloor = x >= 0 ? (int)x : (int)x - 1;
            int yFloor = y >= 0 ? (int)y : (int)y - 1;
            int X = xFloor & 0xFF, Y = yFloor & 0xFF;
            x -= xFloor; y -= yFloor;

            double u = x * x * x * (x * (x * 6 - 15) + 10); // Fade(x)
            double v = y * y * y * (y * (y * 6 - 15) + 10); // Fade(y)
            int A = p[X] + Y, B = p[X + 1] + Y;

            // Normally, calculating Grad involves a function call. However, we can directly pack this table
            // (since each value indicates either -1, 0 1) into a set of bit flags. This way we avoid needing 
            // to call another function that performs branching
            const int xFlags = 0x46552222, yFlags = 0x2222550A;

            int hash = (p[p[A]] & 0xF) << 1;
            double g22 = (((xFlags >> hash) & 3) - 1) * x + (((yFlags >> hash) & 3) - 1) * y; // Grad(p[p[A], x, y)
            hash = (p[p[B]] & 0xF) << 1;
            double g12 = (((xFlags >> hash) & 3) - 1) * (x - 1) + (((yFlags >> hash) & 3) - 1) * y; // Grad(p[p[B], x - 1, y)
            double c1 = g22 + u * (g12 - g22);

            hash = (p[p[A + 1]] & 0xF) << 1;
            double g21 = (((xFlags >> hash) & 3) - 1) * x + (((yFlags >> hash) & 3) - 1) * (y - 1); // Grad(p[p[A + 1], x, y - 1)
            hash = (p[p[B + 1]] & 0xF) << 1;
            double g11 = (((xFlags >> hash) & 3) - 1) * (x - 1) + (((yFlags >> hash) & 3) - 1) * (y - 1); // Grad(p[p[B + 1], x - 1, y - 1)
            double c2 = g21 + u * (g11 - g21);

            return c1 + v * (c2 - c1);
        }

        byte[] p = new byte[512];
    }

    public sealed class OctaveNoise
    {

        readonly ImprovedNoise[] baseNoise;
        public OctaveNoise(int octaves, JavaRandom rnd)
        {
            baseNoise = new ImprovedNoise[octaves];
            for (int i = 0; i < octaves; i++)
                baseNoise[i] = new ImprovedNoise(rnd);
        }

        public double Compute(double x, double y)
        {
            double amplitude = 1, frequency = 1;
            double sum = 0;
            for (int i = 0; i < baseNoise.Length; i++)
            {
                sum += baseNoise[i].Compute(x * frequency, y * frequency) * amplitude;
                amplitude *= 2.0;
                frequency *= 0.5;
            }
            return sum;
        }
    }

    public sealed class CombinedNoise
    {

        readonly OctaveNoise noise1, noise2;
        public CombinedNoise(OctaveNoise noise1, OctaveNoise noise2)
        {
            this.noise1 = noise1;
            this.noise2 = noise2;
        }

        public double Compute(double x, double y)
        {
            double offset = noise2.Compute(x, y);
            return noise1.Compute(x + offset, y);
        }
    }

    public sealed partial class NotchyGenerator
    {

        void FillOblateSpheroid(int x, int y, int z, float radius, VoxelDefinition block)
        {
            int xStart = Math.Max(x - radius, 0).Floor();
            int xEnd = Math.Min(x + radius, Chunk.SizeXy - 1).Floor();
            int yStart = Math.Max(y - radius, 0).Floor();
            int yEnd = Math.Min(y + radius, Chunk.Height - 1).Floor();
            int zStart = Math.Max(z - radius, 0).Floor();
            int zEnd = Math.Min(z + radius, Chunk.SizeXy - 1).Floor();
            float radiusSq = radius * radius;

            for (int yy = yStart; yy <= yEnd; yy++)
                for (int zz = zStart; zz <= zEnd; zz++)
                    for (int xx = xStart; xx <= xEnd; xx++)
                    {
                        int dx = xx - x, dy = yy - y, dz = zz - z;
                        if ((dx * dx + 2 * dy * dy + dz * dz) < radiusSq)
                        {
                            int index = (yy * Chunk.SizeXy + zz) * Chunk.SizeXy + xx;
                            if (blocks[index].Block == VoxelDefinition.Stone.Type)
                                blocks[index] = new Voxel(block.Type, 0);
                        }
                    }
        }

        void FloodFill(int startIndex, Voxel block)
        {
            if (startIndex < 0) return; // y below map, immediately ignore
            FastIntStack stack = new FastIntStack(4);
            stack.Push(startIndex);

            while (stack.Size > 0)
            {
                int index = stack.Pop();
                if (blocks[index].Block != VoxelDefinition.Air.Type) continue;
                blocks[index] = block;

                int x = index % Chunk.SizeXy;
                int y = index / oneY;
                int z = (index / Chunk.SizeXy) % Chunk.SizeXy;

                if (x > 0) stack.Push(index - 1);
                if (x < Chunk.SizeXy - 1) stack.Push(index + 1);
                if (z > 0) stack.Push(index - Chunk.SizeXy);
                if (z < Chunk.SizeXy - 1) stack.Push(index + Chunk.SizeXy);
                if (y > 0) stack.Push(index - oneY);
            }
        }

        sealed class FastIntStack
        {
            public int[] Values;
            public int Size;

            public FastIntStack(int capacity)
            {
                Values = new int[capacity];
                Size = 0;
            }

            public int Pop()
            {
                return Values[--Size];
            }

            public void Push(int item)
            {
                if (Size == Values.Length)
                {
                    int[] array = new int[Values.Length * 2];
                    Buffer.BlockCopy(Values, 0, array, 0, Size * sizeof(int));
                    Values = array;
                }
                Values[Size++] = item;
            }
        }
    }

    // Based on https://docs.oracle.com/javase/7/docs/api/java/util/Random.html
    public sealed class JavaRandom
    {

        long seed;
        const long value = 0x5DEECE66DL;
        const long mask = (1L << 48) - 1;

        public JavaRandom(int seed) { SetSeed(seed); }
        public void SetSeed(int seed)
        {
            this.seed = (seed ^ value) & mask;
        }

        public int Next(int min, int max) { return min + Next(max - min); }

        public int Next(int n)
        {
            if ((n & -n) == n)
            { // i.e., n is a power of 2
                seed = (seed * value + 0xBL) & mask;
                long raw = (long)((ulong)seed >> (48 - 31));
                return (int)((n * raw) >> 31);
            }

            int bits, val;
            do
            {
                seed = (seed * value + 0xBL) & mask;
                bits = (int)((ulong)seed >> (48 - 31));
                val = bits % n;
            } while (bits - val + (n - 1) < 0);
            return val;
        }

        public float NextFloat()
        {
            seed = (seed * value + 0xBL) & mask;
            int raw = (int)((ulong)seed >> (48 - 24));
            return raw / ((float)(1 << 24));
        }
    }

    public sealed partial class NotchyGenerator : IMapGenerator
    {

        int waterLevel, oneY;
        Voxel[] blocks;
        short[] heightmap;
        JavaRandom rnd;
        int minHeight;

        public override string GeneratorName { get { return "Vanilla classic"; } }

        public override Voxel[] Generate()
        {
            blocks = new Voxel[Width * Height * Length];
            rnd = new JavaRandom(Seed);
            oneY = Width * Length;
            waterLevel = Height / 2;
            minHeight = Height;

            CreateHeightmap();
            CreateStrata();
            CarveCaves();
            CarveOreVeins(0.9f, "coal ore", VoxelDefinition.CoalOre);
            CarveOreVeins(0.7f, "iron ore", VoxelDefinition.IronOre);
            CarveOreVeins(0.5f, "gold ore", VoxelDefinition.GoldOre);

            FloodFillWaterBorders();
            FloodFillWater();
            FloodFillLava();

            CreateSurfaceLayer();
            PlantFlowers();
            PlantMushrooms();
            PlantTrees();
            return blocks;
        }

        void CreateHeightmap()
        {
            CombinedNoise n1 = new CombinedNoise(
                new OctaveNoise(8, rnd), new OctaveNoise(8, rnd));
            CombinedNoise n2 = new CombinedNoise(
                new OctaveNoise(8, rnd), new OctaveNoise(8, rnd));
            OctaveNoise n3 = new OctaveNoise(6, rnd);
            int index = 0;
            short[] hMap = new short[Width * Length];
            CurrentState = "Building heightmap";

            for (int z = 0; z < Length; z++)
            {
                CurrentProgress = (float)z / Length;
                for (int x = 0; x < Width; x++)
                {
                    double hLow = n1.Compute(x * 1.3f, z * 1.3f) / 6 - 4, height = hLow;

                    if (n3.Compute(x, z) <= 0)
                    {
                        double hHigh = n2.Compute(x * 1.3f, z * 1.3f) / 5 + 6;
                        height = Math.Max(hLow, hHigh);
                    }

                    height *= 0.5;
                    if (height < 0) height *= 0.8f;

                    int adjHeight = (int)(height + waterLevel);
                    minHeight = adjHeight < minHeight ? adjHeight : minHeight;
                    hMap[index++] = (short)adjHeight;
                }
            }
            heightmap = hMap;
        }

        void CreateStrata()
        {
            OctaveNoise n = new OctaveNoise(8, rnd);
            CurrentState = "Creating strata";
            int hMapIndex = 0, maxY = Height - 1, mapIndex = 0;
            // Try to bulk fill bottom of the map if possible
            int minStoneY = CreateStrataFast();

            for (int z = 0; z < Length; z++)
            {
                CurrentProgress = (float)z / Length;
                for (int x = 0; x < Width; x++)
                {
                    int dirtThickness = (int)(n.Compute(x, z) / 24 - 4);
                    int dirtHeight = heightmap[hMapIndex++];
                    int stoneHeight = dirtHeight + dirtThickness;

                    stoneHeight = Math.Min(stoneHeight, maxY);
                    dirtHeight = Math.Min(dirtHeight, maxY);

                    mapIndex = minStoneY * oneY + z * Width + x;
                    for (int y = minStoneY; y <= stoneHeight; y++)
                    {
                        blocks[mapIndex] = new Voxel(VoxelDefinition.Stone.Type, 0); mapIndex += oneY;
                    }

                    stoneHeight = Math.Max(stoneHeight, 0);
                    mapIndex = (stoneHeight + 1) * oneY + z * Width + x;
                    for (int y = stoneHeight + 1; y <= dirtHeight; y++)
                    {
                        blocks[mapIndex] = new Voxel(VoxelDefinition.Dirt.Type, 0); mapIndex += oneY;
                    }
                }
            }
        }

        int CreateStrataFast()
        {
            // Make lava layer at bottom
            int mapIndex = 0;
            for (int z = 0; z < Length; z++)
                for (int x = 0; x < Width; x++)
                {
                    blocks[mapIndex++] = new Voxel(VoxelDefinition.Lava.Type, 0);
                }

            // Invariant: the lowest value dirtThickness can possible be is -14
            int stoneHeight = minHeight - 14;
            if (stoneHeight <= 0) return 1; // no layer is fully stone

            // We can quickly fill in bottom solid layers
            for (int y = 1; y <= stoneHeight; y++)
                for (int z = 0; z < Length; z++)
                    for (int x = 0; x < Width; x++)
                    {
                        blocks[mapIndex++] = new Voxel(VoxelDefinition.Stone.Type, 0);
                    }
            return stoneHeight;
        }

        void CarveCaves()
        {
            int cavesCount = blocks.Length / 8192;
            CurrentState = "Carving caves";

            for (int i = 0; i < cavesCount; i++)
            {
                CurrentProgress = (float)i / cavesCount;
                double caveX = rnd.Next(Width);
                double caveY = rnd.Next(Height);
                double caveZ = rnd.Next(Length);

                int caveLen = (int)(rnd.NextFloat() * rnd.NextFloat() * 200);
                double theta = rnd.NextFloat() * 2 * Math.PI, deltaTheta = 0;
                double phi = rnd.NextFloat() * 2 * Math.PI, deltaPhi = 0;
                double caveRadius = rnd.NextFloat() * rnd.NextFloat();

                for (int j = 0; j < caveLen; j++)
                {
                    caveX += Math.Sin(theta) * Math.Cos(phi);
                    caveZ += Math.Cos(theta) * Math.Cos(phi);
                    caveY += Math.Sin(phi);

                    theta = theta + deltaTheta * 0.2;
                    deltaTheta = deltaTheta * 0.9 + rnd.NextFloat() - rnd.NextFloat();
                    phi = phi / 2 + deltaPhi / 4;
                    deltaPhi = deltaPhi * 0.75 + rnd.NextFloat() - rnd.NextFloat();
                    if (rnd.NextFloat() < 0.25) continue;

                    int cenX = (int)(caveX + (rnd.Next(4) - 2) * 0.2);
                    int cenY = (int)(caveY + (rnd.Next(4) - 2) * 0.2);
                    int cenZ = (int)(caveZ + (rnd.Next(4) - 2) * 0.2);

                    double radius = (Height - cenY) / (double)Height;
                    radius = 1.2 + (radius * 3.5 + 1) * caveRadius;
                    radius = radius * Math.Sin(j * Math.PI / caveLen);
                    FillOblateSpheroid(cenX, cenY, cenZ, (float)radius, VoxelDefinition.Air);
                }
            }
        }

        void CarveOreVeins(float abundance, string blockName, VoxelDefinition block)
        {
            int numVeins = (int)(blocks.Length * abundance / 16384);
            CurrentState = "Carving " + blockName;

            for (int i = 0; i < numVeins; i++)
            {
                CurrentProgress = (float)i / numVeins;
                double veinX = rnd.Next(Width);
                double veinY = rnd.Next(Height);
                double veinZ = rnd.Next(Length);

                int veinLen = (int)(rnd.NextFloat() * rnd.NextFloat() * 75 * abundance);
                double theta = rnd.NextFloat() * 2 * Math.PI, deltaTheta = 0;
                double phi = rnd.NextFloat() * 2 * Math.PI, deltaPhi = 0;

                for (int j = 0; j < veinLen; j++)
                {
                    veinX += Math.Sin(theta) * Math.Cos(phi);
                    veinZ += Math.Cos(theta) * Math.Cos(phi);
                    veinY += Math.Sin(phi);

                    theta = deltaTheta * 0.2;
                    deltaTheta = deltaTheta * 0.9 + rnd.NextFloat() - rnd.NextFloat();
                    phi = phi / 2 + deltaPhi / 4;
                    deltaPhi = deltaPhi * 0.9 + rnd.NextFloat() - rnd.NextFloat();

                    float radius = abundance * (float)Math.Sin(j * Math.PI / veinLen) + 1;
                    FillOblateSpheroid((int)veinX, (int)veinY, (int)veinZ, radius, block);
                }
            }
        }

        void FloodFillWaterBorders()
        {
            int waterY = waterLevel - 1;
            int index1 = (waterY * Length + 0) * Width + 0;
            int index2 = (waterY * Length + (Length - 1)) * Width + 0;
            CurrentState = "Flooding edge water";

            for (int x = 0; x < Width; x++)
            {
                CurrentProgress = 0 + ((float)x / Width) * 0.5f;
                FloodFill(index1, new Voxel(VoxelDefinition.Water.Type, 0));
                FloodFill(index2, new Voxel(VoxelDefinition.Water.Type, 0));
                index1++; index2++;
            }

            index1 = (waterY * Length + 0) * Width + 0;
            index2 = (waterY * Length + 0) * Width + (Width - 1);
            for (int z = 0; z < Length; z++)
            {
                CurrentProgress = 0.5f + ((float)z / Length) * 0.5f;
                FloodFill(index1, new Voxel(VoxelDefinition.Water.Type, 0));
                FloodFill(index2, new Voxel(VoxelDefinition.Water.Type, 0));
                index1 += Width; index2 += Width;
            }
        }

        void FloodFillWater()
        {
            int numSources = Width * Length / 800;
            CurrentState = "Flooding water";

            for (int i = 0; i < numSources; i++)
            {
                CurrentProgress = (float)i / numSources;
                int x = rnd.Next(Width), z = rnd.Next(Length);
                int y = waterLevel - rnd.Next(1, 3);
                FloodFill((y * Length + z) * Width + x, new Voxel(VoxelDefinition.Water.Type, 0));
            }
        }

        void FloodFillLava()
        {
            int numSources = Width * Length / 20000;
            CurrentState = "Flooding lava";

            for (int i = 0; i < numSources; i++)
            {
                CurrentProgress = (float)i / numSources;
                int x = rnd.Next(Width), z = rnd.Next(Length);
                int y = (int)((waterLevel - 3) * rnd.NextFloat() * rnd.NextFloat());
                FloodFill((y * Length + z) * Width + x, new Voxel(VoxelDefinition.Lava.Type, 0));
            }
        }

        void CreateSurfaceLayer()
        {
            OctaveNoise n1 = new OctaveNoise(8, rnd), n2 = new OctaveNoise(8, rnd);
            CurrentState = "Creating surface";
            // TODO: update heightmap

            int hMapIndex = 0;
            for (int z = 0; z < Length; z++)
            {
                CurrentProgress = (float)z / Length;
                for (int x = 0; x < Width; x++)
                {
                    int y = heightmap[hMapIndex++];
                    if (y < 0 || y >= Height) continue;

                    int index = (y * Length + z) * Width + x;
                    Voxel blockAbove = y >= (Height - 1) ? Voxel.Zero : blocks[index + oneY];
                    if (blockAbove.Block == VoxelDefinition.Water.Type && (n2.Compute(x, z) > 12))
                    {
                        blocks[index] = new Voxel(VoxelDefinition.Gravel.Type, 0);
                    }
                    else if (blockAbove.Block == VoxelDefinition.Air.Type)
                    {
                        blocks[index] = (y <= waterLevel && (n1.Compute(x, z) > 8)) 
                            ? new Voxel(VoxelDefinition.Sand.Type, 0) 
                            : new Voxel(VoxelDefinition.Grass.Type, 0) ;
                    }
                }
            }
        }

        void PlantFlowers()
        {
            return;

            //int numPatches = Width * Length / 3000;
            //CurrentState = "Planting flowers";

            //for (int i = 0; i < numPatches; i++)
            //{
            //    CurrentProgress = (float)i / numPatches;
            //    Voxel type = (Voxel)(VoxelDefinition.Dandelion + rnd.Next(2));
            //    int patchX = rnd.Next(Width), patchZ = rnd.Next(Length);
            //    for (int j = 0; j < 10; j++)
            //    {
            //        int flowerX = patchX, flowerZ = patchZ;
            //        for (int k = 0; k < 5; k++)
            //        {
            //            flowerX += rnd.Next(6) - rnd.Next(6);
            //            flowerZ += rnd.Next(6) - rnd.Next(6);
            //            if (flowerX < 0 || flowerZ < 0 || flowerX >= Width || flowerZ >= Length)
            //                continue;

            //            int flowerY = heightmap[flowerZ * Width + flowerX] + 1;
            //            if (flowerY <= 0 || flowerY >= Height) continue;

            //            int index = (flowerY * Length + flowerZ) * Width + flowerX;
            //            if (blocks[index] == VoxelDefinition.Air && blocks[index - oneY] == VoxelDefinition.Grass)
            //                blocks[index] = type;
            //        }
            //    }
            //}
        }

        void PlantMushrooms()
        {
            return;
            //int numPatches = blocks.Length / 2000;
            //CurrentState = "Planting mushrooms";

            //for (int i = 0; i < numPatches; i++)
            //{
            //    CurrentProgress = (float)i / numPatches;
            //    BlockRaw type = (BlockRaw)(VoxelDefinition.BrownMushroom + rnd.Next(2));
            //    int patchX = rnd.Next(Width);
            //    int patchY = rnd.Next(Height);
            //    int patchZ = rnd.Next(Length);

            //    for (int j = 0; j < 20; j++)
            //    {
            //        int mushX = patchX, mushY = patchY, mushZ = patchZ;
            //        for (int k = 0; k < 5; k++)
            //        {
            //            mushX += rnd.Next(6) - rnd.Next(6);
            //            mushZ += rnd.Next(6) - rnd.Next(6);
            //            if (mushX < 0 || mushZ < 0 || mushX >= Width || mushZ >= Length)
            //                continue;
            //            int solidHeight = heightmap[mushZ * Width + mushX];
            //            if (mushY >= (solidHeight - 1))
            //                continue;

            //            int index = (mushY * Length + mushZ) * Width + mushX;
            //            if (blocks[index] == VoxelDefinition.Air && blocks[index - oneY] == VoxelDefinition.Stone)
            //                blocks[index] = type;
            //        }
            //    }
            //}
        }

        void PlantTrees()
        {
            int numPatches = Width * Length / 4000;
            CurrentState = "Planting trees";

            for (int i = 0; i < numPatches; i++)
            {
                CurrentProgress = (float)i / numPatches;
                int patchX = rnd.Next(Width), patchZ = rnd.Next(Length);

                for (int j = 0; j < 20; j++)
                {
                    int treeX = patchX, treeZ = patchZ;
                    for (int k = 0; k < 20; k++)
                    {
                        treeX += rnd.Next(6) - rnd.Next(6);
                        treeZ += rnd.Next(6) - rnd.Next(6);
                        if (treeX < 0 || treeZ < 0 || treeX >= Width ||
                            treeZ >= Length || rnd.NextFloat() >= 0.25)
                            continue;

                        int treeY = heightmap[treeZ * Width + treeX] + 1;
                        if (treeY >= Height) continue;
                        int treeHeight = 5 + rnd.Next(3);

                        int index = (treeY * Length + treeZ) * Width + treeX;
                        Voxel blockUnder = treeY > 0 ? blocks[index - oneY] : Voxel.Zero;

                        if (blockUnder.Block == VoxelDefinition.Grass.Type && CanGrowTree(treeX, treeY, treeZ, treeHeight))
                        {
                            GrowTree(treeX, treeY, treeZ, treeHeight);
                        }
                    }
                }
            }
        }

        bool CanGrowTree(int treeX, int treeY, int treeZ, int treeHeight)
        {
            // check tree base
            int baseHeight = treeHeight - 4;
            for (int y = treeY; y < treeY + baseHeight; y++)
                for (int z = treeZ - 1; z <= treeZ + 1; z++)
                    for (int x = treeX - 1; x <= treeX + 1; x++)
                    {
                        if (x < 0 || y < 0 || z < 0 || x >= Width || y >= Height || z >= Length)
                            return false;
                        int index = (y * Length + z) * Width + x;
                        if (blocks[index].Block != 0) return false;
                    }

            // and also check canopy
            for (int y = treeY + baseHeight; y < treeY + treeHeight; y++)
                for (int z = treeZ - 2; z <= treeZ + 2; z++)
                    for (int x = treeX - 2; x <= treeX + 2; x++)
                    {
                        if (x < 0 || y < 0 || z < 0 || x >= Width || y >= Height || z >= Length)
                            return false;
                        int index = (y * Length + z) * Width + x;
                        if (blocks[index].Block != 0) return false;
                    }
            return true;
        }

        void GrowTree(int treeX, int treeY, int treeZ, int height)
        {
            int baseHeight = height - 4;
            int index = 0;

            // leaves bottom layer
            for (int y = treeY + baseHeight; y < treeY + baseHeight + 2; y++)
                for (int zz = -2; zz <= 2; zz++)
                    for (int xx = -2; xx <= 2; xx++)
                    {
                        int x = xx + treeX, z = zz + treeZ;
                        index = (y * Length + z) * Width + x;

                        if (Math.Abs(xx) == 2 && Math.Abs(zz) == 2)
                        {
                            if (rnd.NextFloat() >= 0.5)
                                blocks[index] = new Voxel(VoxelDefinition.Leaves.Type, 0);
                        }
                        else
                        {
                            blocks[index] = new Voxel(VoxelDefinition.Leaves.Type, 0);
                        }
                    }

            // leaves top layer
            int bottomY = treeY + baseHeight + 2;
            for (int y = treeY + baseHeight + 2; y < treeY + height; y++)
                for (int zz = -1; zz <= 1; zz++)
                    for (int xx = -1; xx <= 1; xx++)
                    {
                        int x = xx + treeX, z = zz + treeZ;
                        index = (y * Length + z) * Width + x;

                        if (xx == 0 || zz == 0)
                        {
                            blocks[index] = new Voxel(VoxelDefinition.Leaves.Type, 0);
                        }
                        else if (y == bottomY && rnd.NextFloat() >= 0.5)
                        {
                            blocks[index] = new Voxel(VoxelDefinition.Leaves.Type, 0);
                        }
                    }

            // then place trunk
            index = (treeY * Length + treeZ) * Width + treeX;
            for (int y = 0; y < height - 1; y++)
            {
                blocks[index] = new Voxel(VoxelDefinition.Log.Type, 0);
                index += oneY;
            }
        }
    }
}
