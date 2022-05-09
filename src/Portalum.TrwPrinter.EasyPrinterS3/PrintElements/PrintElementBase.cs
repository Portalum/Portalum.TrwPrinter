namespace Portalum.TrwPrinter.EasyPrinterS3.PrintElements
{
    public abstract class PrintElementBase
    {
        public abstract Task<byte[]> GetPrintDataAsync(CancellationToken cancellationToken = default);

        public double GetPrinterX(int virtualX)
        {
            return virtualX * 2;//Doofe formel
        }

        public double GetPrinterY(int virtualY)
        {
            return virtualY * 2;//Doofe formel
        }
    }
}
