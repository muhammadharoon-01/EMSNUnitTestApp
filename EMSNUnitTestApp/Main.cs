using PS19.ATM.ReturnStatus;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Text;

namespace EMSNUnitTestApp
{
    [TestFixture]
    public class Main
    {
        private const string MapName = "TestCommunication";
        private const string MutexName = "Global\\TestCommunicationMutex";
        private const int BufferSize = 4096;

        [Test]
        //[Ignore("Ignore a test")]
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

        [Test]
        public void ServerReporting()
        {
            using (var mmf = MemoryMappedFile.CreateOrOpen(MapName, BufferSize))
            using (var mutex = new Mutex(false, MutexName))
            {
                Console.WriteLine("Server started and waiting for client response...");

                try
                {
                    // Step 1: Send the list of test cases to the client
                    string[] testCaseNames = { "EMS116", "EMS117" };
                    SendTestCaseNames(mmf, mutex, testCaseNames);

                    // Step 2: Continuously receive results from the client
                    ReceiveStepResults(mmf, mutex);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Server error: {ex.Message}");
                }
            }
        }

        private void SendTestCaseNames(MemoryMappedFile mmf, Mutex mutex, string[] testCaseNames)
        {
            mutex.WaitOne();
            try
            {
                using (var accessor = mmf.CreateViewAccessor())
                {
                    // Convert the test case list to a single string
                    string testCaseList = string.Join(",", testCaseNames);

                    // Write the data length and data
                    byte[] data = Encoding.UTF8.GetBytes(testCaseList);
                    accessor.Write(0, data.Length);
                    accessor.WriteArray(4, data, 0, data.Length);

                    Console.WriteLine("Test case list sent to client.");
                }
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }

        private void ReceiveStepResults(MemoryMappedFile mmf, Mutex mutex)
        {
            while (true)
            {
                mutex.WaitOne();
                try
                {
                    using (var accessor = mmf.CreateViewAccessor())
                    {
                        // Read the data length
                        int length = accessor.ReadInt32(0);
                        if (length > 0)
                        {
                            // Read the step result
                            byte[] buffer = new byte[length];
                            accessor.ReadArray(4, buffer, 0, length);
                            string stepResult = Encoding.UTF8.GetString(buffer);

                            // Log the step result
                            Console.WriteLine($"Step Result: {stepResult}");

                            // Clear the shared memory
                            accessor.Write(0, 0);

                            // Break if the client indicates test completion
                            if (stepResult == "All Tests Completed")
                            {
                                Console.WriteLine("All test results received. Exiting server...");
                                break;
                            }
                        }
                    }
                }
                finally
                {
                    mutex.ReleaseMutex();
                }

                Thread.Sleep(500); // Polling interval
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
        [Ignore("Ignore a test")]
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