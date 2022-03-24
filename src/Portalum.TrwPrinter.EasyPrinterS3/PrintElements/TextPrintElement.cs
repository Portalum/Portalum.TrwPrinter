using System.Text;

namespace Portalum.TrwPrinter.EasyPrinterS3.PrintElements
{
    public class TextPrintElement : PrintElementBase
    {
        private readonly string _text;
        private readonly int _positionX;
        private readonly int _positionY;
        private readonly TextSize _textSize;
        private readonly TextOrientation _textOrientation;

        public TextPrintElement(
            string text,
            int positionX = 0,
            int positionY = 0,
            TextSize textSize = TextSize.Medium,
            TextOrientation textOrientation = TextOrientation.Normal)
        {
            this._text = text;
            this._positionX = positionX;
            this._positionY = positionY;
            this._textSize = textSize;
            this._textOrientation = textOrientation;
        }

        public override async Task<byte[]> GetPrintDataAsync(CancellationToken cancellationToken = default)
        {
            using var memoryStream = new MemoryStream();

            //TODO: This is more a print document config
            var rotateWholeCardData = new byte[] { 0x1B, 0x55 };
            await memoryStream.WriteAsync(rotateWholeCardData, 0, rotateWholeCardData.Length, cancellationToken); //U (Rotates whole card (text and graphic) by 180°)

            //Orientation
            if (this._textOrientation == TextOrientation.Normal)
            {
                var textOrientationNormalData = new byte[] { 0x1B, 0x4E };
                await memoryStream.WriteAsync(textOrientationNormalData, 0, textOrientationNormalData.Length, cancellationToken); //N
            }
            else if (this._textOrientation == TextOrientation.Rotated90)
            {
                var textOrientationRotated90Data = new byte[] { 0x1B, 0x4 };
                await memoryStream.WriteAsync(textOrientationRotated90Data, 0, textOrientationRotated90Data.Length, cancellationToken); //O
            }

            //Format text
            switch (this._textSize)
            {
                case TextSize.Small:
                    var smallTextData = new byte[] { 0x1B, 0x43 };
                    await memoryStream.WriteAsync(smallTextData, 0, smallTextData.Length, cancellationToken);
                    break;
                case TextSize.Medium:
                    var mediumTextData = new byte[] { 0x1B, 0x43 };
                    await memoryStream.WriteAsync(mediumTextData, 0, mediumTextData.Length, cancellationToken);
                    break;
                case TextSize.Large:
                    var largeTextData = new byte[] { 0x1B, 0x43 };
                    await memoryStream.WriteAsync(largeTextData, 0, largeTextData.Length, cancellationToken);
                    break;
            }

            var printDoubleWidthData = new byte[] { 0x1B, 0x62, 0x30 };
            var printDoubleHeightData = new byte[] { 0x1B, 0x77, 0x30 };
            await memoryStream.WriteAsync(printDoubleWidthData, 0, printDoubleWidthData.Length, cancellationToken); //b0 (Print double-width characters)
            await memoryStream.WriteAsync(printDoubleHeightData, 0, printDoubleHeightData.Length, cancellationToken); //w0 (Print double-height characters)

            //Position
            var xPositionCommandData = new byte[] { 0x1B, 0x25, 0x78 }; //%x
            var xPositionData = Encoding.ASCII.GetBytes($"{this._positionX:D2}");
            var yPositionCommandData = new byte[] { 0x1B, 0x25, 0x79 }; //%y
            var yPositionData = Encoding.ASCII.GetBytes($"{this._positionY:D4}");

            await memoryStream.WriteAsync(xPositionCommandData, 0, xPositionCommandData.Length, cancellationToken);
            await memoryStream.WriteAsync(xPositionData, 0, xPositionData.Length, cancellationToken);
            await memoryStream.WriteAsync(yPositionCommandData, 0, yPositionCommandData.Length, cancellationToken);
            await memoryStream.WriteAsync(yPositionData, 0, yPositionData.Length, cancellationToken);

            //Content
            var contentData = Encoding.GetEncoding("ISO-8859-1").GetBytes(this._text);
            await memoryStream.WriteAsync(contentData, 0, contentData.Length, cancellationToken);

            return memoryStream.ToArray();
        }
    }
}
