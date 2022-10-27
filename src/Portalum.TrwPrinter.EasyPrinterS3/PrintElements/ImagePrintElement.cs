using Portalum.TrwPrinter.EasyPrinterS3.Helpers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Text;

namespace Portalum.TrwPrinter.EasyPrinterS3.PrintElements
{
    public class ImagePrintElement : PrintElementBase
    {
        private readonly int _pixelMultiplier = 8;
        private readonly int _paddingByteCount = 13;

        private readonly string _imagePath;
        private readonly double _positionX1;
        private readonly double _positionX2;
        private readonly double _positionY1;
        private readonly double _positionY2;
        private readonly ElementOrientation _elementOrientation;

        public ImagePrintElement(
            string imagePath,
            double positionX1,
            double positionX2,
            double positionY1,
            double positionY2,
            ElementOrientation elementOrientation = ElementOrientation.Normal)
        {
            this._imagePath = imagePath;
            this._positionX1 = positionX1;
            this._positionX2 = positionX2;
            this._positionY1 = positionY1;
            this._positionY2 = positionY2;
            this._elementOrientation = elementOrientation;
        }

        private async Task<byte[]> PreparePrintImageAsync(CancellationToken cancellationToken = default)
        {
            var width = (this._positionX2 - this._positionX1);
            var height = (this._positionY2 - this._positionY1) * this._pixelMultiplier;
            var offsetY = (this._maxHeight - this._positionY2) * this._pixelMultiplier;

#if (NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
            var imageData = await File.ReadAllBytesAsync(this._imagePath, cancellationToken);
#else
            var imageData = File.ReadAllBytes(this._imagePath);
#endif

            var rotate = 90;
            if (this._elementOrientation == ElementOrientation.Rotated90)
            {
                rotate = 0;
            }

            using var image = Image.Load<Rgba32>(imageData);
            image.Mutate(x => x.Rotate(rotate).Resize((int)width, (int)height, false));

            using var printImage = new Image<Rgba32>((int)width, (int)(height + offsetY));

            printImage.Mutate(o => o
                .BackgroundColor(Color.White)
                .DrawImage(image, new Point(0, (int)offsetY), 1f)
                .Flip(FlipMode.Vertical)
            );

            using var memoryStreamPrintImage = new MemoryStream();
            await printImage.SaveAsPngAsync(memoryStreamPrintImage);

            return memoryStreamPrintImage.ToArray();
        }

        public override async Task<byte[]> GetPrintDataAsync(CancellationToken cancellationToken = default)
        {
            var paddingData = Enumerable.Repeat((byte)0x00, this._paddingByteCount).ToArray();
            var imagePrintCommand = new byte[] { 0x1B, 0x51 };

            var imagePositionCommandData = new byte[] { 0x1B, 0x25, 0x79 };
            var imagePositionData = Encoding.ASCII.GetBytes($"{this.ConvertX((int)this._positionX2):D4}");

            var printData = await this.PreparePrintImageAsync(cancellationToken);

            var imagePrintPackage = ImageHelper.GetImagePrintPackage(printData);

            using var memoryStream = new MemoryStream();

            await memoryStream.WriteAsync(imagePositionCommandData, 0, imagePositionCommandData.Length, cancellationToken);
            await memoryStream.WriteAsync(imagePositionData, 0, imagePositionData.Length, cancellationToken);

            for (var y = 0; y < imagePrintPackage.Rows; y++)
            {
                await memoryStream.WriteAsync(imagePrintCommand, 0, imagePrintCommand.Length, cancellationToken);

                var lengthInfoData = new byte[] { (byte)(imagePrintPackage.BytesPerRow + this._paddingByteCount) };
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
