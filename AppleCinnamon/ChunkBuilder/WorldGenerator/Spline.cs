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
            "-14.998864;385.8201|67.75997;385.70157|63.749485;154.8606|91.963425;8.376783|107.44489;5.188682|123.94478;7.231304|126.21547;4.2390695|133.42714;10.514781|116.61546;89.931984|145.8616;87.81881|195.23338;89.527504|167.33446;210.95784|203.55981;195.17604|252.3645;216.28584|213.50485;321.2165|237.50516;330.47858|303.3902;336.63095|272.34082;380.1881|311.20694;378.0029|328.05408;398.56567|346.11432;399.69797|363.46448;396.6673|379.00165;390.37802|395.10825;390.0682|414.86246;381.54547|456.2758;438.13144|446.66238;369.75516|476.45618;462.22937");

        public static readonly SplineSegment Erosion = Parse(
            "-3;383.95834|117.17391;381.25|39.4124;307.50195|131.90639;310.4714|149.15196;264.5668|115.86482;239.30241|177.96088;241.67432|242.13953;240.46538|172.62468;125.77357|217.30142;132.17213|276.29526;129.70065|261.05392;90.50274|272.06876;34.425617|279.50067;13.738113|296.9925;13.132828|321.7481;27.578316|333.7312;106.10275|324.95953;149.61182|346.06204;153.02005|357.93698;154.4038|363.95325;153.75339|371.59552;152.45258|380.5183;147.95935|386.37195;95.76422|395.64026;83.0813|403.93292;68.93495|391.73782;68.44715|421.0061;45.032516|431.73782;45.52032|434.17685;45.52032|447.83536;46.983734");

        public static readonly SplineSegment PeaksAndRivers = Parse(
            "3.75;113.75|41.25;120|92.5;118.75|158.75;115|196.25;112.5|175;35|202.5;31.25|227.73438;31.645844|201.48438;111.64584|223.98438;110.39584|224.92188;167.89583|231.17188;179.14583|252.42188;175.39583|247.42188;276.64584|326.17188;245.39583|314.92188;305.39584|326.17188;347.89584|336.17188;360.39584|349.92188;344.14584|408.67188;229.14583|344.92188;225.39583|397.42188;184.14583");

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
