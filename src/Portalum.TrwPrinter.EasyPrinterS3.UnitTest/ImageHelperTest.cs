using Microsoft.VisualStudio.TestTools.UnitTesting;
using Portalum.TrwPrinter.EasyPrinterS3.Helpers;
using System.IO;
using System.Threading.Tasks;

namespace Portalum.TrwPrinter.EasyPrinterS3.UnitTest
{
    [TestClass]
    public class ImageHelperTest
    {
        [TestMethod]
        public async Task SetDisplayTextAsyncTest()
        {
            var imageData = File.ReadAllBytes(@"../../../../../doc/2bit307x326.png");
            var test = ImageHelper.GetImagePrintPackage(imageData, false);
        }
    }
}