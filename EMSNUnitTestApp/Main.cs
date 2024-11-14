using EMSNUnitTestApp.Tests;
using PS19.ATM.ReturnStatus;
using System.Diagnostics;
using System.Text;

namespace EMSNUnitTestApp
{
    [TestFixture]
    public class Main
    {
        [Test]
        [Ignore("Ignore a test")]
        public void RunBatScriptWithPsExec()
        {
            // Path to the bat script you want to run
            string batFilePath = @"psexec-script.bat";

            // Set up the process start information
            var processInfo = new ProcessStartInfo
            {
                FileName = $"{batFilePath}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = false
            };

            // Start the process
            using (var process = new Process { StartInfo = processInfo })
            {
                process.Start();

                // Capture output and errors if needed
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                process.WaitForExit();

                // Optionally, you can also verify the output or errors
                TestContext.WriteLine($"Output: {output}");
                TestContext.WriteLine($"Error: {error}");
            }
        }

        DeviceCommandsTests dcTests = new DeviceCommandsTests();

        //[SetUp]
        //public void Setup()
        //{
        //    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        //    try
        //    {
        //        SelfTest selfTest = new SelfTest();
        //        Status status = selfTest.PerformSelfTest();
        //        Assert.That(status.ErrorOccurred, Is.False, status.ReturnedMessage);
        //        if (status.ErrorOccurred) { return; }
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine(ex.ToString());
        //        TestContext.WriteLine("Error!\r\nDetails: " + ex.Message);
        //        return;
        //    }
        //}

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