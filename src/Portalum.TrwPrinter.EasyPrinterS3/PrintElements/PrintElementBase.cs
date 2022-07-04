namespace Portalum.TrwPrinter.EasyPrinterS3.PrintElements
{
    public abstract class PrintElementBase
    {
        protected readonly int _startingPointOfPrintingAreaX = 130;
        protected readonly int _startingPointOfPrintingAreaY = 13;
        protected readonly int _maxWidth = 960;
        protected readonly int _maxHeight = 70;

        public abstract Task<byte[]> GetPrintDataAsync(CancellationToken cancellationToken = default);

        protected int ConvertX(int posX)
        {
            if (posX < 0)
            {
                posX = 0;
            }

            if (posX > this._maxWidth)
            { 
                posX = this._maxWidth;
            }

            return Math.Abs(posX - this._maxWidth) + this._startingPointOfPrintingAreaX;
        }

        protected int ConvertY(int posY)
        {
            if (posY < 0)
            { 
                posY = 0;
            }

            if (posY > this._maxHeight)
            {
                posY = this._maxHeight;
            }

            return Math.Abs(posY - this._maxHeight) + this._startingPointOfPrintingAreaY;
        }
    }
}
