using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Portalum.TrwPrinter.EasyPrinterS3.Models;
using Portalum.TrwPrinter.EasyPrinterS3.PrintElements;
using System.Threading;
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
            this.ResetControls();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.CleanupPrinterClientAsync().GetAwaiter().GetResult();
        }

        private void ResetControls()
        {
            this.ButtonConnect.IsEnabled = true;
            this.ButtonDisconnect.IsEnabled = false;

            this.LabelInfo.Content = string.Empty;
            this.LabelRfid.Content = string.Empty;

            this.ButtonAbortFeed.IsEnabled = false;
            this.ButtonEjectCard.IsEnabled = false;
            this.ButtonFeedCardFromFrontFeeder.IsEnabled = false;
            this.ButtonFeedCardFromCardHopper.IsEnabled = false;
            this.ButtonReadUid.IsEnabled = false;
            this.GroupBoxPrint.IsEnabled = false;
        }

        private bool IsReady()
        {
            if (this._printerClient == null || !this._deviceCommunication.IsConnected)
            {
                return false;
            }

            return true;
        }

        private void PreparePrinterClient(string ipAddress)
        {
            var loggerDeviceCommunication = this._loggerFactory.CreateLogger<TcpNetworkDeviceCommunication>();
            var loggerPrinterClient = this._loggerFactory.CreateLogger<PrinterClient>();

            this._deviceCommunication = new TcpNetworkDeviceCommunication(ipAddress, 50020, logger: loggerDeviceCommunication);
            this._printerClient = new PrinterClient(this._deviceCommunication, loggerPrinterClient);
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

            this.ButtonReadUid.Dispatcher.Invoke(() =>
            {
                this.ButtonReadUid.IsEnabled = printerState.CardInPrintPosition;
            });
        }

        private async void ButtonConnect_Click(object sender, RoutedEventArgs e)
        {
            this.ButtonConnect.IsEnabled = false;
            this.TextBoxIpAddress.IsEnabled = false;
            this.LabelInfo.Content = "Connecting...";

            await this.CleanupPrinterClientAsync();

            this.PreparePrinterClient(this.TextBoxIpAddress.Text);

            using var cancellationTokenSource = new CancellationTokenSource(16000); //The new firmware needs min. 15 seconds to allow reconnection.
            if (!await this._printerClient.ConnectAsync(cancellationTokenSource.Token))
            {
                this.LabelInfo.Content = "Cannot connect";
                await Task.Delay(500);
                this.LabelInfo.Content = string.Empty;

                this.TextBoxIpAddress.IsEnabled = true;
                this.ButtonConnect.IsEnabled = true;
                return;
            }

            this.LabelInfo.Content = "Connected";
            this.TextBoxIpAddress.IsEnabled = true;
            this.ButtonAbortFeed.IsEnabled = true;
            this.ButtonConnect.IsEnabled = false;
            this.ButtonDisconnect.IsEnabled = true;

            await this.LoadPrinterInfosAsync();
        }

        private async void ButtonDisconnect_Click(object sender, RoutedEventArgs e)
        {
            await this.CleanupPrinterClientAsync();
            this.PrinterStatusUserControl.ResetStates();

            this.ResetControls();
        }

        private async void ButtonEjectCard_Click(object sender, RoutedEventArgs e)
        {
            if (!this.IsReady())
            {
                return;
            }

            await this._printerClient.EjectCardAsync();
        }

        private async void ButtonFeedCardFromHopper_Click(object sender, RoutedEventArgs e)
        {
            if (!this.IsReady())
            {
                return;
            }

            await this._printerClient.FeedCardFromHopperAsync();
        }

        private async void ButtonAbortFeed_Click(object sender, RoutedEventArgs e)
        {
            if (!this.IsReady())
            {
                return;
            }

            await this._printerClient.AbortFeedAsync();
        }

        private async Task LoadPrinterInfosAsync()
        {
            if (!this.IsReady())
            {
                return;
            }

            var firmware = await this._printerClient.GetFirmwareAsync();
            await Task.Delay(50);
            var serialNumber = await this._printerClient.GetPrinterSerialNumberAsync();

            this.LabelInfo.Content = $"Firmware:{firmware} | SerialNumber:{serialNumber}";
        }

        private async void ButtonPrintImageDemo_Click(object sender, RoutedEventArgs e)
        {
            if (!this.IsReady())
            {
                return;
            }

            var openFileDialog = new OpenFileDialog();
            var dialogResult = openFileDialog.ShowDialog();
            if (dialogResult.HasValue && !dialogResult.Value)
            {
                return;
            }

            var filePath = openFileDialog.FileName;

            var imageConfigDialog = new ImageConfigWindow();
            imageConfigDialog.Owner = this;
            var imageConfigResult = imageConfigDialog.ShowDialog();
            if (!imageConfigResult.HasValue || imageConfigResult == false)
            {
                return;
            }

            var printDocument = new PrintDocument();
            printDocument.AddElement(new ImagePrintElement(filePath, imageConfigDialog.X1, imageConfigDialog.X2, imageConfigDialog.Y1, imageConfigDialog.Y2));
            await this._printerClient.PrintDocumentAsync(printDocument);
        }

        private async void ButtonPrintTextDemo_Click(object sender, RoutedEventArgs e)
        {
            if (!this.IsReady())
            {
                return;
            }

            var printDocument = new PrintDocument();

            printDocument.AddElement(new TextPrintElement("X0/Y0", 0, 0, TextSize.Small));
            printDocument.AddElement(new TextPrintElement("X0/Y65", 0, 65, TextSize.Small));

            printDocument.AddElement(new TextPrintElement("X0/Y35", 0, 35, TextSize.Medium));
            printDocument.AddElement(new TextPrintElement("X400/Y35", 400, 35, TextSize.Large));
            printDocument.AddElement(new TextPrintElement("X800/Y35", 800, 35, TextSize.Medium));

            printDocument.AddElement(new TextPrintElement("X840/Y0", 840, 0, TextSize.Small));
            printDocument.AddElement(new TextPrintElement("X840/Y65", 840, 65, TextSize.Small));

            printDocument.AddElement(new TextPrintElement("X200/Y65 Rotated90", 200, 65, TextSize.Medium, TextOrientation.Rotated90));
            printDocument.AddElement(new TextPrintElement("X300/Y20 double", 300, 20, TextSize.Medium, TextOrientation.Normal, true, true));

            await this._printerClient.PrintDocumentAsync(printDocument);
        }

        private async void ButtonPrintFullDemo_Click(object sender, RoutedEventArgs e)
        {
            if (!this.IsReady())
            {
                return;
            }

            var openFileDialog = new OpenFileDialog();
            var dialogResult = openFileDialog.ShowDialog();
            if (dialogResult.HasValue && !dialogResult.Value)
            {
                return;
            }

            var filePath = openFileDialog.FileName;

            var printDocument = new PrintDocument();
            printDocument.AddElement(new ImagePrintElement(filePath, 0, 400, 10, 60));
            printDocument.AddElement(new TextPrintElement("Max Mustermann", 500, 40, TextSize.Large));
            printDocument.AddElement(new TextPrintElement("Langestraße 4a", 500, 35, TextSize.Medium));
            printDocument.AddElement(new TextPrintElement("10115 Berlin", 500, 30, TextSize.Medium));

            await this._printerClient.PrintDocumentAsync(printDocument);
        }

        private async void ButtonLoadCardFromFront_Click(object sender, RoutedEventArgs e)
        {
            if (!this.IsReady())
            {
                return;
            }

            await this._printerClient.FeedCardFromFrontFeederAsync();
        }

        private async void ButtonEraseCard_Click(object sender, RoutedEventArgs e)
        {
            if (!this.IsReady())
            {
                return;
            }

            await this._printerClient.EraseAreaAsync();
        }

        private async void ButtonReadUid_Click(object sender, RoutedEventArgs e)
        {
            if (!this.IsReady())
            {
                return;
            }

            this.LabelRfid.Content = "please wait...";
            var rfidInfo = await this._printerClient.ReadCardMifareUidAsync();

            if (rfidInfo.Successful)
            {
                this.LabelRfid.Content = rfidInfo.Uid;
                return;
            }

            this.LabelRfid.Content = $"Error: {rfidInfo.ErrorMessage}";
        }
    }
}
