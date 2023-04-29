
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NoiseGeneratorTest
{
    public partial class MainWindow
    {
        public MainWindowViewModel ViewModel { get; }

        public MainWindow()
        {
            InitializeComponent();

            ViewModel = new MainWindowViewModel(this);
            DataContext = ViewModel;

            var pf = new PathFigure();
            var cb = new CubicBezier(MyCanvas, 
                new Point(100, 100), 
                new Point(150, 50), 
                new Point(150, 150), 
                new Point(200, 100),
                pf);
            cb.LinkTo(new Point(250, 50), new Point(250, 150), new Point(300, 100));

            pf.StartPoint = cb.Start.Point;
            pf.Segments = new PathSegmentCollection(cb.GetSegments());

            var pfc = new PathFigureCollection(new[] { pf });
            var pg = new PathGeometry(pfc);

            MyPath.Data = pg;
        }

    }

    public class CubicBezier
    {
        public Canvas Canvas { get; set; }

        public ControlPoint Cp1 { get; set; }
        public ControlPoint Cp2 { get; set; }

        public EndPoint End { get; set; }
        public EndPoint Start { get; set; }

        public CubicBezier Prev { get; set; }
        public CubicBezier Next { get; set; }

        public BezierSegment Segment { get; set; }
        public PathFigure PathFigure { get; set; }

        public Line Cl1 { get; set; }
        public Line Cl2 { get; set; }


        public CubicBezier(Canvas canvas, Point start, Point cp1, Point cp2, Point end, PathFigure pathFigure = null)
        {
            PathFigure = pathFigure;
            Canvas = canvas;

            Cl1 = new Line
            {
                Stroke = new SolidColorBrush(Colors.Orange),
                StrokeThickness = 1,
                //StrokeDashCap = PenLineCap.Flat
            };

            Cl2 = new Line
            {
                Stroke = new SolidColorBrush(Colors.Orange),
                StrokeThickness = 1,
                //StrokeDashCap = PenLineCap.Flat
            };

            Canvas.Children.Add(Cl1);
            Canvas.Children.Add(Cl2);


            Start = new EndPoint(start, Canvas, this);
            Cp1 = new ControlPoint(cp1, Canvas, this);
            End = new EndPoint(end, Canvas, this);
            Cp2 = new ControlPoint(cp2, Canvas, this);

            
        }

        public CubicBezier LinkTo(Point cp1, Point cp2, Point end)
        {
            var segment = new CubicBezier(Canvas, End.Point, cp1, cp2, end);
            segment.Prev = this;
            Next = segment;
            return segment;
        }

        public IEnumerable<PathSegment> GetSegments()
        {
            var s = this;
            while (s != null)
            {
                //var q = new BezierSegment();
                //var binding = new Binding(nameof(Cp1.Point))
                //{
                //    Source = s
                //};
                //q.Set
                var segment = new BezierSegment(s.Cp1.Point, s.Cp2.Point, s.End.Point, true);
                s.Segment = segment;
                s.Update();
                s = s.Next;

                yield return segment;
            }
        }

        public void Update()
        {
            if (Segment == null) return;
            Segment.Point1 = Cp1.Point;
            Segment.Point2 = Cp2.Point;
            Segment.Point3 = End.Point;

            if (Prev != null)
            {
                Cl1.X1 = Prev.End.Point.X;
                Cl1.Y1 = Prev.End.Point.Y;
            }
            else
            {
                Cl1.X1 = Start.Point.X;
                Cl1.Y1 = Start.Point.Y;

                PathFigure.StartPoint = Start.Point;
            }
            
            Cl1.X2 = Cp1.Point.X;
            Cl1.Y2 = Cp1.Point.Y;

            Cl2.X1 = End.Point.X;
            Cl2.Y1 = End.Point.Y;
            Cl2.X2 = Cp2.Point.X;
            Cl2.Y2 = Cp2.Point.Y;

            if (Next != null)
            {
                Next.Update();
            }

        }
    }

    public class BezierPoint
    {
        public readonly CubicBezier Owner;
        public Canvas Canvas { get; set; }
        public Point Point { get; set; }
        public Ellipse Elipse { get; set; }
        public SolidColorBrush Brush { get; set; }
        public double Radius { get; set; }

        private bool _isDragging;
        private Point _startDragPosition;

        public BezierPoint(Color color, Point point, Canvas canvas, CubicBezier owner)
        {
            Owner = owner;
            Canvas = canvas;
            Point = point;
            Brush = new SolidColorBrush(color);
            Radius = 10;

            Elipse = new Ellipse
            {
                Fill = Brush,
                Width = Radius,
                Height = Radius
            };

            Elipse.MouseDown += (_, e) =>
            {
                _startDragPosition = e.GetPosition(Canvas);
                _isDragging = true;
            };

            Elipse.MouseUp += (_, _) =>
            {
                _isDragging = false;
            };

            Canvas.MouseMove    += (_, e) =>
            {
                if (_isDragging)
                {
                    var currentPosition = e.GetPosition(Canvas);
                    var delta = currentPosition - _startDragPosition;

                    var left = Canvas.GetLeft(Elipse);
                    var top = Canvas.GetTop(Elipse);

                    Canvas.SetLeft(Elipse, left + delta.X);
                    Canvas.SetTop(Elipse, top + delta.Y);

                    Point = Point + delta;

                    Owner.Update();

                    _startDragPosition = currentPosition;
                }
            };

            Canvas.SetLeft(Elipse, point.X - Radius / 2f);
            Canvas.SetTop(Elipse, point.Y - Radius / 2f);

            Canvas.Children.Add(Elipse);
        }
    }

    public class ControlPoint : BezierPoint
    {
        public ControlPoint(Point point, Canvas canvas, CubicBezier owner)
         : base(Colors.Red, point, canvas, owner)
        {

        }
    }

    public class EndPoint : BezierPoint
    {
        public EndPoint(Point point, Canvas canvas, CubicBezier owner)
            : base(Colors.Blue, point, canvas, owner)
        {
        }
    }
}
