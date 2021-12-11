using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AppleCinnamon.Helper;
using AppleCinnamon.Settings;
using SharpDX;

namespace AppleCinnamon.Pipeline
{
    public sealed class LocalSunlightInitializer
    {
        public DataflowContext<Chunk> Process(DataflowContext<Chunk> context)
        {
            var sw = Stopwatch.StartNew();
            var chunk = context.Payload;

            for (var i = 0; i != Chunk.SizeXy; i++)
            {
                for (var k = 0; k != Chunk.SizeXy; k++)
                {
                    _ = LightingService.Sunlight(chunk, new Int3(i, chunk.CurrentHeight, k), 15).ToList();
                }
            }

            sw.Stop();
            return new DataflowContext<Chunk>(context, context.Payload, sw.ElapsedMilliseconds, nameof(LocalSunlightInitializer));
        }
    }
}
