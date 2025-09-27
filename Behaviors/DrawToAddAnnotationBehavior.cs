using Dicom.Imaging.Mathematics;
using DicomAnnotationsDrawing.Models;
using Microsoft.Xaml.Behaviors;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DicomAnnotationsDrawing.Behaviors
{
    public class DrawToAddAnnotationBehavior : Behavior<FrameworkElement>
    {
        public static readonly DependencyProperty ItemsProperty =
            DependencyProperty.Register(
                nameof(Items),
                typeof(IList),
                typeof(DrawToAddAnnotationBehavior),
                new PropertyMetadata(null));

        public IList? Items
        {
            get => (IList?)GetValue(ItemsProperty);
            set => SetValue(ItemsProperty, value);
        }

        public static readonly DependencyProperty SelectedProperty =
            DependencyProperty.Register(
                nameof(Selected),
                typeof(Annotation),
                typeof(DrawToAddAnnotationBehavior),
                new PropertyMetadata(null));

        public Annotation? Selected
        {
            get => (Annotation?)GetValue(SelectedProperty);
            set => SetValue(SelectedProperty, value);
        }

        public static readonly DependencyProperty OverlayCanvasProperty =
            DependencyProperty.Register(
                nameof(OverlayCanvas),
                typeof(Canvas),
                typeof(DrawToAddAnnotationBehavior),
                new PropertyMetadata(null));

        public Canvas? OverlayCanvas
        {
            get => (Canvas?)GetValue(OverlayCanvasProperty);
            set => SetValue(OverlayCanvasProperty, value);
        }

        public static readonly DependencyProperty MinSizeProperty =
            DependencyProperty.Register(
                nameof(MinSize),
                typeof(double),
                typeof(DrawToAddAnnotationBehavior),
                new PropertyMetadata(4.0));

        public double MinSize
        {
            get => (double)GetValue(MinSizeProperty);
            set => SetValue(MinSizeProperty, value);
        }

        public static readonly DependencyProperty AddAnnotationCommandProperty =
    DependencyProperty.Register(nameof(AddAnnotationCommand), typeof(ICommand), typeof(DrawToAddAnnotationBehavior), new PropertyMetadata(null));
        public ICommand? AddAnnotationCommand
        {
            get => (ICommand?)GetValue(AddAnnotationCommandProperty);
            set => SetValue(AddAnnotationCommandProperty, value);
        }

        private Point? _start;
        private Rectangle? _rubber;

        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.PreviewMouseLeftButtonDown += OnHostMouseDown;
            AssociatedObject.PreviewMouseMove += OnHostMouseMove;
            AssociatedObject.PreviewMouseLeftButtonUp += OnHostMouseUp;
            AssociatedObject.MouseLeave += OnHostMouseLeave;

            if (OverlayCanvas != null)
            {
                OverlayCanvas.IsHitTestVisible = false;
            }
        }

        protected override void OnDetaching()
        {
            AssociatedObject.PreviewMouseLeftButtonDown -= OnHostMouseDown;
            AssociatedObject.PreviewMouseMove -= OnHostMouseMove;
            AssociatedObject.PreviewMouseLeftButtonUp -= OnHostMouseUp;
            AssociatedObject.MouseLeave -= OnHostMouseLeave;
            base.OnDetaching();
        }

        private void OnHostMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (OverlayCanvas == null || Items == null)
            {
                return;
            }

            if (ClickedAnnotation(e.OriginalSource as DependencyObject))
            {
                return;
            }

            _start = e.GetPosition(OverlayCanvas);

            // Create rubber-band rectangle on the overlay (display only).
            _rubber = new Rectangle
            {
                Stroke = Brushes.DeepSkyBlue,
                StrokeThickness = 1.0,
                StrokeDashArray = new DoubleCollection { 3, 2 },
                Fill = new SolidColorBrush(Color.FromArgb(32, 0, 191, 255))
            };
            Canvas.SetLeft(_rubber, _start.Value.X);
            Canvas.SetTop(_rubber, _start.Value.Y);
            _rubber.Width = 0;
            _rubber.Height = 0;
            OverlayCanvas.Children.Add(_rubber);

            AssociatedObject.CaptureMouse();
            e.Handled = true;
        }

        private void OnHostMouseMove(object? sender, MouseEventArgs e)
        {
            if (OverlayCanvas == null || _start == null || _rubber == null)
            {
                return;
            }

            var pos = e.GetPosition(OverlayCanvas);
            var x = Math.Min(pos.X, _start.Value.X);
            var y = Math.Min(pos.Y, _start.Value.Y);
            var w = Math.Abs(pos.X - _start.Value.X);
            var h = Math.Abs(pos.Y - _start.Value.Y);

            Canvas.SetLeft(_rubber, x);
            Canvas.SetTop(_rubber, y);
            _rubber.Width = w;
            _rubber.Height = h;
        }

        private void OnHostMouseUp(object? sender, MouseButtonEventArgs e)
        {
            if (OverlayCanvas == null || Items == null || _start == null || _rubber == null)
            {
                Cleanup();
                return;
            }

            var end = e.GetPosition(OverlayCanvas);
            var x = Math.Min(end.X, _start.Value.X);
            var y = Math.Min(end.Y, _start.Value.Y);
            var w = Math.Abs(end.X - _start.Value.X);
            var h = Math.Abs(end.Y - _start.Value.Y);

            if (w >= MinSize && h >= MinSize)
            {
                var rect = new Rect(x, y, w, h);
                if (AddAnnotationCommand != null && AddAnnotationCommand.CanExecute(rect))
                {
                    AddAnnotationCommand.Execute(rect);
                }
            }

            Cleanup();
            AssociatedObject.ReleaseMouseCapture();
            e.Handled = true;
        }

        private void OnHostMouseLeave(object? sender, MouseEventArgs e)
        {
            if (AssociatedObject.IsMouseCaptured)
            {
                AssociatedObject.ReleaseMouseCapture();
            }
            Cleanup();
        }

        private void Cleanup()
        {
            if (OverlayCanvas != null && _rubber != null)
            {
                OverlayCanvas.Children.Remove(_rubber);
            }

            _rubber = null;
            _start = null;
        }

        private static bool ClickedAnnotation(DependencyObject? d)
        {
            while (d != null)
            {
                if (d is Controls.DraggableResizableAnnotation)
                {
                    return true;
                }
                d = VisualTreeHelper.GetParent(d);
            }
            return false;
        }
    }
}
