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

        public async Task<byte[]> GetPrintDataAsync()
        {
            var startX = 0;
            var endX = 96;
            var startY = 100;
            var endY = 1100;

            using var memoryStream = new MemoryStream();

            //Limit print area
            var limitPrintAreaData1 = new byte[] { 0x1B, 0x25, 0x64 }; //%d
            await memoryStream.WriteAsync(limitPrintAreaData1);

            var limitPrintAreaData2 = Encoding.ASCII.GetBytes($"{startY:D4}{endY:D4}");
            await memoryStream.WriteAsync(limitPrintAreaData2);

            //Set erase area mode
            var setEraseAreaModeCommand = new byte[] { 0x1B, 0x4C, 0x31 }; //L1
            await memoryStream.WriteAsync(setEraseAreaModeCommand);

            var setEraseAreaModePosition = Encoding.ASCII.GetBytes($"{startX:D2}{startY:D4}{endX:D2}{endY:D4}");
            await memoryStream.WriteAsync(setEraseAreaModePosition);

            foreach (var printElement in this._printElements)
            {
                await memoryStream.WriteAsync(await printElement.GetPrintDataAsync());
            }

            var startPrintData = new byte[] { 0x0C }; //\f
            await memoryStream.WriteAsync(startPrintData);

            return memoryStream.ToArray();
        }
    }
}
