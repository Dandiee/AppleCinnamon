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
    }
}
