using System.Globalization;
using System.Linq;
using SharpDX;

namespace AppleCinnamon.ChunkBuilder.WorldGenerator
{
    public class SplineSegment
    {
        public const float CANVAS_SIZE = 400f;
        public const int SEGMENTS_COUNT = 1000;

        public static readonly SplineSegment Continental = Parse(
            "-3.68934;314.98676|40.914734;316.23727|24.633839;39.72454|39.732132;37.39889|56.449993;32.867252|58.553623;27.554434|70.19506;39.76968|97.71285;30.41274|60.067844;91.7177|95.5997;127.91405|176.781;100.83703|163.33446;181.95784|203.55981;195.17604|246.3645;218.28584|209.50485;297.2165|237.50516;330.47858|268.3902;355.63095|272.34082;380.1881|311.20694;378.0029|328.05408;398.56567|346.11432;399.69797|363.46448;396.6673|379.00165;390.37802|395.10825;390.0682|414.86246;381.54547|456.2758;438.13144|446.66238;369.75516|476.45618;462.22937");

        public static readonly SplineSegment Erosion = Parse(
            "-3;383.95834|117.17391;381.25|46.4124;331.50195|131.90639;310.4714|168.56944;275.24643|124.86482;268.30243|177.96088;241.67432|242.13953;240.46538|172.62468;125.77357|217.30142;132.17213|276.29526;129.70065|261.05392;90.50274|267.2144;41.221733|279.50067;13.738113|296.9925;13.132828|305.24326;49.908413|333.7312;106.10275|324.95953;149.61182|346.06204;153.02005|357.93698;154.4038|363.95325;153.75339|371.59552;152.45258|380.5183;147.95935|386.37195;95.76422|395.64026;83.0813|403.93292;68.93495|391.73782;68.44715|421.0061;45.032516|431.73782;45.52032|434.17685;45.52032|447.83536;46.983734");

        public static readonly SplineSegment PeaksAndRivers = Parse(
            "-3.25;137|68.75;238.25|81.75;249.75|163.75;151.25|196.25;112.5|176.25;8.75|203.75;8.75|232.73438;6.6458435|201.48438;111.64584|223.98438;110.39584|224.92188;167.89583|231.17188;179.14583|249.92188;197.89583|268.67188;239.14584|279.92188;256.64584|291.17188;286.64584|308.67188;320.39584|324.92188;356.64584|338.67188;397.89584|383.67188;225.39583|366.17188;255.39583|397.42188;184.14583");

        public SplineSegment Prev;
        public SplineSegment Next;

        public readonly Vector2 P1;
        public readonly Vector2 C1;
        public readonly Vector2 C2;
        public readonly Vector2 P2;

        public readonly Vector2[] Segments;

        public SplineSegment(Vector2 p1, Vector2 c1, Vector2 c2, Vector2 p2)
        {
            P1 = p1;
            C1 = c1;
            C2 = c2;
            P2 = p2;

            Segments = new Vector2[SEGMENTS_COUNT];
            for (var i = 0; i < SEGMENTS_COUNT; i++)
            {
                Segments[i] = Lerp(i / 1000f);
            }
        }

        public SplineSegment LinkTo(Vector2 c1, Vector2 c2, Vector2 p2)
        {
            var next = new SplineSegment(P2, c1, c2, p2)
            {
                Prev = this
            };
            Next = next;
            return next;
        }

        private Vector2 Lerp(float t)
        {
            var a = Vector2.Lerp(P1, C1, t);
            var b = Vector2.Lerp(C1, C2, t);
            var c = Vector2.Lerp(C2, P2, t);
            var d = Vector2.Lerp(a, b, t);
            var e = Vector2.Lerp(b, c, t);
            return Vector2.Lerp(d, e, t);
        }

        public float GetValue(float input)
        {
            var a = ((input + 1f) * (CANVAS_SIZE / 2f));

            var current = this;
            while (current != null)
            {
                if (current.P1.X <= a && current.P2.X >= a)
                {
                    var result = current.BinarySearchIterative(a);
                    if (result == null) return -1;
                    return (result.Value.Y / CANVAS_SIZE) * 2f - 1f;
                }

                current = current.Next;
            }

            return -1;
        }

        private Vector2? BinarySearchIterative(float value)
        {
            int min = 0;
            int max = Segments.Length - 1;
            while (min <= max)
            {
                int mid = (min + max) / 2;
                if (Segments[mid].X <= value && (mid + 1 == Segments.Length || Segments[mid + 1].X >= value))
                {
                    return Segments[mid];

                }

                if (value < Segments[mid].X)
                {
                    max = mid - 1;
                }
                else
                {
                    min = mid + 1;
                }
            }
            return null;
        }

        public static SplineSegment Parse(string input)
        {
            var vectors = input.Split("|").Select(s =>
            {
                var coords = s.Split(";");
                return new Vector2(
                    float.Parse(coords[0], CultureInfo.InvariantCulture),
                    float.Parse(coords[1], CultureInfo.InvariantCulture));
            }).ToList();

            var splineSegment = new SplineSegment(vectors[0], vectors[1], vectors[2], vectors[3]);
            var last = splineSegment;

            for (var i = 4; i < vectors.Count; i += 3)
            {
                last = last.LinkTo(vectors[i], vectors[i + 1], vectors[i + 2]);
            }

            return splineSegment;
        }
    }
}
