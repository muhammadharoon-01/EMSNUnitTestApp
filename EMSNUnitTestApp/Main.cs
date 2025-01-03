using System.Data.SQLite;
using System.Diagnostics;
using Assert = NUnit.Framework.Assert;
using Status = PS19.ATM.ReturnStatus.Status;

namespace EMSNUnitTestApp
{
    [TestFixture]
    public class Main
    {
        private const string DatabaseFile = "JenkinsEMSTestDB.sqlite";
        private const string ConnectionString = "Data Source=JenkinsEMSTestDB.sqlite;Version=3;";

        [Test]
        public void TestCases()
        {
            Status status;

            // Step 1: Create the SQLite database file
            status = CreateDatabaseFile();
            Assert.That(status.ErrorOccurred, Is.False, status.ReturnedMessage);


            // Step 2: Create the tables
            status = CreateTables();
            Assert.That(status.ErrorOccurred, Is.False, status.ReturnedMessage);

            // Step 3: Populate the test case names in the database
            status = PopulateTestCases();
            Assert.That(status.ErrorOccurred, Is.False, status.ReturnedMessage);

            Thread.Sleep(5000); // Adjust as needed for timing

            // Step 4: Run the EMS Application to execute the test cases
            RunBatScriptWithPsExec();

            // Step 5: Poll the database to get updated step results
            status = PollStepResults();
            Assert.That(status.ErrorOccurred, Is.False, status.ReturnedMessage);
        }

        private void RunBatScriptWithPsExec()
        {
            string batFilePath = "psexec-script.bat";

            var processInfo = new ProcessStartInfo
            {
                FileName = batFilePath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var process = new Process { StartInfo = processInfo })
            {
                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                process.WaitForExit();

                TestContext.WriteLine($"Output: {output}");
                TestContext.WriteLine($"Error: {error}");

                //Assert.AreEqual(0, process.ExitCode, "Script execution failed.");
            }
        }

        private Status CreateDatabaseFile()
        {
            try
            {
                if (!System.IO.File.Exists(DatabaseFile))
                {
                    SQLiteConnection.CreateFile(DatabaseFile);
                    return new Status { ErrorOccurred = false, ReturnedMessage = "Database file created." };
                }
                return new Status { ErrorOccurred = false, ReturnedMessage = "Database file already exists." };
            }
            catch (Exception ex)
            {
                return new Status { ErrorOccurred = true, ReturnedMessage = ex.Message };
            }
        }

        private Status CreateTables()
        {
            try
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();

                    string createTestCasesTable = @"
                    CREATE TABLE IF NOT EXISTS TestCases (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL,
                        Status TEXT DEFAULT 'Pending'
                    );";

                    string createStepResultsTable = @"
                    CREATE TABLE IF NOT EXISTS StepResults (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        TestCaseId INTEGER NOT NULL,
                        StepDescription TEXT NOT NULL,
                        Result TEXT NOT NULL,
                        FOREIGN KEY (TestCaseId) REFERENCES TestCases(Id)
                    );";

                    ExecuteNonQuery(connection, createTestCasesTable);
                    ExecuteNonQuery(connection, createStepResultsTable);

                    return new Status { ErrorOccurred = false, ReturnedMessage = "Tables created successfully." };
                }
            }
            catch (Exception ex)
            {
                return new Status { ErrorOccurred = true, ReturnedMessage = ex.Message };
            }
        }

        private Status PopulateTestCases()
        {
            try
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();

                    string insertTestCase = "INSERT INTO TestCases (Name) VALUES (@Name);";

                    using (var command = new SQLiteCommand(insertTestCase, connection))
                    {
                        command.Parameters.AddWithValue("@Name", "EMS116");
                        command.ExecuteNonQuery();
                    }

                    return new Status { ErrorOccurred = false, ReturnedMessage = "Test cases populated in the database." };
                }
            }
            catch (Exception ex)
            {
                return new Status { ErrorOccurred = true, ReturnedMessage = ex.Message };
            }
        }

        private Status PollStepResults()
        {
            try
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();

                    using (var command = new SQLiteCommand("SELECT * FROM StepResults", connection))
                    {
                        while (true)
                        {
                            using (var reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    int id = reader.GetInt32(0);
                                    string testCaseId = reader["TestCaseId"].ToString();
                                    string stepDescription = reader["StepDescription"].ToString();
                                    string result = reader["Result"].ToString();

                                    Assert.That(result.Contains("Fail"), Is.False, $"Test failed: {stepDescription}");

                                    if (stepDescription.Equals("Test Completed", StringComparison.OrdinalIgnoreCase))
                                    {
                                        TestContext.WriteLine("All steps completed. Ending polling.");
                                        return new Status { ErrorOccurred = false, ReturnedMessage = "All steps completed." };
                                    }
                                }
                            }
                            Thread.Sleep(5000); // Adjust polling interval as needed
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new Status { ErrorOccurred = true, ReturnedMessage = ex.Message };
            }
        }
        

        private void ExecuteNonQuery(SQLiteConnection connection, string query)
        {
            using (var command = new SQLiteCommand(query, connection))
            {
                command.ExecuteNonQuery();
            }
        }

        private class Status
        {
            public bool ErrorOccurred { get; set; }
            public string ReturnedMessage { get; set; }
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