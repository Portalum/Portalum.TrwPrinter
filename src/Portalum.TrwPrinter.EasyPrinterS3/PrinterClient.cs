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

        private byte[] _dataBuffer;
        private PrinterState _printerState;

        public PrinterState PrinterState => this._printerState;

        public event Action<PrinterState> PrinterStateChanged;

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

            this._disposeCancellationTokenSource = new CancellationTokenSource();

            _ = Task.Run(async () =>
            {
                while (!this._disposeCancellationTokenSource.IsCancellationRequested)
                {
                    if (!this._deviceCommunication.IsConnected)
                    {
                        await Task.Delay(100, this._disposeCancellationTokenSource.Token);
                        continue;
                    }

                    var state = await this.GetShortStateAsync();
                    if (state == null)
                    {
                        await Task.Delay(100, this._disposeCancellationTokenSource.Token);
                        continue;
                    }

                    this._printerState = state;
                    this.PrinterStateChanged?.Invoke(state);

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
            }
        }

        private void DataReceived(byte[] data)
        {
            var dataHex = BitConverter.ToString(data);
            this._logger.LogInformation($"{nameof(DataReceived)} - {dataHex}");

            this._dataBuffer = data;
        }

        private async Task<byte[]> SendAndReceiveAsync(
            byte[] sendData,
            CancellationToken cancellationToken = default)
        {
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

        public async Task<PrinterState> GetShortStateAsync(CancellationToken cancellationToken = default)
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

        public async Task SendEraseAreaAsync(CancellationToken cancellationToken = default)
        {
            var startX = 0;
            var endX = 96;
            var startY = 0;
            var endY = 1100;

            //Set erase area mode
            var deleteCardData = new byte[] { 0x1B, 0x4C, 0x31 }; //L1
            await this._deviceCommunication.SendAsync(deleteCardData, cancellationToken);

            var someData = Encoding.ASCII.GetBytes($"{startX:D2}{startY:D4}{endX:D2}{endY:D4}");
            await this._deviceCommunication.SendAsync(someData, cancellationToken);   

            //Start Print
            var startPrintData = new byte[] { 0x0C }; //\f
            await this._deviceCommunication.SendAsync(startPrintData, cancellationToken);
        }

        public async Task SendPrintDemoCardAsync(
            PrintDocument printDocument,
            CancellationToken cancellationToken = default)
        {
            var printData = await printDocument.GetPrintDataAsync();
            await this._deviceCommunication.SendAsync(printData, cancellationToken);
        }

        public async Task SetDisplayTextAsync(
            DisplayTextConfig displayTextConfig,
            CancellationToken cancellationToken = default)
        {
            var commandData = new byte[] { 0x1B, 0x24, 0x44, 0x31 };
            var lineLength = 20;

            using (var memoryStream = new MemoryStream())
            {
                memoryStream.Write(commandData);

                var line1 = displayTextConfig.Line1 ?? string.Empty;
                var line2 = displayTextConfig.Line2 ?? string.Empty;
                var line3 = displayTextConfig.Line3 ?? string.Empty;
                var line4 = displayTextConfig.Line4 ?? string.Empty;

                var line1Data = Encoding.ASCII.GetBytes(line1.PadRight(lineLength, ' '));
                var line2Data = Encoding.ASCII.GetBytes(line2.PadRight(lineLength, ' '));
                var line3Data = Encoding.ASCII.GetBytes(line3.PadRight(lineLength, ' '));
                var line4Data = Encoding.ASCII.GetBytes(line4.PadRight(lineLength, ' '));

                memoryStream.Write(line1Data);
                memoryStream.Write(line2Data);
                memoryStream.Write(line3Data);
                memoryStream.Write(line4Data);

                await this._deviceCommunication.SendAsync(memoryStream.ToArray(), cancellationToken);
            }
        }

        public async Task ResetDisplayTextAsync(CancellationToken cancellationToken = default)
        {
            var commandData = new byte[] { 0x1B, 0x24, 0x44, 0x30 };
            await this._deviceCommunication.SendAsync(commandData, cancellationToken);
        }

        //public async Task GetStatusAsync(CancellationToken cancellationToken = default)
        //{
        //    //var commandData = new byte[] { 0x1A };
        //    var commandData = new byte[] { 0x1B, 0x3F };
        //    await this._deviceCommunication.SendAsync(commandData, cancellationToken);
        //}
    }
}