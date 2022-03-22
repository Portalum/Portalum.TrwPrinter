namespace Portalum.TrwPrinter.EasyPrinterS3.Models
{
    public class PrinterState
    {
        public bool CardSensorPrintPosition { get; set; }
        public bool CardSensorFront { get; set; }
        /// <summary>
        /// Not longer in use
        /// </summary>
        public bool CardSensorBack { get; set; }
        public bool CardInPrintPosition { get; set; }
        public bool PrintingInProgress { get; set; }
        public bool CardHasBeenPrinted { get; set; }
        public bool CardInPeripherialUnitPosition { get; set; }
        public bool Error { get; set; }

        public PrinterState() { }

        public PrinterState(ByteBitInfo byteBitInfo)
        {
            this.CardSensorPrintPosition = byteBitInfo.Bit0;
            this.CardSensorFront = byteBitInfo.Bit1;
            this.CardSensorBack = byteBitInfo.Bit2;
            this.CardInPrintPosition = byteBitInfo.Bit3;
            this.PrintingInProgress = byteBitInfo.Bit4;
            this.CardHasBeenPrinted = byteBitInfo.Bit5;
            this.CardInPeripherialUnitPosition = byteBitInfo.Bit6;
            this.Error = byteBitInfo.Bit7;
        }
    }
}
