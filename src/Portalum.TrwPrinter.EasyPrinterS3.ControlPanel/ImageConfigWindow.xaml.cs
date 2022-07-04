using System.Windows;

namespace Portalum.TrwPrinter.EasyPrinterS3.ControlPanel
{
    /// <summary>
    /// Interaction logic for ImagePositionWindow.xaml
    /// </summary>
    public partial class ImageConfigWindow : Window
    {
        public double X1 { get; set; }
        public double X2 { get; set; }
        public double Y1 { get; set; }
        public double Y2 { get; set; }

        public ImageConfigWindow()
        {
            this.InitializeComponent();
        }

        private void ButtonLeftBottom200x20_Click(object sender, RoutedEventArgs e)
        {
            this.X1 = 0;
            this.X2 = 200;
            this.Y1 = 0;
            this.Y2 = 20;

            this.DialogResult = true;
        }

        private void ButtonBottomRight200x20_Click(object sender, RoutedEventArgs e)
        {
            this.X1 = 760;
            this.X2 = 960;
            this.Y1 = 0;
            this.Y2 = 20;

            this.DialogResult = true;
        }

        private void ButtonFullSize_Click(object sender, RoutedEventArgs e)
        {
            this.X1 = 0;
            this.X2 = 960;
            this.Y1 = 0;
            this.Y2 = 70;

            this.DialogResult = true;
        }

        private void ButtonHalfSize_Click(object sender, RoutedEventArgs e)
        {
            this.X1 = 0;
            this.X2 = 450;
            this.Y1 = 0;
            this.Y2 = 35;

            this.DialogResult = true;
        }

        private void ButtonTopRight200_20_Click(object sender, RoutedEventArgs e)
        {
            this.X1 = 760;
            this.X2 = 960;
            this.Y1 = 50;
            this.Y2 = 70;

            this.DialogResult = true;
        }
    }
}
