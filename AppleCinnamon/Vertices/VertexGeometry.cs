using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace AppleCinnamon.Vertices
{
    [StructLayout(LayoutKind.Sequential)]
    public struct VertexGeometry
    {
        public const int Size = 12;

        // actual results
        // faces: 13793568	13 793 568
        // voxel:  7031615	 7 031 615
        // avg: 1.96 face/voxel

        // 87654321|87654321|87654321|87654321
        // XXXXXXXX|XXXXXXXX|XXXXXXXX|XXXXXXXX
        public Vector3 Position;

        // 87654321|87654321|87654321|87654321
        // ZZZZZZZZ|ZZZZZZZZ|ZZZZZZZZ|ZZZZZZZZ
        // public int PositionZ;

        // 87654321|87654321|87654321|87654321
        // 66665555|44443333|22221111|IIIIIIII => SL / Sides
        // public uint IndexAndLight;
        // 
        // // 87654321|87654321|87654321|87654321
        // // 22222222|11111111|00VVVVVV|YYYYYYYY => AO / Sides
        // public uint PositionYVisibilityAmbient;
        // 
        // // 87654321|87654321|87654321|87654321
        // // 66666666|55555555|44444444|33333333 => AO / Sides
        // public uint Ambient;


        public static readonly InputElement[] InputElements =
       {
            new InputElement("POSITION", 0, Format.R32G32B32_Float, 00, 0),
            //new InputElement("RANDOM", 0, Format.R32_UInt, 08, 0),
            //new InputElement("RANDOMKA", 0, Format.R32_UInt, 12, 0),
            //new InputElement("RANDOMOCSKA", 0, Format.R32_UInt, 16, 0),
        };


        public VertexGeometry(byte block, float i, byte j, float k, byte[] lights, byte[][] ambients, byte visibility)
        {
            Position = new Vector3(i, j, k);
            
            //IndexAndLight = block;
            //IndexAndLight |= (uint)lights[0] << 8;  // sizeof(block)
            //IndexAndLight |= (uint)lights[1] << 12; // sizeof(block)  + sizeof(light) * 1
            //IndexAndLight |= (uint)lights[2] << 16; // sizeof(block)  + sizeof(light) * 2
            //IndexAndLight |= (uint)lights[3] << 20; // sizeof(block)  + sizeof(light) * 3
            //IndexAndLight |= (uint)lights[4] << 24; // sizeof(block)  + sizeof(light) * 4
            //IndexAndLight |= (uint)lights[5] << 28; // sizeof(block)  + sizeof(light) * 5

            //// 87654321|87654321|87654321|87654321
            //// 22222222|11111111|00VVVVVV|YYYYYYYY => AO / Sides
            //PositionYVisibilityAmbient = j;
            //PositionYVisibilityAmbient |= (uint)visibility << 8; // sizeof(height)

            //PositionYVisibilityAmbient |= (uint)ambients[0][0] << 16; // sizeof(height) + sizeof(visibility) + sizeof(empty)
            //PositionYVisibilityAmbient |= (uint)ambients[0][1] << 18; // sizeof(height) + sizeof(visibility) + sizeof(empty) + sizeof(ambient) * n
            //PositionYVisibilityAmbient |= (uint)ambients[0][2] << 20; // sizeof(height) + sizeof(visibility) + sizeof(empty) + sizeof(ambient) * n
            //PositionYVisibilityAmbient |= (uint)ambients[0][3] << 22; // sizeof(height) + sizeof(visibility) + sizeof(empty) + sizeof(ambient) * n

            //PositionYVisibilityAmbient |= (uint)ambients[1][0] << 24; // sizeof(height) + sizeof(visibility) + sizeof(empty)
            //PositionYVisibilityAmbient |= (uint)ambients[1][1] << 26; // sizeof(height) + sizeof(visibility) + sizeof(empty) + sizeof(ambient) * n
            //PositionYVisibilityAmbient |= (uint)ambients[1][2] << 28; // sizeof(height) + sizeof(visibility) + sizeof(empty) + sizeof(ambient) * n
            //PositionYVisibilityAmbient |= (uint)ambients[1][3] << 30; // sizeof(height) + sizeof(visibility) + sizeof(empty) + sizeof(ambient) * n


            //Ambient = 0;
            //Ambient |= (uint)ambients[2][0] << 0;
            //Ambient |= (uint)ambients[2][1] << 2;
            //Ambient |= (uint)ambients[2][2] << 4;
            //Ambient |= (uint)ambients[2][3] << 6;

            //Ambient |= (uint)ambients[3][0] << 8;
            //Ambient |= (uint)ambients[3][1] << 10;
            //Ambient |= (uint)ambients[3][2] << 12;
            //Ambient |= (uint)ambients[3][3] << 14;

            //Ambient |= (uint)ambients[4][0] << 16;
            //Ambient |= (uint)ambients[4][1] << 18;
            //Ambient |= (uint)ambients[4][2] << 20;
            //Ambient |= (uint)ambients[4][3] << 22;

            //Ambient |= (uint)ambients[5][0] << 24;
            //Ambient |= (uint)ambients[5][1] << 26;
            //Ambient |= (uint)ambients[5][2] << 28;
            //Ambient |= (uint)ambients[5][3] << 30;

        }

        // Summary
        // texture information
        // sunlight information: 4bit per side = 24bit = 3byte => 4bit/1byte left
        // per vertex ambient occlusion: 2bit / vertex, 4 vertex / side => 8bit/1byte/side =>  48bit/6byte/voxel =>
        // visibility flags: 1bit / side, 6bit/voxel

        // Total cost:
        // ------------------------------
        // Position:    72bit   9byte -
        // Index:        8bit   1byte -
        // Light:       24bit   3byte -
        // Ambient:     48bit   6byte
        // Visibility:   6bit   1byte -
        // ------------------------------
        // Summa:      158bit   19.75byte (20 byte), 5 uint
        // ------------------------------
        // Avg size/face:       10 byte
        // Ref per face size:   24 byte
        // Opt per face size:    8 byte

        // Grouping
        // -----------------------------
        // PositionX
    }
}
