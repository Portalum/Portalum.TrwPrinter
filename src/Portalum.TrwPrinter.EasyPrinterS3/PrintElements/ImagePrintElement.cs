using Portalum.TrwPrinter.EasyPrinterS3.Helpers;
using System.Text;

namespace Portalum.TrwPrinter.EasyPrinterS3.PrintElements
{
    public class ImagePrintElement : PrintElementBase
    {
        private readonly string _imagePath;
        private readonly int _positionY;

        public ImagePrintElement(
            string imagePath,
            int positionY = 0)
        {
            this._imagePath = imagePath;
            this._positionY = positionY;
        }

        public override async Task<byte[]> GetPrintDataAsync(CancellationToken cancellationToken = default)
        {
            var paddingByteCount = 13;
            var paddingData = Enumerable.Repeat((byte)0x00, paddingByteCount).ToArray();
            var imagePrintCommand = new byte[] { 0x1B, 0x51 };

            var imageData = await File.ReadAllBytesAsync(this._imagePath, cancellationToken);
            var imagePrintPackage = ImageHelper.GetImagePrintPackage(imageData);

            var imagePositionCommand = new byte[] { 0x1B, 0x25, 0x79 }; //%y
            var imagePosition = Encoding.ASCII.GetBytes($"{this._positionY:D4}");

            using var memoryStream = new MemoryStream();

            await memoryStream.WriteAsync(imagePositionCommand);
            await memoryStream.WriteAsync(imagePosition);

            for (var y = 0; y < imagePrintPackage.Rows; y++)
            {
                await memoryStream.WriteAsync(imagePrintCommand, 0, imagePrintCommand.Length, cancellationToken);

                var lengthInfoData = new byte[] { (byte)(imagePrintPackage.BytesPerRow + paddingByteCount) };
                await memoryStream.WriteAsync(lengthInfoData, 0, lengthInfoData.Length, cancellationToken);

                await memoryStream.WriteAsync(paddingData, 0, paddingData.Length, cancellationToken);

                var imageRowData = imagePrintPackage.PrintData.Skip(y * imagePrintPackage.BytesPerRow).Take(imagePrintPackage.BytesPerRow).ToArray();
                Array.Reverse(imageRowData);
                await memoryStream.WriteAsync(imageRowData, 0, imageRowData.Length, cancellationToken);
            }

            return memoryStream.ToArray();
        }
    }
}
