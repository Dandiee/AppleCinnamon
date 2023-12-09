using System.Diagnostics.Metrics;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using SharpDX;
using Clipboard = System.Windows.Clipboard;
using Color = SharpDX.Color;
using Panel = System.Windows.Controls.Panel;
using Point = System.Windows.Point;

namespace NoiseGeneratorTest
{
    public partial class SplineEditor
    {
        private CB _first;
        private CB _last;
        private double _zoom = 0.8;
        private Point _pan;
        private bool _isPanning;
        private Point _panStartPosition;

        public event EventHandler<object> OnUpdated;

        public void FireEvent()
        {
            OnUpdated?.Invoke(this, null);
        }

        public SplineEditor()
        {
            InitializeComponent();
            
            var canvas = new BezierCanvas(MyCanvas, this);
            _first = new CB(canvas, new Point(100, 10), new Point(120, 20), new Point(150, 30), new Point(200, 50));
            _last = _first.LinkTo(new(250, 60), new(260, 70), new(300, 100));

            canvas.Cb = _first;

            SetPath();

            MyCanvas.MouseRightButtonDown += (_, e) =>
            {
                var target = e.GetPosition(MyCanvas);
                var cDelta = new Vector(20, 20);
                _last = _last.LinkTo(target + cDelta, target - cDelta, target);

                SetPath();
            };

            MyCanvas.MouseLeftButtonDown += (_, e) =>
            {
                _isPanning = true;
                _panStartPosition = e.GetPosition(MyGrid);
            };

            MyCanvas.MouseLeave += (_, _) =>
            {
                _isPanning = false;
            };

            MyCanvas.MouseMove += (_, e) =>
            {
                if (_isPanning)
                {
                    var current = e.GetPosition(MyGrid);
                    var delta = current - _panStartPosition;

                    _pan = _pan + delta;

                    MyCanvas.RenderTransform = new TransformGroup
                    {
                        Children = new TransformCollection(new Transform[]
                        {
                            new ScaleTransform(_zoom, _zoom),
                            new TranslateTransform(_pan.X, _pan.Y)
                        })
                    };

                    _panStartPosition = current;
                }
            };

            MyCanvas.MouseLeftButtonUp += (_, e) =>
            {
                _isPanning = false;
            };

            MyCanvas.MouseWheel += (_, e) =>
            {
                _zoom += (e.Delta / (120f * 100));

                MyCanvas.RenderTransform = new TransformGroup
                {
                    Children = new TransformCollection(new Transform[]
                    {
                        new ScaleTransform(_zoom, _zoom),
                        new TranslateTransform(_pan.X, _pan.Y)
                    })
                };
            };
        }

        private void SetPath()
        {
            var path = new Path
            {
                Stroke = new SolidColorBrush(Colors.Black),
                StrokeThickness = 1,
                IsHitTestVisible = false,
                Data = new PathGeometry(new[]
                {
                    _first.GetPathFigure()
                })
            };

            MyCanvas.Children.Add(path);
            Panel.SetZIndex(path, 100);
        }

        public float GetValue(float input, double canvasHeight)
        {
            var a = (float)((input + 1f) * canvasHeight / 2f);
            foreach (var cb in _first.GetCbs())
            {
                var x1 = cb.V1.X;
                var x2 = cb.V4.X;

                if (x1 <= a && x2 >= a)
                {
                    var result = BinarySearchIterative(ref cb.Values, a);
                    if (result == null) return -1;
                    return (float)(((result.Value.Y / canvasHeight) * 2) - 1f);
                }
            }

            return -1;
        }

        public static Vector2? BinarySearchIterative(ref Vector2[] inputArray, float value)
        {
            var tries = 0;
            int min = 0;
            int max = inputArray.Length - 1;
            while (min <= max)
            {
                tries++;
                int mid = (min + max) / 2;
                if (inputArray[mid].X <= value && (mid + 1 == inputArray.Length || inputArray[mid + 1].X >= value))
                {
                    return inputArray[mid];

                }
                else if (value < inputArray[mid].X)
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

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            var str = Clipboard.GetText();
            if (string.IsNullOrEmpty(str)) return;

            try
            {
                var vectors = str.Split("|").Select(s =>
                {
                    var coords = s.Split(";");
                    return new Point(float.Parse(coords[0], CultureInfo.InvariantCulture),
                        float.Parse(coords[1], CultureInfo.InvariantCulture));
                }).ToList();

                var elementsToRemove = MyCanvas.Children.OfType<FrameworkElement>()
                    .Where(s => string.IsNullOrEmpty(s.Name)).ToList();

                foreach (var elementToRemove in elementsToRemove)
                {
                    MyCanvas.Children.Remove(elementToRemove);
                }

                var canvas = new BezierCanvas(MyCanvas, this);
                _first = new CB(canvas, vectors[0], vectors[1], vectors[2], vectors[3]);
                _last = _first;
                for (var i = 4; i < vectors.Count; i += 3)
                {
                    _last = _last.LinkTo(vectors[i], vectors[i + 1], vectors[i + 2]);
                }
                canvas.Cb = _first;
                SetPath();

            }
            catch {}
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            var str = _first._path.StartPoint.ToVector2().ToInvariantString();
            foreach (var cb in _first.GetCbs())
            {
                str += $"|{cb._segment.Point1.ToVector2().ToInvariantString()}";
                str += $"|{cb._segment.Point2.ToVector2().ToInvariantString()}";
                str += $"|{cb._segment.Point3.ToVector2().ToInvariantString()}";
            }

            Clipboard.SetText(str);
        }
    }

    public class BezierCanvas
    {
        public readonly Canvas Canvas;
        private readonly SplineEditor _editor;

        private bool _isDragging;
        private Point _startDragPosition;
        private Ellipse _draggedEllipse;

        public CB Cb { get; set; }

        public BezierCanvas(Canvas canvas, SplineEditor editor)
        {
            Canvas = canvas;
            _editor = editor;

            Canvas.PreviewMouseMove += (_, e) =>
            {
                if (_isDragging && _draggedEllipse != null)
                {
                    var currentPosition = e.GetPosition(Canvas);
                    var delta = currentPosition - _startDragPosition;

                    var left = Canvas.GetLeft(_draggedEllipse);
                    var top = Canvas.GetTop(_draggedEllipse);

                    Canvas.SetLeft(_draggedEllipse, left + delta.X);
                    Canvas.SetTop(_draggedEllipse, top + delta.Y);

                    foreach (var cb in Cb.GetCbs())
                    {
                        cb.UpdatePosition();
                    }

                    _editor.FireEvent();

                    _startDragPosition = currentPosition;

                    e.Handled = true;
                }
            };
        }

        public void Add(Ellipse ellipse, Point origo)
        {
            Canvas.Children.Add(ellipse);
            Canvas.SetLeft(ellipse, origo.X - ellipse.Width / 2f);
            Canvas.SetTop(ellipse, origo.Y - ellipse.Width / 2f);
            Panel.SetZIndex(ellipse, 10);


            ellipse.MouseDown += (_, e) =>
            {
                _startDragPosition = e.GetPosition(Canvas);
                _isDragging = true;
                _draggedEllipse = ellipse;

            };

            ellipse.MouseUp += (_, _) =>
            {
                _isDragging = false;
            };
        }

        public void Add(Line line, Point from, Point to)
        {
            Canvas.Children.Add(line);
            Panel.SetZIndex(line, 5);
            line.X1 = from.X;
            line.Y1 = from.Y;
            line.X2 = to.X;
            line.Y2 = to.Y;
        }
    }

    public class CB
    {
        private static readonly SolidColorBrush CColor = new(Colors.GreenYellow);
        private static readonly SolidColorBrush LColor = new(Colors.Orange);
        private static readonly SolidColorBrush PColor = new(Colors.Red);
        public const double ERadius = 10;

        private BezierCanvas _canvas;
        public BezierSegment _segment;
        public readonly PathFigure _path;

        public CB Prev { get; set; }
        public CB Next { get; set; }

        public Ellipse P1 { get; set; } = new() { Fill = PColor, Width = ERadius, Height = ERadius };
        public Ellipse C1 { get; set; } = new() { Fill = CColor, Width = ERadius, Height = ERadius };
        public Ellipse C2 { get; set; } = new() { Fill = CColor, Width = ERadius, Height = ERadius };
        public Ellipse P2 { get; set; } = new() { Fill = PColor, Width = ERadius, Height = ERadius };

        public Line L1 { get; set; } = new() { Stroke = LColor, StrokeThickness = 1, IsHitTestVisible = false };
        public Line L2 { get; set; } = new() { Stroke = LColor, StrokeThickness = 1, IsHitTestVisible = false };

        public CB(BezierCanvas canvas, Point p1, Point c1, Point c2, Point p2)
        {
            _canvas = canvas;
            _canvas.Add(P1, p1);
            _path = new PathFigure
            {
                StartPoint = p1
            };

            _canvas.Add(L1, p1, p2);

            Add(null, c1, c2, p2);
        }

        private CB(CB prev, Point c1, Point c2, Point p2)
        {
            Add(prev, c1, c2, p2);
        }

        private void Add(CB prev, Point c1, Point c2, Point p2)
        {
            if (prev != null)
            {
                prev.Next = this;
                Prev = prev;
                _canvas = prev._canvas;
                _canvas.Add(L1, prev._segment.Point3, c1);
            }


            _segment = new BezierSegment(c1, c2, p2, true);
            _canvas.Add(C1, c1);
            _canvas.Add(C2, c2);
            _canvas.Add(P2, p2);

            _canvas.Add(L2, c2, p2);

            UpdatePosition();
        }

        public CB LinkTo(Point c1, Point c2, Point p2) => new(this, c1, c2, p2);

        public Vector2[] Values = new Vector2[1000];

        public Vector2 V1;
        public Vector2 V2;
        public Vector2 V3;
        public Vector2 V4;

        public void UpdatePosition()
        {
            if (Prev == null)
            {
                _path.StartPoint = P1.GetPosition();

                L1.X1 = _path.StartPoint.X;
                L1.Y1 = _path.StartPoint.Y;
                V1 = _path.StartPoint.ToVector2();
            }
            else
            {
                L1.X1 = Prev._segment.Point3.X;
                L1.Y1 = Prev._segment.Point3.Y;
                V1 = Prev._segment.Point3.ToVector2();
            }

            _segment.Point1 = C1.GetPosition();
            _segment.Point2 = C2.GetPosition();
            _segment.Point3 = P2.GetPosition();

            V2 = _segment.Point1.ToVector2();
            V3 = _segment.Point2.ToVector2();
            V4 = _segment.Point3.ToVector2();

            L1.X2 = _segment.Point1.X;
            L1.Y2 = _segment.Point1.Y;

            L2.X1 = _segment.Point2.X;
            L2.Y1 = _segment.Point2.Y;

            L2.X2 = _segment.Point3.X;
            L2.Y2 = _segment.Point3.Y;

            for (var i = 0; i < 1000; i++)
            {
                Values[i] = HyperLerp(i / 1000f);
            }
        }

        private Vector2 HyperLerp(float t)
        {
            var p0 = (Prev?._segment.Point3 ?? _path.StartPoint).ToVector2();
            var p1 = _segment.Point1.ToVector2();
            var p2 = _segment.Point2.ToVector2();
            var p3 = _segment.Point3.ToVector2();

            var a = Vector2.Lerp(p0, p1, t);
            var b = Vector2.Lerp(p1, p2, t);
            var c = Vector2.Lerp(p2, p3, t);
            var d = Vector2.Lerp(a, b, t);
            var e = Vector2.Lerp(b, c, t);
            return Vector2.Lerp(d, e, t);
        }

        public IEnumerable<BezierSegment> GetSegments()
        {
            var current = this;
            while (current != null)
            {
                yield return current._segment;
                current = current.Next;
            }
        }

        public IEnumerable<CB> GetCbs()
        {
            var current = this;
            while (current != null)
            {
                yield return current;
                current = current.Next;
            }
        }

        public PathFigure GetPathFigure()
        {
            _path.Segments = new PathSegmentCollection(GetSegments());
            return _path;
        }
    }

    public static class EllipseExtensions
    {
        public static Point GetPosition(this Ellipse ellipse)
        {
            var x = Canvas.GetLeft(ellipse) + ellipse.Width / 2f;
            var y = Canvas.GetTop(ellipse) + ellipse.Height / 2f;

            return new Point(x, y);
        }

        public static Vector2 ToVector2(this Point p) => new Vector2((float)p.X, (float)p.Y);

        public static string ToInvariantString(this Vector2 p) => p.X.ToString(CultureInfo.InvariantCulture) + ";" +
                                                                  p.Y.ToString(CultureInfo.InvariantCulture);
    }

    public class Vector2Comparer : IComparer<Vector2>
    {
        public int Compare(Vector2 x, Vector2 y)
        {
            var xComparison = x.X.CompareTo(y.X);
            if (xComparison != 0) return xComparison;

            return x.IsZero.CompareTo(y.IsZero);
        }
    }
}
