using Microsoft.VisualStudio.TestTools.UnitTesting;
using Portalum.TrwPrinter.EasyPrinterS3.Models;
using Portalum.TrwPrinter.EasyPrinterS3.PrintElements;
using System.Threading.Tasks;

namespace Portalum.TrwPrinter.EasyPrinterS3.UnitTest
{
    [Ignore]
    [TestClass]
    public class PrinterClientTest
    {
        private IDeviceCommunication _deviceCommunication;
        private PrinterClient _printerClient;

        [TestInitialize]
        public async Task TestInitializeAsync()
        {
            this._deviceCommunication = new TcpNetworkDeviceCommunication("10.15.0.99", 50020);
            this._printerClient = new PrinterClient(this._deviceCommunication);
            var isConnected = await this._printerClient.ConnectAsync();
            Assert.IsTrue(isConnected, "Printer is not connected");
        }

        [TestCleanup]
        public async Task DisposeAsync()
        {
            await this._printerClient.DisconnectAsync();
            this._printerClient.Dispose();
            this._deviceCommunication.Dispose();
        }

        [TestMethod]
        public async Task SetDisplayTextAsyncTest()
        {
            var config = new DisplayTextConfig
            {
                Line1 = "Display Test Line1",
                Line2 = "Display Test Line2",
                Line3 = "Display Test Line3",
                Line4 = "Display Test Line4"
            };

            await this._printerClient.SetDisplayTextAsync(config);
        }

        [TestMethod]
        public async Task ResetDisplayTextAsyncTest()
        {
            await this._printerClient.ResetDisplayTextAsync();
        }

        [TestMethod]
        public async Task CheckCardFromFrontEntryAndEjectTest()
        {
            await this._printerClient.FeedCardFromFrontFeederAsync();
            await Task.Delay(5000);
            await this._printerClient.EjectCardAsync();
        }

        [TestMethod]
        public async Task ReadCardUidTest()
        {
            await this._printerClient.FeedCardFromFrontFeederAsync();
            await this._printerClient.ReadCardUidAsync();
            await this._printerClient.EjectCardAsync();
        }

        [TestMethod]
        public async Task ReadCardMifareUidTest()
        {
            await this._printerClient.FeedCardFromFrontFeederAsync();
            while (!this._printerClient.PrinterState.CardInPrintPosition)
            {
                continue;
            }

            await this._printerClient.ReadCardMifareUidAsync();
            await this._printerClient.EjectCardAsync();
        }

        [TestMethod]
        public async Task SendEraseAreaAsyncTest()
        {
            await this._printerClient.FeedCardFromHopperAsync();
            await this._printerClient.SendEraseAreaAsync();
            await this._printerClient.EjectCardAsync();
        }

        [TestMethod]
        public async Task SendPrintDemoCard1Async()
        {
            var printDocument = new PrintDocument();
            printDocument.AddElement(new TextPrintElement("Position X25", 25, 650));

            await this._printerClient.FeedCardFromHopperAsync();
            await this._printerClient.SendPrintDemoCardAsync(printDocument);
            await this._printerClient.EjectCardAsync();
        }

        [TestMethod]
        public async Task GetShortStateAsyncTest()
        {
            var printerBriefState = await this._printerClient.GetShortStateAsync();
            Assert.IsNotNull(printerBriefState);
        }
    }
}