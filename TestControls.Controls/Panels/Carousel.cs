using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using static System.Math;

namespace TestControls.Controls.Panels
{

    public class Carousel : Panel
    {
        #region Fields

        private double _maxWidth;
        private double _maxHeight;
        private Rect[] childrenLocations;
        private PlaneProjection[] childrenProjections;
        private double[] childrenOpacity;

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

        #region Overridden

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (!Children.Any()) return new Size();

            var n = childrenLocations.Length;
            for (int i = 0; i < n; ++i)
            {
                Children[i].Arrange(childrenLocations[i]);
                Children[i].Projection = childrenProjections[i];
                Children[i].Opacity = childrenOpacity[i];
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

            CalculateEllipseLocations(availableSize);
            CalculateChildrenProjections();
            CalculateChildrenOpacity();

            availableSize.Height = EllipseHeight + _maxHeight;

            return availableSize;
        }

        #endregion

        #region Rotation

        public void RotateLeft()
        {
            if (!Children.Any()) return;

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

            ArrangeOverride(DesiredSize);
        }

        public void RotateRight()
        {
            if (!Children.Any()) return;

            var x = childrenLocations[0];
            var y = childrenProjections[0];
            var z = childrenOpacity[0];

            for (int i = 0; i < childrenProjections.Length - 1; ++i)
            {
                childrenLocations[i] = childrenLocations[i + 1];
                childrenProjections[i] = childrenProjections[i + 1];
                childrenOpacity[i] = childrenOpacity[i + 1];
            }

            childrenLocations[childrenProjections.Length - 1] = x;
            childrenProjections[childrenProjections.Length - 1] = y;
            childrenOpacity[childrenOpacity.Length - 1] = z;

            ArrangeOverride(DesiredSize);
        }

        public void RotateToItem(object item)
        {
            if (!Children.Any() || (SelectedItem != null && SelectedItem.Equals(item))) return;
            SelectedItem = item;

            var children = Children.Select(x => x as FrameworkElement).ToList();

            var index = children.IndexOf(children.FirstOrDefault(x => (x?.DataContext.Equals(item)).Value));

            OffsetChildrenLocations(index);
            OffsetChildrenProjections(index);
            OffsetChildrenOpacity(index);

            ArrangeOverride(DesiredSize);
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
            childrenLocations = new Rect[count];

            var top = new Point();
            var pivot = new Point(EllipseWidth, EllipseHeight);

            var step = (2 * PI) / count;
            var angle = PI / 2;

            for (int i = 0; i < count; ++i)
            {
                top.X = pivot.X + EllipseWidth * Cos(angle);
                top.Y = pivot.Y + EllipseHeight * Sin(angle);

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

        #endregion
    }
}
