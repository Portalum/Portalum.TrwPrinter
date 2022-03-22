namespace Portalum.TrwPrinter.EasyPrinterS3.PrintElements
{
    public abstract class PrintElementBase
    {
        public abstract Task<byte[]> GetPrintDataAsync(CancellationToken cancellationToken = default);
    }
}
