using DicomAnnotationsDrawing.Models;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace DicomAnnotationsDrawing.Controls
{
    public partial class DraggableResizableAnnotation : UserControl
    {
        public DraggableResizableAnnotation()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            PreviewMouseLeftButtonDown += (s, e) =>
            {
                if (Ann != null)
                {
                    Ann.IsSelected = true;
                    SelectCommand?.Execute(Ann);
                }
            };
        }

        public static readonly DependencyProperty XProperty =
            DependencyProperty.Register(nameof(X), typeof(double), typeof(DraggableResizableAnnotation),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPosSizeChanged));

        public static readonly DependencyProperty YProperty =
            DependencyProperty.Register(nameof(Y), typeof(double), typeof(DraggableResizableAnnotation),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPosSizeChanged));

        public static readonly DependencyProperty BoxWidthProperty =
            DependencyProperty.Register(nameof(BoxWidth), typeof(double), typeof(DraggableResizableAnnotation),
                new FrameworkPropertyMetadata(50.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPosSizeChanged));

        public static readonly DependencyProperty BoxHeightProperty =
            DependencyProperty.Register(nameof(BoxHeight), typeof(double), typeof(DraggableResizableAnnotation),
                new FrameworkPropertyMetadata(50.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPosSizeChanged));

        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(DraggableResizableAnnotation),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsSelectedChanged));

        public static readonly DependencyProperty SelectCommandProperty =
            DependencyProperty.Register(nameof(SelectCommand), typeof(ICommand), typeof(DraggableResizableAnnotation),
                new PropertyMetadata(null));

        public Annotation? Ann => DataContext as Annotation;
        public double X { get => (double)GetValue(XProperty); set => SetValue(XProperty, value); }
        public double Y { get => (double)GetValue(YProperty); set => SetValue(YProperty, value); }
        public double BoxWidth { get => (double)GetValue(BoxWidthProperty); set => SetValue(BoxWidthProperty, value); }
        public double BoxHeight { get => (double)GetValue(BoxHeightProperty); set => SetValue(BoxHeightProperty, value); }
        public bool IsSelected { get => (bool)GetValue(IsSelectedProperty); set => SetValue(IsSelectedProperty, value); }
        public ICommand? SelectCommand { get => (ICommand?)GetValue(SelectCommandProperty); set => SetValue(SelectCommandProperty, value); }

        private Canvas? _parentCanvas;
        private Rect _startRect;
        private Point _moveMouseStart;
        private const double MinSize = 5.0;

        private double CanvasW => _parentCanvas?.ActualWidth is double w && !double.IsNaN(w) ? w : double.PositiveInfinity;
        private double CanvasH => _parentCanvas?.ActualHeight is double h && !double.IsNaN(h) ? h : double.PositiveInfinity;

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            _parentCanvas = FindParentCanvas(this);
            SetThumbEventHandlers();
            ApplyModelToView();
        }

        private void SetThumbEventHandlers()
        {
            SetMoveThumbEventHandlers();

            // Corners
            SetResizeThumbEventHandlers(TopLeftThumb, -1, -1);
            SetResizeThumbEventHandlers(TopRightThumb, +1, -1);
            SetResizeThumbEventHandlers(BottomLeftThumb, -1, +1);
            SetResizeThumbEventHandlers(BottomRightThumb, +1, +1);

            // Edges
            SetResizeThumbEventHandlers(LeftThumb, -1, 0);
            SetResizeThumbEventHandlers(RightThumb, +1, 0);
            SetResizeThumbEventHandlers(TopThumb, 0, -1);
            SetResizeThumbEventHandlers(BottomThumb, 0, +1);
        }

        private void SetMoveThumbEventHandlers()
        {
            MoveThumb.DragStarted += (s, _) =>
            {
                CaptureStartRect();
                _moveMouseStart = MousePosInCanvasClamped();
            };

            MoveThumb.DragDelta += (s, _) =>
            {
                var p = MousePosInCanvasClamped();

                double dx = p.X - _moveMouseStart.X;
                double dy = p.Y - _moveMouseStart.Y;

                double nx = Clamp(_startRect.Left + dx, 0, CanvasW - _startRect.Width);
                double ny = Clamp(_startRect.Top + dy, 0, CanvasH - _startRect.Height);

                X = nx;
                Y = ny;
            };
        }

        private void SetResizeThumbEventHandlers(Thumb t, int dx, int dy)
        {
            t.DragStarted += (s, _) =>
            {
                CaptureStartRect();
            };

            t.DragDelta += (s, e) =>
            {
                var p = MousePosInCanvasClamped();

                double left = _startRect.Left;
                double right = _startRect.Right;
                double top = _startRect.Top;
                double bottom = _startRect.Bottom;

                if (dx < 0)
                {
                    left = Math.Min(p.X, right - MinSize);
                }
                else if (dx > 0)
                {
                    right = Math.Max(p.X, left + MinSize);
                    right = Math.Min(right, CanvasW);
                }

                if (dy < 0)
                {
                    top = Math.Min(p.Y, bottom - MinSize);
                }
                else if (dy > 0)
                {
                    bottom = Math.Max(p.Y, top + MinSize);
                    bottom = Math.Min(bottom, CanvasH);
                }

                double x = Clamp(left, 0, CanvasW - MinSize);
                double y = Clamp(top, 0, CanvasH - MinSize);
                double w = ClampSize(right - x, MinSize, CanvasW - x);
                double h = ClampSize(bottom - y, MinSize, CanvasH - y);

                X = x;
                Y = y;
                BoxWidth = Math.Max(MinSize, w);
                BoxHeight = Math.Max(MinSize, h);
            };
        }

        private void ApplyModelToView()
        {
            Canvas.SetLeft(this, X);
            Canvas.SetTop(this, Y);
            Width = Math.Max(MinSize, BoxWidth);
            Height = Math.Max(MinSize, BoxHeight);
        }

        private void CaptureStartRect()
        {
            double left = Canvas.GetLeft(this);
            double top = Canvas.GetTop(this);
            if (double.IsNaN(left)) left = X;
            if (double.IsNaN(top)) top = Y;

            double w = ActualWidth > 0 ? ActualWidth : Width;
            double h = ActualHeight > 0 ? ActualHeight : Height;
            if (w <= 0) w = BoxWidth;
            if (h <= 0) h = BoxHeight;

            _startRect = new Rect(left, top, Math.Max(MinSize, w), Math.Max(MinSize, h));
        }

        private Point MousePosInCanvasClamped()
        {
            if (_parentCanvas == null) return new Point(0, 0);
            var p = Mouse.GetPosition(_parentCanvas);
            double x = Clamp(p.X, 0, CanvasW);
            double y = Clamp(p.Y, 0, CanvasH);
            return new Point(x, y);
        }

        private static void OnPosSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (DraggableResizableAnnotation)d;
            c.ApplyModelToView();
        }

        private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (DraggableResizableAnnotation)d;
            if ((bool)e.NewValue) c.Focus();
        }

        private static Canvas? FindParentCanvas(FrameworkElement e)
        {
            for (FrameworkElement? p = e; p != null; p = VisualTreeHelper.GetParent(p) as FrameworkElement)
                if (p is Canvas c) return c;
            return null;
        }

        private static double Clamp(double v, double min, double max)
        {
            if (double.IsInfinity(max) || double.IsNaN(max)) return Math.Max(min, v);
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }

        private static double ClampSize(double v, double min, double max)
        {
            if (double.IsInfinity(max) || double.IsNaN(max)) return Math.Max(min, v);
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }
    }
}
