using Portalum.TrwPrinter.EasyPrinterS3.Models;
using System.Windows.Controls;
using System.Windows.Media;

namespace Portalum.TrwPrinter.EasyPrinterS3.ControlPanel
{
    /// <summary>
    /// Interaction logic for PrinterStateUserControl.xaml
    /// </summary>
    public partial class PrinterStateUserControl : UserControl
    {
        public PrinterStateUserControl()
        {
            this.InitializeComponent();
        }

        public void ResetStates()
        {
            this.RectanglePrintingInProgress.Fill = Brushes.Gray;
            this.RectangleCardHasBeenPrinted.Fill = Brushes.Gray;
            this.RectangleCardInPrintPosition.Fill = Brushes.Gray;
            this.RectangleCardSensorFrontFeeder.Fill = Brushes.Gray;
            this.RectangleError.Fill = Brushes.Gray;
        }

        public void UpdateState(PrinterState printerState)
        {
            this.RectanglePrintingInProgress.Dispatcher.Invoke(() =>
            {
                if (printerState.PrintingInProgress)
                {
                    this.RectanglePrintingInProgress.Fill = Brushes.Red;
                    return;
                }

                this.RectanglePrintingInProgress.Fill = Brushes.Green;
            });

            this.RectangleCardHasBeenPrinted.Dispatcher.Invoke(() =>
            {
                if (printerState.CardHasBeenPrinted)
                {
                    this.RectangleCardHasBeenPrinted.Fill = Brushes.Green;
                    return;
                }

                this.RectangleCardHasBeenPrinted.Fill = Brushes.Red;
            });

            this.RectangleCardInPrintPosition.Dispatcher.Invoke(() =>
            {
                if (printerState.CardInPrintPosition)
                {
                    this.RectangleCardInPrintPosition.Fill = Brushes.Green;
                    return;
                }

                this.RectangleCardInPrintPosition.Fill = Brushes.Red;
            });

            this.RectangleCardSensorFrontFeeder.Dispatcher.Invoke(() =>
            {
                if (printerState.CardSensorFront)
                {
                    this.RectangleCardSensorFrontFeeder.Fill = Brushes.Green;
                    return;
                }

                this.RectangleCardSensorFrontFeeder.Fill = Brushes.Red;
            });

            this.RectangleError.Dispatcher.Invoke(() =>
            {
                if (printerState.Error)
                {
                    this.RectangleError.Fill = Brushes.Red;
                    return;
                }

                this.RectangleError.Fill = Brushes.LightGray;
            });
        }
    }
}
