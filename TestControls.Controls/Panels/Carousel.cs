using System;
using System.Collections.Generic;
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

        private List<Rect[]> childrenLocationsList;
        private List<PlaneProjection[]> childrenProjectionsList;
        private List<double[]> childrenOpacityList;

        private static Duration rorateToItemDuration = new Duration(new System.TimeSpan(0, 0, 0, 0, 150));
        private static Duration rorateDuration = new Duration(new System.TimeSpan(0, 0, 0, 0, 500));

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

            childrenLocationsList = new List<Rect[]>();
            childrenProjectionsList = new List<PlaneProjection[]>();
            childrenOpacityList = new List<double[]>();
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

            availableSize.Height = EllipseHeight + _maxHeight;
            availableSize.Width = EllipseWidth + _maxWidth;

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

            childrenLocationsList.Clear();
            childrenProjectionsList.Clear();
            childrenOpacityList.Clear();

            var children = Children.Select(x => x as FrameworkElement).ToList();

            if (SelectedItem == null)
            {
                SelectedItem = children[children.Count - 1].DataContext;
            }
            else
            {
                var currentIndex = children.IndexOf(children.FirstOrDefault(x => (x?.DataContext.Equals(SelectedItem)).Value));
                SelectedItem = children.ElementAt(currentIndex == 0 ? children.Count - 1 : currentIndex - 1).DataContext;
            }

            Animate(rorateDuration, 1, false);
        }


        public void RotateRight()
        {
            if (!Children.Any()) return;

            childrenLocationsList.Clear();
            childrenProjectionsList.Clear();
            childrenOpacityList.Clear();

            var children = Children.Select(x => x as FrameworkElement).ToList();

            if (SelectedItem == null)
            {
                SelectedItem = children[children.Count < 2 ? 0 : 1].DataContext;
            }
            else
            {
                var currentIndex = children.IndexOf(children.FirstOrDefault(x => (x?.DataContext.Equals(SelectedItem)).Value));
                SelectedItem = children.ElementAt(currentIndex == children.Count - 1 ? 0 : currentIndex + 1).DataContext;
            }

            Animate(rorateDuration, 1, true);
        }

        public void RotateToItem(object item)
        {
            if (!Children.Any() || (SelectedItem != null && SelectedItem.Equals(item))) return;

            var children = Children.Select(x => x as FrameworkElement).ToList();

            var index = children.IndexOf(children.FirstOrDefault(x => (x?.DataContext.Equals(item)).Value));
            var currentIndex = SelectedItem == null ? 0 : children.IndexOf(children.FirstOrDefault(x => (x?.DataContext.Equals(SelectedItem)).Value));
            SelectedItem = item;

            childrenLocationsList.Clear();
            childrenProjectionsList.Clear();
            childrenOpacityList.Clear();

            var dif = currentIndex - index;

            if (Abs(dif) > 0) Animate(rorateToItemDuration, Abs(dif), dif < 0);
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

        private void Animate(Duration duration, int count, bool rotateRight)
        {
            var translate = new TranslateTransform();

            _storyboards = new Storyboard[count];

            DoubleAnimation opacityAnimation;
            DoubleAnimation projectionAnimation;
            DoubleAnimation xAnimation;
            DoubleAnimation yAnimation;

            for (int j = 0; j < count; ++j)
            {
                if (rotateRight)
                    RightOffset();
                else
                    LeftOffset();

                _storyboards[j] = new Storyboard();

                for (int i = 0; i < Children.Count; ++i)
                {
                    opacityAnimation = new DoubleAnimation() { From = prevChildrenOpacity[i], To = childrenOpacity[i], Duration = duration };
                    Storyboard.SetTarget(opacityAnimation, Children[i]);
                    Storyboard.SetTargetProperty(opacityAnimation, nameof(UIElement.Opacity));

                    projectionAnimation = new DoubleAnimation() { From = prevChildrenProjections[i].LocalOffsetZ, To = childrenProjections[i].LocalOffsetZ, Duration = duration };
                    Storyboard.SetTarget(projectionAnimation, Children[i]);
                    Storyboard.SetTargetProperty(projectionAnimation, new PropertyPath("UIElement.Projection.LocalOffsetZ").Path);

                    Children[i].RenderTransform = translate;

                    xAnimation = new DoubleAnimation() { EnableDependentAnimation = true, From = 0, To = childrenLocations[i].X - prevChildrenLocations[i].X, Duration = duration };
                    Storyboard.SetTarget(xAnimation, Children[i]);
                    Storyboard.SetTargetProperty(xAnimation, "(UIElement.RenderTransform).(TranslateTransform.X)");

                    yAnimation = new DoubleAnimation() { EnableDependentAnimation = true, From = 0, To = childrenLocations[i].Y - prevChildrenLocations[i].Y, Duration = duration };
                    Storyboard.SetTarget(yAnimation, Children[i]);
                    Storyboard.SetTargetProperty(yAnimation, "(UIElement.RenderTransform).(TranslateTransform.Y)");

                    Storyboard.SetTarget(yAnimation, Children[i]);

                    _storyboards[j].FillBehavior = FillBehavior.Stop;

                    _storyboards[j].Children.Add(opacityAnimation);
                    _storyboards[j].Children.Add(projectionAnimation);
                    _storyboards[j].Children.Add(xAnimation);
                    _storyboards[j].Children.Add(yAnimation);
                }
            }

            int t = 0;
            for (int i = 0; i < count; ++i)
            {
                _storyboards[i].Completed += (s, e) =>
                 {
                     try
                     {
                         for (int k = 0; k < Children.Count; ++k)
                         {
                             Canvas.SetZIndex(Children[k], (int)childrenProjectionsList[t][k].LocalOffsetZ);
                             Children[k].Arrange(childrenLocationsList[t][k]);
                             Children[k].Opacity = childrenOpacityList[t][k];
                             Children[k].Projection = childrenProjectionsList[t][k];
                         }

                         if (t < count - 1)
                             _storyboards[++t].Begin();
                     }
                     catch { }
                 };
            }

            _storyboards[0].Begin();

        }

        public void RightOffset()
        {
            if (!Children.Any()) return;

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

            childrenProjectionsList.Add(childrenProjections.ToArray());
            childrenLocationsList.Add(childrenLocations.ToArray());
            childrenOpacityList.Add(childrenOpacity.ToArray());
        }


        public void LeftOffset()
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

            childrenProjectionsList.Add(childrenProjections.ToArray());
            childrenLocationsList.Add(childrenLocations.ToArray());
            childrenOpacityList.Add(childrenOpacity.ToArray());
        }

        #endregion
    }
}
