namespace Portalum.TrwPrinter.EasyPrinterS3.PrintElements
{
    public abstract class PrintElementBase
    {
        protected PrintPositionInfo _printPositionInfo;

        public void SetPrintPositionInfo(PrintPositionInfo printPositionInfo)
        {
            this._printPositionInfo = printPositionInfo;
        }

        /// <summary>
        /// blabla super doku
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public abstract Task<byte[]> GetPrintDataAsync(CancellationToken cancellationToken = default);

        protected int GetPrinterPositionX(int x)
        {
            return x + this._printPositionInfo.OffsetX;
        }

        protected int ConvertX(int posX)
        {
            if (posX < 0) { posX = 0; }
            if (posX > 960) { posX = 960; }
            posX = Math.Abs(posX - 960) + 130 - this._printPositionInfo.OffsetX;
            return posX;
        }

        protected int ConvertY(int posY)
        {
            if (posY < 0) { posY = 0; }
            if (posY > 70) { posY = 70; }
            posY = Math.Abs(posY - 70) + 13;
            return posY;
        }
    }
}
