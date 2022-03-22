using System.Collections;
using System.Text;

namespace Portalum.TrwPrinter.EasyPrinterS3.PrintElements
{
    public class RectanglePrintElement : PrintElementBase
    {
        private readonly int _width;
        private readonly int _height;
        private readonly int _positionY;

        public RectanglePrintElement(
            int width = 8,
            int height = 8,
            int positionY = 0)
        {
            this._width = width;
            this._height = height;
            this._positionY = positionY;
        }

        public override async Task<byte[]> GetPrintDataAsync(CancellationToken cancellationToken = default)
        {
            var paddingByteCount = 13;
            var paddingData = Enumerable.Repeat((byte)0x00, paddingByteCount).ToArray();
            var fullLineData = this.DrawFullLine();
            var internalAreaData = this.DrawInternalArea();
            var imageRowInfoData = new byte[] { 0x1B, 0x51, (byte)(fullLineData.Length + paddingByteCount) };

            var imagePositionCommand = new byte[] { 0x1B, 0x25, 0x79 }; //%y
            var imagePosition = Encoding.ASCII.GetBytes($"{this._positionY:D4}");

            using var memoryStream = new MemoryStream();

            await memoryStream.WriteAsync(imagePositionCommand, cancellationToken);
            await memoryStream.WriteAsync(imagePosition, cancellationToken);

            await memoryStream.WriteAsync(imageRowInfoData, cancellationToken);
            await memoryStream.WriteAsync(paddingData, cancellationToken);
            await memoryStream.WriteAsync(fullLineData, cancellationToken);

            for (var height = 0; height < this._height - 2; height++)
            {
                await memoryStream.WriteAsync(imageRowInfoData, cancellationToken);
                await memoryStream.WriteAsync(paddingData, cancellationToken);
                await memoryStream.WriteAsync(internalAreaData, cancellationToken);
            }

            await memoryStream.WriteAsync(imageRowInfoData, cancellationToken);
            await memoryStream.WriteAsync(paddingData, cancellationToken);
            await memoryStream.WriteAsync(fullLineData, cancellationToken);

            return memoryStream.ToArray();
        }

        private int RequiredBytes()
        {
            var bitsPerByte = 8;
            var temp = this._width / (double)bitsPerByte;
            return (int)Math.Ceiling(temp);
        }

        private byte[] DrawFullLine()
        {
            var requiredBytes = this.RequiredBytes();
            var data = new byte[requiredBytes];

            var bitArray = new BitArray(data);
            for (var i = 0; i < this._width; i++)
            {
                bitArray[i] = true;
            }

            bitArray.CopyTo(data, 0);

            return data;
        }

        private byte[] DrawInternalArea()
        {
            var requiredBytes = this.RequiredBytes();
            var data = new byte[requiredBytes];

            var bitArray = new BitArray(data);
            bitArray[7] = true;
            bitArray[this._width - 8] = true;

            bitArray.CopyTo(data, 0);

            return data;
        }
    }
}
