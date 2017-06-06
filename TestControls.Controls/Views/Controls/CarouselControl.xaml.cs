using TestControls.Controls.Panels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace TestControls.Controls.Views.Controls
{
    public sealed partial class CarouselControl : UserControl
    {
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty
       .Register(nameof(ItemsSource), typeof(object), typeof(CarouselControl), new PropertyMetadata(null, ItemsSourcePropertyChanged));

        private static void ItemsSourcePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        }

        public object ItemsSource
        {
            get { return GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public CarouselControl()
        {
            this.InitializeComponent();
        }

        private void LeftButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            (ItemsControl.ItemsPanelRoot as Carousel)?.RotateRight();
        }

        private void RightButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            (ItemsControl.ItemsPanelRoot as Carousel)?.RotateLeft();
        }

        private void Carousel_Tapped(object sender, TappedRoutedEventArgs e)
        {
            (ItemsControl.ItemsPanelRoot as Carousel)?.
                RotateToItem((e.OriginalSource as FrameworkElement)?.DataContext);
        }
    }
}
