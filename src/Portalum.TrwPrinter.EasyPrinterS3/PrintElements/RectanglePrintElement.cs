using System.Collections;
using System.Text;

namespace Portalum.TrwPrinter.EasyPrinterS3.PrintElements
{
    public class RectanglePrintElement : PrintElementBase
    {
        private readonly int _width;
        private readonly int _height;
        private readonly int _positionX;

        public RectanglePrintElement(
            int width = 8,
            int height = 8,
            int positionX = 0)
        {
            this._width = width;
            this._height = height;
            this._positionX = positionX;
        }

        public override async Task<byte[]> GetPrintDataAsync(CancellationToken cancellationToken = default)
        {
            var paddingByteCount = 13;
            var paddingData = Enumerable.Repeat((byte)0x00, paddingByteCount).ToArray();
            var fullLineData = this.DrawFullLine();
            var internalAreaData = this.DrawInternalArea();
            var imageRowInfoData = new byte[] { 0x1B, 0x51, (byte)(fullLineData.Length + paddingByteCount) };

            var imagePositionCommandData = new byte[] { 0x1B, 0x25, 0x79 }; //%y
            var imagePositionData = Encoding.ASCII.GetBytes($"{this._positionX:D4}");

            using var memoryStream = new MemoryStream();

            await memoryStream.WriteAsync(imagePositionCommandData, 0, imagePositionCommandData.Length, cancellationToken);
            await memoryStream.WriteAsync(imagePositionData, 0, imagePositionData.Length, cancellationToken);

            await memoryStream.WriteAsync(imageRowInfoData, 0, imageRowInfoData.Length, cancellationToken);
            await memoryStream.WriteAsync(paddingData, 0, paddingData.Length, cancellationToken);
            await memoryStream.WriteAsync(fullLineData, 0, fullLineData.Length, cancellationToken);

            for (var height = 0; height < this._height - 2; height++)
            {
                await memoryStream.WriteAsync(imageRowInfoData, 0, imageRowInfoData.Length, cancellationToken);
                await memoryStream.WriteAsync(paddingData, 0, paddingData.Length, cancellationToken);
                await memoryStream.WriteAsync(internalAreaData, 0, internalAreaData.Length, cancellationToken);
            }

            await memoryStream.WriteAsync(imageRowInfoData, 0, imageRowInfoData.Length, cancellationToken);
            await memoryStream.WriteAsync(paddingData, 0, paddingData.Length, cancellationToken);
            await memoryStream.WriteAsync(fullLineData, 0, fullLineData.Length, cancellationToken);

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
