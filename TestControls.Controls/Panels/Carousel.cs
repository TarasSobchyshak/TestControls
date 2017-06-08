using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Shapes;
using static System.Math;

namespace TestControls.Controls.Panels
{

    public class Carousel : Panel
    {
        #region Fields

        private Storyboard _storyboard;
        private Storyboard[] _storyboards;
        private double _maxWidth;
        private double _maxHeight;

        private double _ellipseMajorAxis;
        private double _ellipseMinorAxis;

        private Rect[] childrenLocations;
        private PlaneProjection[] childrenProjections;
        private double[] childrenOpacity;
        private Rect[] prevChildrenLocations;
        private PlaneProjection[] prevChildrenProjections;
        private double[] prevChildrenOpacity;

        private static Duration rorateToItemDuration = new Duration(new System.TimeSpan(0, 0, 0, 0, 200));
        private static Duration rorateDuration = new Duration(new System.TimeSpan(0, 0, 0, 0, 400));
        #endregion

        #region Properties

        public static readonly DependencyProperty EllipseHeightProperty = DependencyProperty
            .Register(nameof(EllipseHeight), typeof(double), typeof(Carousel), new PropertyMetadata(0.5, EllipseHeightPropertyCallBack));

        public static readonly DependencyProperty EllipseWidthProperty = DependencyProperty
            .Register(nameof(EllipseWidth), typeof(double), typeof(Carousel), new PropertyMetadata(0.5, EllipseWidhtPropertyCallBack));

        public static readonly DependencyProperty OffsetProperty = DependencyProperty
            .Register(nameof(Offset), typeof(int), typeof(Carousel), new PropertyMetadata(0.5, OffsetPropertyCallBack));

        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty
           .Register(nameof(SelectedItem), typeof(object), typeof(Carousel), new PropertyMetadata(null, SelectedItemPropertyChanged));

        public double EllipseHeight
        {
            get { return (double)GetValue(EllipseHeightProperty); }
            set { SetValue(EllipseHeightProperty, value); }
        }

        public double EllipseWidth
        {
            get { return (double)GetValue(EllipseWidthProperty); }
            set { SetValue(EllipseWidthProperty, value); }
        }

        public int Offset
        {
            get { return (int)GetValue(OffsetProperty); }
            set { SetValue(OffsetProperty, value); }
        }

        public object SelectedItem
        {
            get { return GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        private static void EllipseHeightPropertyCallBack(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        }
        private static void EllipseWidhtPropertyCallBack(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        }
        private static void OffsetPropertyCallBack(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        }
        private static void SelectedItemPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        }

        #endregion

        #region Constructors

        public Carousel()
        {
            _storyboard = new Storyboard();
        }

        #endregion

        #region Overridden

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (!Children.Any()) return new Size();

            var n = childrenLocations.Length;
            for (int i = 0; i < n; ++i)
            {
                Children[i].Opacity = childrenOpacity[i];
                Children[i].Projection = childrenProjections[i];
                Children[i].Arrange(childrenLocations[i]);
                Canvas.SetZIndex(Children[i], (int)childrenProjections[i].LocalOffsetZ);
            }

            return finalSize;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (!Children.Any()) return new Size();

            foreach (UIElement item in Children)
            {
                item.Measure(availableSize);

                if (_maxHeight < item.DesiredSize.Height)
                    _maxHeight = item.DesiredSize.Height;

                if (_maxWidth < item.DesiredSize.Width)
                    _maxWidth = item.DesiredSize.Width;
            }

            availableSize.Height = EllipseHeight;
            availableSize.Width = EllipseWidth;

            CalculateEllipseLocations(availableSize);
            CalculateChildrenProjections();
            CalculateChildrenOpacity();


            return availableSize;
        }

        #endregion

        #region Rotation

        public void RotateLeft()
        {
            if (!Children.Any()) return;

            prevChildrenProjections = childrenProjections.ToArray();
            prevChildrenLocations = childrenLocations.ToArray();
            prevChildrenOpacity = childrenOpacity.ToArray();

            var x = childrenLocations[childrenLocations.Length - 1];
            var y = childrenProjections[childrenProjections.Length - 1];
            var z = childrenOpacity[childrenOpacity.Length - 1];

            for (int i = childrenOpacity.Length - 1; i > 0; --i)
            {
                childrenLocations[i] = childrenLocations[i - 1];
                childrenProjections[i] = childrenProjections[i - 1];
                childrenOpacity[i] = childrenOpacity[i - 1];
            }

            childrenLocations[0] = x;
            childrenProjections[0] = y;
            childrenOpacity[0] = z;

            //ArrangeOverride(DesiredSize);
            Animate(rorateDuration, 10);
        }


        public void RotateRight()
        {
            if (!Children.Any()) return;

            prevChildrenProjections = childrenProjections.ToArray();
            prevChildrenLocations = childrenLocations.ToArray();
            prevChildrenOpacity = childrenOpacity.ToArray();

            var x = childrenLocations[0];
            var y = childrenProjections[0];
            var z = childrenOpacity[0];

            for (int i = 0; i < childrenProjections.Length - 1; ++i)
            {
                childrenLocations[i] = childrenLocations[i + 1];
                childrenProjections[i] = childrenProjections[i + 1];
                childrenOpacity[i] = childrenOpacity[i + 1];
            }

            childrenLocations[childrenLocations.Length - 1] = x;
            childrenProjections[childrenProjections.Length - 1] = y;
            childrenOpacity[childrenOpacity.Length - 1] = z;

            //ArrangeOverride(DesiredSize);
            Animate(rorateDuration, 10);
        }

        public void RotateToItem(object item)
        {
            if (!Children.Any() || (SelectedItem != null && SelectedItem.Equals(item))) return;
            SelectedItem = item;

            var children = Children.Select(x => x as FrameworkElement).ToList();

            var index = children.IndexOf(children.FirstOrDefault(x => (x?.DataContext.Equals(item)).Value));

            prevChildrenProjections = childrenProjections.ToArray();
            prevChildrenLocations = childrenLocations.ToArray();
            prevChildrenOpacity = childrenOpacity.ToArray();

            //OffsetChildrenLocations(index);
            //OffsetChildrenProjections(index);
            //OffsetChildrenOpacity(index);

            //ArrangeOverride(DesiredSize);
            Animate(rorateToItemDuration, 15);
        }

        #endregion

        #region Private methods 

        private void OffsetChildrenLocations(int offset)
        {
            if (!Children.Any()) return;

            CalculateEllipseLocations(DesiredSize);

            var n = childrenLocations.Length;

            if (offset < 0 || offset > n) return;

            var copy = new Rect[n];

            for (int i = offset; i < n; ++i)
                copy[i] = childrenLocations[i - offset];
            for (int i = 0; i < offset; ++i)
                copy[i] = childrenLocations[n - offset + i];

            childrenLocations = copy;
        }

        private void OffsetChildrenProjections(int offset)
        {
            if (!Children.Any()) return;

            CalculateChildrenProjections();

            var n = childrenProjections.Length;

            if (offset < 0 || offset > n) return;

            var copy = new PlaneProjection[n];

            for (int i = offset; i < n; ++i)
                copy[i] = childrenProjections[i - offset];
            for (int i = 0; i < offset; ++i)
                copy[i] = childrenProjections[n - offset + i];

            childrenProjections = copy;
        }

        private void OffsetChildrenOpacity(int offset)
        {
            if (!Children.Any()) return;

            CalculateChildrenOpacity();

            var n = childrenOpacity.Length;

            if (offset < 0 || offset > n) return;

            var copy = new double[n];

            for (int i = offset; i < n; ++i)
                copy[i] = childrenOpacity[i - offset];
            for (int i = 0; i < offset; ++i)
                copy[i] = childrenOpacity[n - offset + i];

            childrenOpacity = copy;
        }

        private void CalculateEllipseLocations(Size availableSize)
        {
            var count = Children.Count;
            var itemSize = new Size(_maxWidth, _maxHeight);

            _ellipseMajorAxis = EllipseWidth / 2;
            _ellipseMinorAxis = EllipseHeight / 2;

            var top = new Point();
            var pivot = new Point(_ellipseMajorAxis, _ellipseMinorAxis);
            childrenLocations = new Rect[count];

            var step = (2 * PI) / count;
            var angle = PI / 2;

            for (int i = 0; i < count; ++i)
            {
                top.X = pivot.X + _ellipseMajorAxis * Cos(angle);
                top.Y = pivot.Y + _ellipseMinorAxis * Sin(angle);

                childrenLocations[i] = new Rect(top, itemSize);
                angle += step;
            }
        }

        private void CalculateChildrenProjections()
        {
            var globalOffsetZ = 0;
            var n = childrenLocations.Length;
            childrenProjections = new PlaneProjection[n];

            for (int i = 0; i < n; ++i)
            {
                var planeProjection = new PlaneProjection();
                if (i != 0)
                {
                    planeProjection.LocalOffsetZ = (n & 1) == 0
                        ? i <= n / 2 ? globalOffsetZ -= Offset : globalOffsetZ += Offset
                        : i == n / 2 + 1 ? globalOffsetZ : i <= n / 2 ? globalOffsetZ -= Offset : globalOffsetZ += Offset;
                }
                else
                {
                    planeProjection.LocalOffsetZ = 0;
                }

                childrenProjections[i] = planeProjection;
            }
        }

        private void CalculateChildrenOpacity()
        {
            var opacity = 1.0;
            var n = childrenLocations.Length;
            var offset = 0.9 / (n / 2);
            childrenOpacity = new double[n];

            for (int i = 0; i < n; ++i)
            {
                double childOpacity;
                if (i != 0)
                {
                    childOpacity = (n & 1) == 0
                        ? i <= n / 2 ? opacity -= offset : opacity += offset
                        : i == n / 2 + 1 ? opacity : i <= n / 2 ? opacity -= offset : opacity += offset;
                }
                else
                {
                    childOpacity = 1.0;
                }

                childrenOpacity[i] = childOpacity;
            }
        }

        private async void Animate(Duration duration, int count)
        {
            if (prevChildrenOpacity == null || prevChildrenProjections == null || prevChildrenLocations == null) return;

            _storyboard.Stop();
            _storyboard.Children.Clear();
            count = 5;
            _storyboards = new Storyboard[count];
            var translate = new TranslateTransform();

            for (int j = 0; j < count; ++j)
            {
                _storyboards[j] = new Storyboard();
                //_storyboards[j].BeginTime = new TimeSpan(0, 0, 0, 0, duration.TimeSpan.Milliseconds * j);

                prevChildrenProjections = childrenProjections.ToArray();
                prevChildrenLocations = childrenLocations.ToArray();
                prevChildrenOpacity = childrenOpacity.ToArray();

                var x0 = childrenLocations[childrenLocations.Length - 1];
                var y0 = childrenProjections[childrenProjections.Length - 1];
                var z0 = childrenOpacity[childrenOpacity.Length - 1];

                for (int q = childrenOpacity.Length - 1; q > 0; --q)
                {
                    childrenLocations[q] = childrenLocations[q - 1];
                    childrenProjections[q] = childrenProjections[q - 1];
                    childrenOpacity[q] = childrenOpacity[q - 1];
                }

                childrenLocations[0] = x0;
                childrenProjections[0] = y0;
                childrenOpacity[0] = z0;

                for (int i = 0; i < Children.Count; ++i)
                {
                    var opacityAnimation0 = new DoubleAnimation() { From = prevChildrenOpacity[i], To = childrenOpacity[i], Duration = duration };
                    Storyboard.SetTarget(opacityAnimation0, Children[i]);
                    Storyboard.SetTargetProperty(opacityAnimation0, nameof(UIElement.Opacity));

                    var projectionAnimation0 = new DoubleAnimation() { From = prevChildrenProjections[i].LocalOffsetZ, To = childrenProjections[i].LocalOffsetZ, Duration = duration };
                    Storyboard.SetTarget(projectionAnimation0, Children[i]);
                    Storyboard.SetTargetProperty(projectionAnimation0, new PropertyPath("UIElement.Projection.LocalOffsetZ").Path);

                    Children[i].RenderTransform = translate;

                    var xAnimation0 = new DoubleAnimation() { From = 0, To = childrenLocations[i].X - prevChildrenLocations[i].X, Duration = duration };
                    Storyboard.SetTarget(xAnimation0, Children[i]);
                    Storyboard.SetTargetProperty(xAnimation0, "(UIElement.RenderTransform).(TranslateTransform.X)");

                    var yAnimation0 = new DoubleAnimation() { From = 0, To = childrenLocations[i].Y - prevChildrenLocations[i].Y, Duration = duration };
                    Storyboard.SetTarget(yAnimation0, Children[i]);
                    Storyboard.SetTargetProperty(yAnimation0, "(UIElement.RenderTransform).(TranslateTransform.Y)");

                    Canvas.SetZIndex(Children[i], (int)childrenProjections[i].LocalOffsetZ);

                    _storyboards[j].Children.Add(opacityAnimation0);
                    _storyboards[j].Children.Add(projectionAnimation0);
                    _storyboards[j].Children.Add(xAnimation0);
                    _storyboards[j].Children.Add(yAnimation0);
                    _storyboards[j].Completed += (s, e) =>
                    {
                        //var story = s as Storyboard;
                        //if (story != null)
                        //{
                        //    story.Stop();
                        //    story.Children.Clear();
                        //    story = null;
                        //}
                        ArrangeOverride(DesiredSize);
                    };
                }
            }
            for (int j = 0; j < count; ++j)
            {
                _storyboards[j].Begin();
                await Task.Delay(duration.TimeSpan.Milliseconds);
            }
            //_storyboard.Completed += (s, e) =>
            //{
            //    _storyboard.Stop();
            //    //ArrangeOverride(DesiredSize);
            //};
            //_storyboard.Begin();
        }

        #endregion
    }
}
