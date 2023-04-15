using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppleCinnamon.Common
{
    public static class Mappings
    {
        public static readonly IReadOnlyCollection<Int2> ChunkManagerDirections = new[]
        {
            new Int2(1, 0),
            new Int2(0, 1),
            new Int2(-1, 0),
            new Int2(0, -1)
        };

        public static readonly IReadOnlyDictionary<Face, Face> OppositeMapping =
            new Dictionary<Face, Face>
            {
                [Face.Top] = Face.Bottom,
                [Face.Bottom] = Face.Top,
                [Face.Left] = Face.Right,
                [Face.Right] = Face.Left,
                [Face.Front] = Face.Back,
                [Face.Back] = Face.Bottom,
            };
    }
}
