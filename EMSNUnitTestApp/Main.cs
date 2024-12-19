using System.Data.SQLite;
using System.Diagnostics;
using Status = PS19.ATM.ReturnStatus.Status;

namespace EMSNUnitTestApp
{
    [TestFixture]
    public class Main
    {
        private const string DatabaseFile = "JenkinsEMSTestDB.sqlite";
        string ConnectionString = "Data Source=JenkinsEMSTestDB.sqlite;Version=3;";

        [Test]
        public void TestCases()
        {
            // Step 1: Create the SQLite database file
            CreateDatabaseFile();

            // Step 2: Create the tables
            CreateTables();

            // Step3: Populate the test case names in database
            PopulateTestCases();
            Thread.Sleep(10000);

            // Step4: Run the EMS Application to execute the test cases
            RunBatScriptWithPsExec();
            Thread.Sleep(5000);
            // Step5: Poll Database after every 5 seconds to get updated step results.
            ServerReporting();
        }
        #region Sequence/Common Methods

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
        private static Status CreateDatabaseFile()
        {
            Status status;
            try
            {
                // Check if the file already exists
                if (!System.IO.File.Exists(DatabaseFile))
                {
                    SQLiteConnection.CreateFile(DatabaseFile);
                    TestContext.WriteLine($"Database file '{DatabaseFile}' created.");
                    status = new()
                    {
                        ErrorOccurred = false,
                        ReturnedMessage = $"Database file '{DatabaseFile}' created.",
                        ReturnedValue = 0
                    };
                }
                else
                {
                    TestContext.WriteLine($"Database file '{DatabaseFile}' already exists.");
                    Console.WriteLine($"Database file '{DatabaseFile}' already exists.");
                    status = new()
                    {
                        ErrorOccurred = true,
                        ReturnedMessage = $"Database file '{DatabaseFile}' already exists.",
                        ReturnedValue = -1
                    };
                }
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"{ex.Message}");
                status = new()
                {
                    ErrorOccurred = true,
                    ReturnedMessage = $"{ex.Message}",
                    ReturnedValue = -1
                };

            }
            return status;
        }
        private static Status CreateTables()
        {
            Status status;
            try
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
                    TestCaseId TEXT NOT NULL,
                    StepDescription TEXT NOT NULL,
                    Result TEXT NOT NULL,
                    FOREIGN KEY (TestCaseId) REFERENCES TestCases(Id)
                );";

                    // Execute the table creation commands
                    using (var command = new SQLiteCommand(createTestCasesTable, connection))
                    {
                        command.ExecuteNonQuery();
                        TestContext.WriteLine("Table 'TestCases' created.");
                        status = new()
                        {
                            ErrorOccurred = false,
                            ReturnedMessage = $"Table 'TestCases' created.",
                            ReturnedValue = 0
                        };
                    }

                    using (var command = new SQLiteCommand(createStepResultsTable, connection))
                    {
                        command.ExecuteNonQuery();
                        TestContext.WriteLine("Table 'StepResults' created.");
                        status = new()
                        {
                            ErrorOccurred = false,
                            ReturnedMessage = $"Table 'StepResults' created.",
                            ReturnedValue = 0
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"{ex.Message}");
                status = new()
                {
                    ErrorOccurred = true,
                    ReturnedMessage = $"{ex.Message}",
                    ReturnedValue = -1
                };
            }
            return status;
        }
        private Status PopulateTestCases()
        {
            Status status;
            try
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
                    connection.Close();
                    connection.Dispose();
                }

                TestContext.WriteLine("Test cases populated in the database.");
                status = new()
                {
                    ErrorOccurred = false,
                    ReturnedMessage = $"Test cases populated in the database.",
                    ReturnedValue = 0
                };
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"{ex.Message}");
                status = new()
                {
                    ErrorOccurred = true,
                    ReturnedMessage = $"{ex.Message}",
                    ReturnedValue = -1
                };
            }
            return status;
        }
        public Status ServerReporting()
        {
            Status status = new();
            try
            {
                //Continuously receive results from the client
                status = PollStepResults();
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"Server error: {ex.Message}");
                status = new()
                {
                    ErrorOccurred = true,
                    ReturnedMessage = $"Server error: {ex.Message}",
                    ReturnedValue = -1
                };
            }
            return status;
        }
        private Status PollStepResults()
        {
            Status status = new();
            try
            {
                string connectionString = $"Data Source={DatabaseFile};Version=3;";

                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    while (true)
                    {
                        // Set the journal mode to WAL
                        using (var Modecommand = new SQLiteCommand("PRAGMA journal_mode=WAL;", connection))
                        {
                            Modecommand.ExecuteNonQuery();

                            using (var command = new SQLiteCommand("SELECT * FROM StepResults", connection))
                            {
                                using (var reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        int id = reader.GetInt32(0);
                                        string testCaseId = reader.GetString(1);
                                        string stepDescription = reader.GetString(2);
                                        string result = reader.GetString(3);

                                        bool isError = false;
                                        if (result.Contains("Pass"))
                                            isError = false;
                                        else
                                            isError = true;

                                        Assert.That(
                                            isError, 
                                            Is.False,
                                            $"Step Result: TestCaseId={testCaseId}, Step='{stepDescription}', Result='{result}'");

                                        TestContext.WriteLine($"Step Result: TestCaseId={testCaseId}, Step='{stepDescription}', Result='{result}'");

                                        status = new()
                                        {
                                            ErrorOccurred = isError,
                                            ReturnedMessage = $"Step Result: TestCaseId={testCaseId}, Step='{stepDescription}', Result='{result}'",
                                        };

                                        // If all steps are completed, break the loop
                                        if (stepDescription == "Test Completed")
                                        {
                                            TestContext.WriteLine("All steps completed. Ending polling.");
                                            status = new()
                                            {
                                                ErrorOccurred = isError,
                                                ReturnedMessage = "All steps completed. End polling.",
                                            };
                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        Thread.Sleep(2000); // Poll every second
                    }
                }
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"{ex.Message}");
                status = new()
                {
                    ErrorOccurred = true,
                    ReturnedMessage = $"{ex.Message}",
                    ReturnedValue = -1
                };
            }
            return status;
        }
        #endregion

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

        //[Test]
        //[Ignore("Ignore a test")]
        //[Repeat(5)]
        //public void Test()
        //{
        //    try
        //    {
        //        Status status = dcTests.EMS163();
        //        Assert.That(status.ErrorOccurred, Is.False, status.ReturnedMessage);
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine(ex.ToString());
        //        TestContext.WriteLine("Error!\r\nDetails: " + ex.Message);
        //        return;
        //    }
        //}
    }
}