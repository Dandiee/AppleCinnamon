using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using SharpDX;
using Point = System.Windows.Point;

namespace NoiseGeneratorTest
{
    public partial class MainWindow
    {
        public MainWindowViewModel ViewModel { get; }

        private readonly CB _first;
        private CB _last;

        private double _zoom = 1;
        private Point _pan;
        private bool _isPanning;
        private Point _panStartPosition;
        private Ellipse _testEllipse;

        public MainWindow()
        {
            InitializeComponent();

            ViewModel = new MainWindowViewModel(this);
            DataContext = ViewModel;

            _testEllipse = new Ellipse
            {
                Fill = new SolidColorBrush(Colors.Purple),
                Width = 10,
                Height = 10
            };
            MyCanvas.Children.Add(_testEllipse);

            var canvas = new BezierCanvas(MyCanvas);
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
            MyPath.Data = new PathGeometry(new[]
            {
                _first.GetPathFigure()
            });

            Panel.SetZIndex(MyPath, 100);
        }

        private void RangeBase_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var slider = sender as Slider;
            var a = slider.Value;
            foreach (var cb in _first.GetCbs())
            {
                var x1 = cb.Prev?._segment.Point3.X ?? cb._path.StartPoint.X;
                var x2 = cb._segment.Point3.X;

                if (x1 <= a && x2 >= a)
                {
                    var step = 0.001f;
                    var current = 0f;
                    while (true)
                    {
                        var res = HyperLerp(cb, current);
                        if (res.X >= a)
                        {
                            Canvas.SetLeft(_testEllipse, res.X - 5);
                            Canvas.SetTop(_testEllipse, res.Y - 5);
                            Peter.Text = res.ToString();
                            return;
                        }

                        current += step;
                    }
                }
            }
        }

        private double Lerp(double v0, double v1, double t) => v0 + t * (v1 - v0);

        private Vector2 HyperLerp(CB cb, float t)
        {
            var p0 = (cb.Prev?._segment.Point3 ?? cb._path.StartPoint).ToVector2();
            var p1 = cb._segment.Point1.ToVector2();
            var p2 = cb._segment.Point2.ToVector2();
            var p3 = cb._segment.Point3.ToVector2();

            var a = Vector2.Lerp(p0, p1, t);
            var b = Vector2.Lerp(p1, p2, t);
            var c = Vector2.Lerp(p2, p3, t);
            var d = Vector2.Lerp(a, b, t);
            var e = Vector2.Lerp(b, c, t);
            return Vector2.Lerp(d, e, t);
        }
    }

    public class BezierCanvas
    {
        public readonly Canvas Canvas;

        private bool _isDragging;
        private Point _startDragPosition;
        private Ellipse _draggedEllipse;

        public CB Cb { get; set; }

        public BezierCanvas(Canvas canvas)
        {
            Canvas = canvas;

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

                    _startDragPosition = currentPosition;

                    e.Handled = true;
                }
            };
        }

        public void Add(Ellipse ellipse, Point origo)
        {
            Canvas.Children.Add(ellipse);
            Canvas.SetLeft(ellipse, origo.X + ellipse.Width / 2f);
            Canvas.SetTop(ellipse, origo.Y + ellipse.Width / 2f);
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

        public void UpdatePosition()
        {
            if (Prev == null)
            {
                _path.StartPoint = P1.GetPosition();

                L1.X1 = _path.StartPoint.X;
                L1.Y1 = _path.StartPoint.Y;
            }
            else
            {
                L1.X1 = Prev._segment.Point3.X;
                L1.Y1 = Prev._segment.Point3.Y;
            }

            _segment.Point1 = C1.GetPosition();
            _segment.Point2 = C2.GetPosition();
            _segment.Point3 = P2.GetPosition();

            L1.X2 = _segment.Point1.X;
            L1.Y2 = _segment.Point1.Y;

            L2.X1 = _segment.Point2.X;
            L2.Y1 = _segment.Point2.Y;

            L2.X2 = _segment.Point3.X;
            L2.Y2 = _segment.Point3.Y;


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
    }
}
