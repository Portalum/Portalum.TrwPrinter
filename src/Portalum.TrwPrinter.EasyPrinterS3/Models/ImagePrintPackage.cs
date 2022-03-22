namespace Portalum.TrwPrinter.EasyPrinterS3.Models
{
    public class ImagePrintPackage
    {
        public int BytesPerRow { get; set; }
        public int Rows { get; set; }
        public byte[] PrintData { get; set; }
    }
}
