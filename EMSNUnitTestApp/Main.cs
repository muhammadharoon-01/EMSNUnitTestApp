using EMSNUnitTestApp.Tests;
using PS19.ATM.ReturnStatus;
using System.Diagnostics;
using System.Text;

namespace EMSNUnitTestApp
{
    [TestFixture]
    public class Main
    {
        DeviceCommandsTests dcTests = new DeviceCommandsTests();

        [SetUp]
        public void Setup()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            try
            {
                SelfTest selfTest = new SelfTest();
                Status status = selfTest.PerformSelfTest();
                Assert.That(status.ErrorOccurred, Is.False, status.ReturnedMessage);
                if (status.ErrorOccurred) { return; }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                TestContext.WriteLine("Error!\r\nDetails: " + ex.Message);
                return;
            }
        }

        [Test]
        [Repeat(5)]
        public void Test()
        {
            try
            {
                Status status = dcTests.EMS163();
                Assert.That(status.ErrorOccurred, Is.False, status.ReturnedMessage);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                TestContext.WriteLine("Error!\r\nDetails: " + ex.Message);
                return;
            }
        }
    }
}