using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Portalum.TrwPrinter.EasyPrinterS3.Models;
using Portalum.TrwPrinter.EasyPrinterS3.PrintElements;
using System.Threading.Tasks;
using System.Windows;

namespace Portalum.TrwPrinter.EasyPrinterS3.ControlPanel
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ILoggerFactory _loggerFactory;
        private PrinterClient _printerClient;
        private IDeviceCommunication _deviceCommunication;

        public MainWindow()
        {
            this._loggerFactory = LoggerFactory.Create(builder =>
                builder.AddFile("default.log", LogLevel.Trace, outputTemplate: "{Timestamp:HH:mm:ss.fff} {Level:u3} {SourceContext} {Message:lj}{NewLine}{Exception}").SetMinimumLevel(LogLevel.Debug));

            this.InitializeComponent();

            this.ButtonDisconnect.IsEnabled = false;
            this.ButtonEjectCard.IsEnabled = false;
            this.ButtonFeedCardFromFrontFeeder.IsEnabled = false;
            this.ButtonFeedCardFromCardHopper.IsEnabled = false;
            this.GroupBoxPrint.IsEnabled = false;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.CleanupPrinterClientAsync().GetAwaiter().GetResult();
        }

        private void PreparePrinterClient(string ipAddress)
        {
            var loggerDeviceCommunication = this._loggerFactory.CreateLogger<TcpNetworkDeviceCommunication>();

            this._deviceCommunication = new TcpNetworkDeviceCommunication(ipAddress, 50020, logger: loggerDeviceCommunication);
            this._printerClient = new PrinterClient(this._deviceCommunication);
            this._printerClient.PrinterStateChanged += this.PrinterStateChanged;
        }

        private async Task CleanupPrinterClientAsync()
        {
            if (this._deviceCommunication != null)
            {
                if (this._deviceCommunication.IsConnected)
                {
                    await this._deviceCommunication.DisconnectAsync();
                }
                this._deviceCommunication.Dispose();
            }

            if (this._printerClient != null)
            {
                this._printerClient.PrinterStateChanged -= this.PrinterStateChanged;
                this._printerClient.Dispose();
            }
        }

        private void PrinterStateChanged(PrinterState printerState)
        {
            this.PrinterStatusUserControl.UpdateState(printerState);

            this.SwitchUserControlActive(printerState);
        }

        private void SwitchUserControlActive(PrinterState printerState)
        {
            this.ButtonEjectCard.Dispatcher.Invoke(() =>
            {
                this.ButtonEjectCard.IsEnabled = printerState.CardInPrintPosition;
            });

            this.ButtonFeedCardFromFrontFeeder.Dispatcher.Invoke(() =>
            {
                this.ButtonFeedCardFromFrontFeeder.IsEnabled = !printerState.CardInPrintPosition;
            });

            this.ButtonFeedCardFromCardHopper.Dispatcher.Invoke(() =>
            {
                this.ButtonFeedCardFromCardHopper.IsEnabled = !printerState.CardInPrintPosition;
            });

            this.GroupBoxPrint.Dispatcher.Invoke(() =>
            {
                this.GroupBoxPrint.IsEnabled = printerState.CardInPrintPosition;
            });
        }

        private async void ButtonConnect_Click(object sender, RoutedEventArgs e)
        {
            await this.CleanupPrinterClientAsync();

            this.PreparePrinterClient(this.TextBoxIpAddress.Text);

            if (!await this._printerClient.ConnectAsync())
            {
                return;
            }

            this.ButtonConnect.IsEnabled = false;
            this.ButtonDisconnect.IsEnabled = true;

            await this.LoadPrinterInfosAsync();
        }

        private async void ButtonDisconnect_Click(object sender, RoutedEventArgs e)
        {
            await this.CleanupPrinterClientAsync();
            this.PrinterStatusUserControl.ResetStates();

            this.ButtonConnect.IsEnabled = true;
            this.ButtonDisconnect.IsEnabled = false;
        }

        private async void ButtonEjectCard_Click(object sender, RoutedEventArgs e)
        {
            await this._printerClient.EjectCardAsync();
        }

        private async void ButtonFeedCardFromHopper_Click(object sender, RoutedEventArgs e)
        {
            await this._printerClient.FeedCardFromHopperAsync();
        }

        private async void ButtonAbortFeed_Click(object sender, RoutedEventArgs e)
        {
            await this._printerClient.AbortFeedAsync();
        }

        private async Task LoadPrinterInfosAsync()
        {
            var firmware = await this._printerClient.GetFirmwareAsync();
            await Task.Delay(50);
            var serialNumber = await this._printerClient.GetPrinterSerialNumberAsync();

            this.LabelPrinterInfo.Content = $"Firmware:{firmware} | SerialNumber:{serialNumber}";
        }

        private async void ButtonPrintImageDemo_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            var dialogResult = openFileDialog.ShowDialog();
            if (dialogResult.HasValue && !dialogResult.Value)
            {
                return;
            }

            var filePath = openFileDialog.FileName;

            var printDocument = new PrintDocument();
            printDocument.AddElement(new ImagePrintElement(filePath, 200));

            await this._printerClient.SendPrintDemoCardAsync(printDocument);
        }

        private async void ButtonPrintTextDemo_Click(object sender, RoutedEventArgs e)
        {
            var printDocument = new PrintDocument();
            printDocument.AddElement(new TextPrintElement("Position X13", 13, 650));
            printDocument.AddElement(new TextPrintElement("Position X15", 15, 650));
            printDocument.AddElement(new TextPrintElement("Position X17", 17, 650));
            printDocument.AddElement(new TextPrintElement("Position X20", 20, 650));
            printDocument.AddElement(new TextPrintElement("Position X25 (Large)", 25, 650, TextSize.Large));
            printDocument.AddElement(new TextPrintElement("Position X50 (Small)", 50, 950, TextSize.Small));
            printDocument.AddElement(new TextPrintElement("Position X55", 55, 950));
            printDocument.AddElement(new TextPrintElement("Position X65", 65, 650));
            printDocument.AddElement(new TextPrintElement("Position X75", 75, 650));
            printDocument.AddElement(new TextPrintElement("Position X85", 85, 650));

            await this._printerClient.SendPrintDemoCardAsync(printDocument);
        }

        private async void ButtonPrintFullDemo_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            var dialogResult = openFileDialog.ShowDialog();
            if (dialogResult.HasValue && !dialogResult.Value)
            {
                return;
            }

            var filePath = openFileDialog.FileName;

            var printDocument = new PrintDocument();
            printDocument.AddElement(new ImagePrintElement(filePath, 650));
            printDocument.AddElement(new TextPrintElement("Max Mustermann", 35, 650, TextSize.Large));
            printDocument.AddElement(new TextPrintElement("Langestraße 4a", 42, 650, TextSize.Medium));
            printDocument.AddElement(new TextPrintElement("10115 Berlin", 49, 650, TextSize.Medium));

            await this._printerClient.SendPrintDemoCardAsync(printDocument);
        }

        private async void ButtonLoadCardFromFront_Click(object sender, RoutedEventArgs e)
        {
            await this._printerClient.FeedCardFromFrontFeederAsync();
        }

        private async void ButtonEraseCard_Click(object sender, RoutedEventArgs e)
        {
            await this._printerClient.SendEraseAreaAsync();
        }

        private async void ButtonReadUid_Click(object sender, RoutedEventArgs e)
        {
            var rfidInfo = await this._printerClient.ReadCardMifareUidAsync();

            if (rfidInfo.Successful)
            {
                MessageBox.Show(rfidInfo.Uid, "Uid", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            MessageBox.Show($"Cannot read {rfidInfo.ErrorMessage}", "Uid Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
