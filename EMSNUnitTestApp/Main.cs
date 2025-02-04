using Status = Allure.Net.Commons;
using Allure.NUnit.Attributes;
using System.Data.SQLite;
using System.Diagnostics;
using Assert = NUnit.Framework.Assert;
using Allure.Net.Commons;

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

            for (int i = 0; i < 3; i++)
            {
                if (!DoesTableExist("TestCases"))
                {
                    //throw new Exception("Table 'TestCases' does not exist after creation.");
                    Console.WriteLine("Table 'TestCases' does not exist after creation.");
                    Thread.Sleep(1000);
                }
                else
                    break;
            }
            // Step 3: Populate the test case names in the database
            status = PollStepResults();
            Assert.That(status.ErrorOccurred, Is.False, status.ReturnedMessage);

            Thread.Sleep(10000); // Adjust as needed for timing

            // Step 4: Run the EMS Application to execute the test cases
            RunBatScriptWithPsExec();

            // Step 5: Poll the database to get updated step results
            //status = PollStepResults();
            Assert.That(status.ErrorOccurred, Is.False, status.ReturnedMessage);
        }
        private bool DoesTableExist(string tableName)
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                string query = $"SELECT name FROM sqlite_master WHERE type='table' AND name='{tableName}';";
                using (var command = new SQLiteCommand(query, connection))
                {
                    var result = command.ExecuteScalar();
                    return result != null;
                }
            }
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

        [Test]
        [Ignore("Ignore a test")]
        [AllureFeature("Polling Steps Report")]
        public void Test_PollStepResults()
        {
            var status = PollStepResults();
            //Assert.IsFalse(status.ErrorOccurred, $"Test failed: {status.ReturnedMessage}");
        }

        [AllureStep("Polling database for step results")]
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
                                    string testCaseId = reader["TestCaseId"].ToString();
                                    string stepDescription = reader["StepDescription"].ToString();
                                    string result = reader["Result"].ToString();

                                    if (result.Contains("Fail", StringComparison.OrdinalIgnoreCase))
                                    {
                                        string failureMessage = $"Test Case {testCaseId} failed at step: {stepDescription}";

                                        AllureLifecycle.Instance.UpdateTestCase(tc =>
                                            tc.steps.Add(new StepResult
                                            {
                                                name = stepDescription,
                                                status = Allure.Net.Commons.Status.failed,
                                                statusDetails = new StatusDetails { message = failureMessage }
                                            }));

                                        TestContext.Progress.WriteLine(failureMessage);
                                        Assert.Fail(failureMessage);
                                    }
                                    else
                                    {
                                        AllureLifecycle.Instance.UpdateTestCase(tc =>
                                            tc.steps.Add(new StepResult
                                            {
                                                name = stepDescription,
                                                status = Allure.Net.Commons.Status.passed
                                            }));

                                        TestContext.Progress.WriteLine($"Step Passed: {stepDescription}");
                                    }

                                    if (stepDescription.Equals("Test Completed", StringComparison.OrdinalIgnoreCase))
                                    {
                                        TestContext.Progress.WriteLine("All steps completed. Ending polling.");
                                        return new Status { ErrorOccurred = false, ReturnedMessage = "All steps completed." };
                                    }
                                }
                            }
                            Thread.Sleep(5000);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AllureLifecycle.Instance.UpdateTestCase(tc => tc.status = Allure.Net.Commons.Status.broken);
                return new Status { ErrorOccurred = true, ReturnedMessage = ex.Message };
            }
        }

        [AllureStep("Capture failure screenshot")]
        private string CaptureScreenshot()
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "screenshot.png");

            // Simulate a screenshot capture (Replace this with real screenshot logic)
            File.WriteAllBytes(filePath, new byte[0]); // Dummy file for testing

            return filePath;
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

        #region Commented MHA
        //private Status PopulateTestCases()
        //{
        //    try
        //    {
        //        using (var connection = new SQLiteConnection(ConnectionString))
        //        {
        //            connection.Open();

        //            string insertTestCase = "INSERT INTO TestCases (Name) VALUES (@Name);";

        //            using (var command = new SQLiteCommand(insertTestCase, connection))
        //            {
        //                command.Parameters.AddWithValue("@Name", "EMS116");
        //                command.ExecuteNonQuery();
        //            }

        //            return new Status { ErrorOccurred = false, ReturnedMessage = "Test cases populated in the database." };
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return new Status { ErrorOccurred = true, ReturnedMessage = ex.Message };
        //    }
        //}
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