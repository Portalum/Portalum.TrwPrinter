using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.IO.Ports;

namespace Portalum.TrwPrinter.EasyPrinterS3
{
    /// <summary>
    /// SerialPort DeviceCommunication
    /// </summary>
    public class SerialPortDeviceCommunication : IDeviceCommunication
    {
        private readonly ILogger<SerialPortDeviceCommunication> _logger;
        private readonly string _comPort;
        private readonly SerialPort _serialPort;

        /// <inheritdoc />
        public event Action<byte[]> DataReceived;

        /// <inheritdoc />
        public event Action<byte[]> DataSent;

        /// <inheritdoc />
        public event Action<ConnectionState> ConnectionStateChanged;

        /// <summary>
        /// SerialPort DeviceCommunication
        /// </summary>
        /// <param name="comPort"></param>
        /// <param name="baudRate"></param>
        /// <param name="parity"></param>
        /// <param name="dataBits"></param>
        /// <param name="stopBits"></param>
        /// <param name="logger"></param>
        public SerialPortDeviceCommunication(
            string comPort,
            int baudRate,
            Parity parity,
            int dataBits,
            StopBits stopBits,
            ILogger<SerialPortDeviceCommunication> logger = default)
        {
            this._comPort = comPort;

            if (logger == null)
            {
                logger = new NullLogger<SerialPortDeviceCommunication>();
            }
            this._logger = logger;

            this._serialPort = new SerialPort(comPort, baudRate, parity, dataBits, stopBits);
            this._serialPort.DataReceived += this.Receive;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (disposing)
            {
                this._serialPort.DataReceived -= this.Receive;
                this._serialPort.Dispose();
            }
        }

        /// <inheritdoc />
        public bool IsConnected
        {
            get { return this._serialPort.IsOpen; }
        }

        /// <inheritdoc />
        public string ConnectionIdentifier
        {
            get { return this._comPort; }
        }

        /// <inheritdoc />
        public Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                this._serialPort.Open();

                return Task.FromResult(true);
            }
            catch (Exception exception)
            {
                this._logger.LogError($"{nameof(ConnectAsync)} - {exception}");
            }

            return Task.FromResult(false);
        }

        /// <inheritdoc />
        public Task<bool> DisconnectAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                this._serialPort.DiscardInBuffer();
                this._serialPort.DiscardOutBuffer();

                this._serialPort.Close();

                return Task.FromResult(true);
            }
            catch (Exception exception)
            {
                this._logger.LogError($"{nameof(DisconnectAsync)} - {exception}");
            }

            return Task.FromResult(false);
        }

        private void Disconnected()
        {
            this._logger.LogInformation($"{nameof(Disconnected)}");

            this.ConnectionStateChanged?.Invoke(ConnectionState.Disconnected);
        }

        /// <inheritdoc />
        public Task SendAsync(
            byte[] data,
            CancellationToken cancellationToken = default)
        {
            this.DataSent?.Invoke(data);

            if (this._logger.IsEnabled(LogLevel.Trace))
            {
                this._logger.LogTrace($"{nameof(SendAsync)} - {BitConverter.ToString(data)}");
            }

            this._serialPort.Write(data, 0, data.Length);

            return Task.CompletedTask;
        }

        private void Receive(object sender, SerialDataReceivedEventArgs e)
        {
            if (this._serialPort.BytesToRead == 0)
            {
                return;
            }

            var buffer = new byte[this._serialPort.BytesToRead];

            this._serialPort.Read(buffer, 0, buffer.Length);

            if (this._logger.IsEnabled(LogLevel.Trace))
            {
                this._logger.LogTrace($"{nameof(Receive)} - {BitConverter.ToString(buffer)}");
            }
            this.DataReceived?.Invoke(buffer);
        }
    }

}
