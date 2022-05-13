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
        private readonly bool _textDoubleWidth;
        private readonly bool _textDoubleHeight;

        public TextPrintElement(
            string text,
            int positionX = 0,
            int positionY = 0,
            TextSize textSize = TextSize.Medium,
            TextOrientation textOrientation = TextOrientation.Normal,
            bool textDoubleWidth = false,
            bool textDoubleHeight = false)
        {
            this._text = text;
            this._positionX = positionX;
            this._positionY = positionY;
            this._textSize = textSize;
            this._textOrientation = textOrientation;
            this._textDoubleWidth = textDoubleWidth;
            this._textDoubleHeight = textDoubleHeight;
        }

        public override async Task<byte[]> GetPrintDataAsync(CancellationToken cancellationToken = default)
        {
            using var memoryStream = new MemoryStream();

            //Orientation
            if (this._textOrientation == TextOrientation.Normal)
            {
                var textOrientationNormalData = new byte[] { 0x1B, 0x4E };
                await memoryStream.WriteAsync(textOrientationNormalData, 0, textOrientationNormalData.Length, cancellationToken); //N
            }
            else if (this._textOrientation == TextOrientation.Rotated90)
            {
                var textOrientationRotated90Data = new byte[] { 0x1B, 0x4F };
                await memoryStream.WriteAsync(textOrientationRotated90Data, 0, textOrientationRotated90Data.Length, cancellationToken); //O
            }

            //Text size
            switch (this._textSize)
            {
                case TextSize.Small:
                    var smallTextData = new byte[] { 0x1B, 0x43 };
                    await memoryStream.WriteAsync(smallTextData, 0, smallTextData.Length, cancellationToken);
                    break;
                case TextSize.Medium:
                    var mediumTextData = new byte[] { 0x1B, 0x44 };
                    await memoryStream.WriteAsync(mediumTextData, 0, mediumTextData.Length, cancellationToken);
                    break;
                case TextSize.Large:
                    var largeTextData = new byte[] { 0x1B, 0x45 };
                    await memoryStream.WriteAsync(largeTextData, 0, largeTextData.Length, cancellationToken);
                    break;
            }

            //Text Double width
            var printDoubleWidthData = new byte[] { 0x1B, 0x62 }; //b0
            await memoryStream.WriteAsync(printDoubleWidthData, 0, printDoubleWidthData.Length, cancellationToken);

            if (this._textDoubleWidth)
            {
                memoryStream.WriteByte(0x31);
            }
            else
            {
                memoryStream.WriteByte(0x30);
            }

            //Text Double height
            var printDoubleHeightData = new byte[] { 0x1B, 0x77 }; //w0
            await memoryStream.WriteAsync(printDoubleHeightData, 0, printDoubleHeightData.Length, cancellationToken);

            if (this._textDoubleHeight)
            {
                memoryStream.WriteByte(0x31);
            }
            else
            {
                memoryStream.WriteByte(0x30);
            }

            //Position
            var convertedPositionX = this.ConvertX(this._positionX);
            var convertedPositionY = this.ConvertY(this._positionY);

            var xPositionCommandData = new byte[] { 0x1B, 0x25, 0x78 }; //%x
            var xPositionData = Encoding.ASCII.GetBytes($"{convertedPositionY:D2}");
            var yPositionCommandData = new byte[] { 0x1B, 0x25, 0x79 }; //%y
            var yPositionData = Encoding.ASCII.GetBytes($"{convertedPositionX:D4}");

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
