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
            await memoryStream.WriteAsync(new byte[] { 0x1B, 0x55 }, cancellationToken); //U (Rotates whole card (text and graphic) by 180 degrees.)

            //Orientation
            if (this._textOrientation == TextOrientation.Normal)
            {
                await memoryStream.WriteAsync(new byte[] { 0x1B, 0x4E }, cancellationToken); //N
            }
            else if (this._textOrientation == TextOrientation.Rotated90)
            {
                await memoryStream.WriteAsync(new byte[] { 0x1B, 0x4F }, cancellationToken); //O
            }

            //Format text
            switch (this._textSize)
            {
                case TextSize.Small:
                    await memoryStream.WriteAsync(new byte[] { 0x1B, 0x43 }, cancellationToken);
                    break;
                case TextSize.Medium:
                    await memoryStream.WriteAsync(new byte[] { 0x1B, 0x44 }, cancellationToken);
                    break;
                case TextSize.Large:
                    await memoryStream.WriteAsync(new byte[] { 0x1B, 0x45 }, cancellationToken);
                    break;
            }
            
            await memoryStream.WriteAsync(new byte[] { 0x1B, 0x62, 0x30 }, cancellationToken); //b0 (Print double-width characters)
            await memoryStream.WriteAsync(new byte[] { 0x1B, 0x77, 0x30 }, cancellationToken); //w0 (Print double-height characters)

            //Position
            await memoryStream.WriteAsync(new byte[] { 0x1B, 0x25, 0x78 }, cancellationToken); //%x
            await memoryStream.WriteAsync(Encoding.ASCII.GetBytes($"{this._positionX:D2}"), cancellationToken);
            await memoryStream.WriteAsync(new byte[] { 0x1B, 0x25, 0x79 }, cancellationToken);//%y
            await memoryStream.WriteAsync(Encoding.ASCII.GetBytes($"{this._positionY:D4}"), cancellationToken);

            //Content
            await memoryStream.WriteAsync(Encoding.ASCII.GetBytes(this._text), cancellationToken);

            return memoryStream.ToArray();
        }
    }
}
