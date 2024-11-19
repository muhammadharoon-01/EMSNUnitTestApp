using PS19.ATM.ReturnStatus;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Data.SQLite;
using System.Text;

namespace EMSNUnitTestApp
{
    [TestFixture]
    public class Main
    {
        private const string MapName = "TestCommunication";
        private const string MutexName = "Global\\TestCommunicationMutex";
        private const string ReadyEventName = "Global\\ClientReadyEvent";
        private const string TestCasesReadEventName = "Global\\TestCasesReadEvent";
        private const int BufferSize = 4096;

        private const string DatabaseFile = "JenkinsEMSTestDB.sqlite";
        string ConnectionString = "Data Source=JenkinsEMSTestDB.sqlite;Version=3;";

        [Test]
        //[Ignore("Ignore a test")]
        public void CreateDBAndTables()
        {
            // Step 1: Create the SQLite database file
            CreateDatabaseFile();

            // Step 2: Create the tables
            CreateTables();

            // Step3: Populate the test case names in database
            PopulateTestCases();

            // Step4: Run the EMS Application to execute the test cases
            RunBatScriptWithPsExec();
        }

        //[Test]
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

        private static void CreateDatabaseFile()
        {
            // Check if the file already exists
            if (!System.IO.File.Exists(DatabaseFile))
            {
                SQLiteConnection.CreateFile(DatabaseFile);
                Console.WriteLine($"Database file '{DatabaseFile}' created.");
            }
            else
            {
                Console.WriteLine($"Database file '{DatabaseFile}' already exists.");
            }
        }
        private static void CreateTables()
        {
            // Connection string to the database
            string connectionString = $"Data Source={DatabaseFile};Version=3;";

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                // SQL for creating the TestCases table
                string createTestCasesTable = @"
                CREATE TABLE IF NOT EXISTS TestCases (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Status TEXT DEFAULT 'Pending'
                );";

                // SQL for creating the StepResults table
                string createStepResultsTable = @"
                CREATE TABLE IF NOT EXISTS StepResults (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    TestCaseId INTEGER NOT NULL,
                    StepDescription TEXT NOT NULL,
                    Result TEXT NOT NULL,
                    FOREIGN KEY (TestCaseId) REFERENCES TestCases(Id)
                );";

                // Execute the table creation commands
                using (var command = new SQLiteCommand(createTestCasesTable, connection))
                {
                    command.ExecuteNonQuery();
                    Console.WriteLine("Table 'TestCases' created.");
                }

                using (var command = new SQLiteCommand(createStepResultsTable, connection))
                {
                    command.ExecuteNonQuery();
                    Console.WriteLine("Table 'StepResults' created.");
                }
            }
        }

        private void PopulateTestCases()
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                string[] testCaseNames = { "EMS116", "EMS117", "EMS118" };

                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = "INSERT INTO TestCases (Name) VALUES (@Name);";
                    foreach (var name in testCaseNames)
                    {
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@Name", name);
                        command.ExecuteNonQuery();
                    }
                }
            }

            Console.WriteLine("Test cases populated in the database.");
        }

        [Test]
        [Ignore("Ignore a test")]
        public void ServerReporting()
        {
            using (var mmf = MemoryMappedFile.CreateOrOpen(MapName, BufferSize))
            using (var mutex = new Mutex(false, MutexName))
            using (var readyEvent = new EventWaitHandle(false, EventResetMode.ManualReset, ReadyEventName))
            using (var testCasesReadEvent = new EventWaitHandle(false, EventResetMode.ManualReset, TestCasesReadEventName))
            {
                Console.WriteLine("Server started and waiting for client connection...");
                RunBatScriptWithPsExec();
                // Wait for the client to signal readiness
                readyEvent.WaitOne();
                Console.WriteLine("Client connected.");

                try
                {
                    // Step 1: Send the list of test cases to the client
                    string[] testCaseNames = { "EMS116" };
                    SendTestCaseNames(mmf, mutex, testCaseNames);

                    // Wait for the client to confirm reading the test cases
                    Console.WriteLine("Waiting for the client to acknowledge test cases...");
                    testCasesReadEvent.WaitOne();
                    Console.WriteLine("Client acknowledged test cases.");

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