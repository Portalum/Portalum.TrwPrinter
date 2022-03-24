using Portalum.TrwPrinter.EasyPrinterS3.PrintElements;
using System.Text;

namespace Portalum.TrwPrinter.EasyPrinterS3
{
    public class PrintDocument
    {
        private readonly List<PrintElementBase> _printElements;

        public PrintDocument()
        {
            this._printElements = new List<PrintElementBase>();
        }

        public void AddElement(PrintElementBase printElement)
        {
            this._printElements.Add(printElement);
        }

        public async Task<byte[]> GetPrintDataAsync(CancellationToken cancellationToken = default)
        {
            var startX = 0;
            var endX = 96;
            var startY = 100;
            var endY = 1100;

            using var memoryStream = new MemoryStream();

            //Limit print area
            var limitPrintAreaCommandData = new byte[] { 0x1B, 0x25, 0x64 }; //%d
            await memoryStream.WriteAsync(limitPrintAreaCommandData, 0, limitPrintAreaCommandData.Length, cancellationToken);

            var limitPrintAreaPositionData = Encoding.ASCII.GetBytes($"{startY:D4}{endY:D4}");
            await memoryStream.WriteAsync(limitPrintAreaPositionData, 0, limitPrintAreaPositionData.Length, cancellationToken);

            //Set erase area mode
            var setEraseAreaModeCommandData = new byte[] { 0x1B, 0x4C, 0x31 }; //L1
            await memoryStream.WriteAsync(setEraseAreaModeCommandData, 0, setEraseAreaModeCommandData.Length, cancellationToken);

            var setEraseAreaModePositionData = Encoding.ASCII.GetBytes($"{startX:D2}{startY:D4}{endX:D2}{endY:D4}");
            await memoryStream.WriteAsync(setEraseAreaModePositionData, 0, setEraseAreaModePositionData.Length, cancellationToken);

            foreach (var printElement in this._printElements)
            {
                var elementPrintData = await printElement.GetPrintDataAsync();
                await memoryStream.WriteAsync(elementPrintData, 0, elementPrintData.Length, cancellationToken);
            }

            var startPrintData = new byte[] { 0x0C }; //\f
            await memoryStream.WriteAsync(startPrintData, 0, startPrintData.Length, cancellationToken);

            return memoryStream.ToArray();
        }
    }
}
