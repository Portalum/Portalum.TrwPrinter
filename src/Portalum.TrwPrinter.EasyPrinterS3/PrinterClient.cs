using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Portalum.TrwPrinter.EasyPrinterS3.Helpers;
using Portalum.TrwPrinter.EasyPrinterS3.Models;
using System.Text;

namespace Portalum.TrwPrinter.EasyPrinterS3
{
    /// <summary>
    /// ANA-U EasyPrinter S3
    /// https://download.ana-u.com/eps3_doc/out/index.html
    /// </summary>
    public class PrinterClient : IDisposable
    {
        private readonly ILogger _logger;
        private readonly IDeviceCommunication _deviceCommunication;
        private readonly CancellationTokenSource _disposeCancellationTokenSource;
        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1);

        private bool _firePrinterStateChanged;
        private PrinterState _printerState;

        public PrinterState PrinterState => this._printerState;

        public event Action<PrinterState> PrinterStateChanged;

        /// <summary>
        /// PrinterClient
        /// </summary>
        /// <param name="deviceCommunication"></param>
        /// <param name="logger"></param>
        public PrinterClient(
            IDeviceCommunication deviceCommunication,
            ILogger logger = default)
        {
            if (logger == null)
            {
                logger = new NullLogger<PrinterClient>();
            }

            this._logger = logger;
            this._deviceCommunication = deviceCommunication;

            this._printerState = new PrinterState();
            this._firePrinterStateChanged = true;

            this._disposeCancellationTokenSource = new CancellationTokenSource();

            _ = Task.Run(async () =>
            {
                while (!this._disposeCancellationTokenSource.IsCancellationRequested)
                {
                    if (!this._deviceCommunication.IsConnected)
                    {
                        this._firePrinterStateChanged = true;

                        await Task.Delay(100, this._disposeCancellationTokenSource.Token);
                        continue;
                    }

                    var state = await this.GetShortStateAsync();
                    if (state == null)
                    {
                        await Task.Delay(100, this._disposeCancellationTokenSource.Token);
                        continue;
                    }

                    if (!this._printerState.Equals(state.Value) || this._firePrinterStateChanged)
                    {
                        this._firePrinterStateChanged = false;
                        this._printerState = state.Value;
                        this.PrinterStateChanged?.Invoke(state.Value);
                    }

                    await Task.Delay(100, this._disposeCancellationTokenSource.Token);
                }
            }, this._disposeCancellationTokenSource.Token);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this._disposeCancellationTokenSource.Cancel();
                this._semaphoreSlim.Dispose();
            }
        }

        private async Task<byte[]> SendAndReceiveAsync(
            byte[] sendData,
            CancellationToken cancellationToken = default)
        {
            await this._semaphoreSlim.WaitAsync();

            byte[] buffer = null;

            using var receiveCancellationTokenSource = new CancellationTokenSource();
            using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, receiveCancellationTokenSource.Token);

            void DataReceived(byte[] receivedData)
            {
                buffer = receivedData;

                receiveCancellationTokenSource.Cancel();
            }

            try
            {
                this._deviceCommunication.DataReceived += DataReceived;

                await this._deviceCommunication.SendAsync(sendData);
                var isDataReceived = await Task.Delay(TimeSpan.FromMilliseconds(2000), linkedCancellationTokenSource.Token)
                    .ContinueWith(task =>
                    {
                        if (task.IsCanceled)
                        {
                            if (receiveCancellationTokenSource.IsCancellationRequested)
                            {
                                return true;
                            }
                        }

                        return false;
                    });

                if (!isDataReceived)
                {
                    return Array.Empty<byte>();
                }

                return buffer;
            }
            finally
            {
                this._deviceCommunication.DataReceived -= DataReceived;
                this._semaphoreSlim.Release();
            }
        }

        public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
        {
            return await this._deviceCommunication.ConnectAsync(cancellationToken);
        }

        public async Task DisconnectAsync()
        {
            await this._deviceCommunication.DisconnectAsync();
        }

        public async Task<PrinterState?> GetShortStateAsync(CancellationToken cancellationToken = default)
        {
            var commandData = new byte[] { 0x1A };
            var receivedData = await this.SendAndReceiveAsync(commandData, cancellationToken);
            if (receivedData.Length == 0)
            {
                return null;
            }

            var byteBitInfo = BitHelper.GetBits(receivedData[0]);
            return new PrinterState(byteBitInfo);
        }

        public async Task RebootAsync(CancellationToken cancellationToken = default)
        {
            var commandData = new byte[] { 0x1B, 0x24, 0x52 };
            await this._deviceCommunication.SendAsync(commandData, cancellationToken);
        }

        public async Task<string> GetFirmwareAsync(CancellationToken cancellationToken = default)
        {
            var commandData = new byte[] { 0x1B, 0x61 };
            var receivedData = await this.SendAndReceiveAsync(commandData, cancellationToken);
            return Encoding.ASCII.GetString(receivedData).TrimEnd();
        }

        public async Task<string> GetPrinterSerialNumberAsync(CancellationToken cancellationToken = default)
        {
            var commandData = new byte[] { 0x1B, 0x25, 0x6F, 0x53 };
            var receivedData = await this.SendAndReceiveAsync(commandData, cancellationToken);
            return Encoding.ASCII.GetString(receivedData).TrimEnd();
        }

        public async Task EjectCardAsync(CancellationToken cancellationToken = default)
        {
            var commandData = new byte[] { 0x1B, 0x33 };
            await this._deviceCommunication.SendAsync(commandData, cancellationToken);
        }

        public async Task FeedCardFromFrontFeederAsync(CancellationToken cancellationToken = default)
        {
            var commandData = new byte[] { 0x1B, 0x31 };
            await this._deviceCommunication.SendAsync(commandData, cancellationToken);
        }

        public async Task FeedCardFromHopperAsync(CancellationToken cancellationToken = default)
        {
            var commandData = new byte[] { 0x1B, 0x63 };
            await this._deviceCommunication.SendAsync(commandData, cancellationToken);
        }

        public async Task AbortFeedAsync(CancellationToken cancellationToken = default)
        {
            var commandData = new byte[] { 0x1B, 0x32 };
            await this._deviceCommunication.SendAsync(commandData, cancellationToken);
        }

        public async Task ReadCardUidAsync(CancellationToken cancellationToken = default)
        {
            //Toggle card
            //var commandData1 = new byte[] { 0x1B, 0x42 };
            //await this._deviceCommunication.SendAsync(commandData1, cancellationToken);

            var commandData = new byte[] { 0x1B, 0xA7 };
            var receivedData = await this.SendAndReceiveAsync(commandData, cancellationToken);
        }

        public async Task<string> ReadCardDirectionAsync(CancellationToken cancellationToken = default)
        {
            var commandData = new byte[] { 0x1B, 0x23 };
            var receivedData = await this.SendAndReceiveAsync(commandData, cancellationToken);
            return Encoding.ASCII.GetString(receivedData);
        }

        public async Task<RfidInfo> ReadCardMifareUidAsync(CancellationToken cancellationToken = default)
        {
            var commandData = new byte[] { 0x1B, 0x24, 0x24, 0x55 };
            var receivedData = await this.SendAndReceiveAsync(commandData, cancellationToken);

            if (receivedData.Length != 0)
            {
                if (receivedData[0] == 0x53 && receivedData.Last() == 0x0D)
                {
                    var uid = Encoding.ASCII.GetString(receivedData.Skip(2).ToArray()).TrimEnd();

                    return new RfidInfo
                    {
                        Successful = true,
                        Uid = uid
                    };
                }

                if (receivedData[0] == 0x45)
                {
                    return new RfidInfo
                    {
                        Successful = false,
                        ErrorMessage = "Read error"
                    };
                }
            }

            return new RfidInfo
            {
                Successful = false,
                ErrorMessage = "Unknown error"
            };
        }

        public async Task EraseAreaAsync(CancellationToken cancellationToken = default)
        {
            var startX = 0;
            var endX = 96;
            var startY = 0;
            var endY = 1100;

            var setEraseAreaModeCommandData = new byte[] { 0x1B, 0x4C, 0x31 }; //L1
            await this._deviceCommunication.SendAsync(setEraseAreaModeCommandData, cancellationToken);

            var setEraseAreaModePositionData = Encoding.ASCII.GetBytes($"{startX:D2}{startY:D4}{endX:D2}{endY:D4}");
            await this._deviceCommunication.SendAsync(setEraseAreaModePositionData, cancellationToken);   

            //Start Print
            var startPrintData = new byte[] { 0x0C }; //\f
            await this._deviceCommunication.SendAsync(startPrintData, cancellationToken);
        }

        public async Task PrintDocumentAsync(
            PrintDocument printDocument,
            CancellationToken cancellationToken = default)
        {
            var printData = await printDocument.GetPrintDataAsync();
            await this._deviceCommunication.SendAsync(printData, cancellationToken);
        }

        /// <summary>
        /// Set a text on the display of the printer
        /// </summary>
        /// <param name="displayTextConfig"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task SetDisplayTextAsync(
            DisplayTextConfig displayTextConfig,
            CancellationToken cancellationToken = default)
        {
            var commandData = new byte[] { 0x1B, 0x24, 0x44, 0x31 };
            var lineLength = 20;

            using (var memoryStream = new MemoryStream())
            {
                await memoryStream.WriteAsync(commandData, 0, commandData.Length, cancellationToken);

                var line1 = displayTextConfig.Line1 ?? string.Empty;
                var line2 = displayTextConfig.Line2 ?? string.Empty;
                var line3 = displayTextConfig.Line3 ?? string.Empty;
                var line4 = displayTextConfig.Line4 ?? string.Empty;

                var line1Data = Encoding.ASCII.GetBytes(line1.PadRight(lineLength, ' '));
                var line2Data = Encoding.ASCII.GetBytes(line2.PadRight(lineLength, ' '));
                var line3Data = Encoding.ASCII.GetBytes(line3.PadRight(lineLength, ' '));
                var line4Data = Encoding.ASCII.GetBytes(line4.PadRight(lineLength, ' '));

                await memoryStream.WriteAsync(line1Data, 0, line1Data.Length, cancellationToken);
                await memoryStream.WriteAsync(line2Data, 0, line2Data.Length, cancellationToken);
                await memoryStream.WriteAsync(line3Data, 0, line3Data.Length, cancellationToken);
                await memoryStream.WriteAsync(line4Data, 0, line4Data.Length, cancellationToken);

                var commdandData = memoryStream.ToArray();
                await this._deviceCommunication.SendAsync(commdandData, cancellationToken);
            }
        }

        /// <summary>
        /// Reset the text on the display of the printer to default
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task ResetDisplayTextAsync(CancellationToken cancellationToken = default)
        {
            var commandData = new byte[] { 0x1B, 0x24, 0x44, 0x30 };
            await this._deviceCommunication.SendAsync(commandData, cancellationToken);
        }
    }
}