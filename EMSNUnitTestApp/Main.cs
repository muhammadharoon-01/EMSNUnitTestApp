using EMSNUnitTestApp.Tests;
using PS19.ATM.ReturnStatus;
using System.Text;

namespace EMSNUnitTestApp
{
    public class Main
    {
        DeviceCommandsTests dcTests = new DeviceCommandsTests();

        [SetUp]
        public void Setup()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            SelfTest selfTest = new SelfTest();
            Status status = selfTest.PerformSelfTest();
            Assert.That(status.ErrorOccurred, Is.False, status.ReturnedMessage);
        }

        [Test]
        [Repeat(5)]
        public void Test()
        {
            Status status = dcTests.EMS163();
            Assert.That(status.ErrorOccurred, Is.False, status.ReturnedMessage);
        }
    }
}