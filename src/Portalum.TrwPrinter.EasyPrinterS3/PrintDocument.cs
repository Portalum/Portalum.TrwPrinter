using Portalum.TrwPrinter.EasyPrinterS3.PrintElements;
using System.Text;

namespace Portalum.TrwPrinter.EasyPrinterS3
{
    public class PrintDocument
    {
        private readonly List<PrintElementBase> _printElements;
        private readonly bool _rotate180Degree;

        /*
         *  Coordinate System for print card
         *  The physical printer has a twisted coordinate system
         *  
         * Y70 ┌──────────────────────┐
         *     │                      │
         *     │                      │
         *     │                      │
         *  Y0 └──────────────────────┘
         *     X0                  X960
         * 
        */

        public PrintDocument(bool rotate180Degree = true)
        {
            this._rotate180Degree = rotate180Degree;

            this._printElements = new List<PrintElementBase>();
        }

        public void AddElement(PrintElementBase printElement)
        {
            this._printElements.Add(printElement);
        }

        public async Task<byte[]> GetPrintDataAsync(CancellationToken cancellationToken = default)
        {
            var startX = 100;
            var endX = 1100;
            var startY = 0;
            var endY = 96;

            using var memoryStream = new MemoryStream();

            if (this._rotate180Degree)
            {
                //U (Rotates whole card (text and graphic) by 180°)
                var rotateWholeCardData = new byte[] { 0x1B, 0x55 };
                await memoryStream.WriteAsync(rotateWholeCardData, 0, rotateWholeCardData.Length, cancellationToken);
            }

            //Limit print area
            var limitPrintAreaCommandData = new byte[] { 0x1B, 0x25, 0x64 }; //%d
            await memoryStream.WriteAsync(limitPrintAreaCommandData, 0, limitPrintAreaCommandData.Length, cancellationToken);

            var limitPrintAreaPositionData = Encoding.ASCII.GetBytes($"{startX:D4}{endX:D4}");
            await memoryStream.WriteAsync(limitPrintAreaPositionData, 0, limitPrintAreaPositionData.Length, cancellationToken);

            //Set erase area mode
            var setEraseAreaModeCommandData = new byte[] { 0x1B, 0x4C, 0x31 }; //L1
            await memoryStream.WriteAsync(setEraseAreaModeCommandData, 0, setEraseAreaModeCommandData.Length, cancellationToken);

            var setEraseAreaModePositionData = Encoding.ASCII.GetBytes($"{startY:D2}{startX:D4}{endY:D2}{endX:D4}");
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
