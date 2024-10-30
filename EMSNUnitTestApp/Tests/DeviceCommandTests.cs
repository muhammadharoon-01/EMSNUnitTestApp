using EMSASE2000AutomateAPI;
using EMSNUnitTestApp.Commons;
using PS19.ATM.ReturnStatus;
using static EMSASE2000AutomateAPI.ASE2000API;

namespace EMSNUnitTestApp
{
    public class DeviceCommandsTests
    {
        public Status EMS163()
        {
            Status status = new Status(true, string.Empty, 0);
            TestContext.WriteLine("Step 1: Open SmartInterface Online Mode");
            status = CommonModel.EMSCommonsV104.OpenSmartInterface();
            if (status.ErrorOccurred)
            {
                TestContext.WriteLine("Error!\r\nDetails: " + status.ReturnedMessage);
                return status;
            }
            Thread.Sleep(1000);

            TestContext.WriteLine("Step 2: Verify on SmartInterface that all 3 devices are in bypass mode.");
            status = CommonModel.EMSCommonsV104.VerifyCircuitBypassMode();
            Thread.Sleep(1000);
            if (status.ErrorOccurred)
            {
                status = CommonModel.EMSCommonsV104.SendCircuitToMonitoringMode();
                if (status.ErrorOccurred)
                {
                    TestContext.WriteLine("Error!\r\nDetails: " + status.ReturnedMessage);
                    return status;
                }
                Thread.Sleep(20000);
            }

            TestContext.WriteLine("Step 3: Issue a read command on ASE 2000 and verify 'ModeStatus' = 4 on DNP3 Emulator.");
            if (CommonModel.EMSCommonsV104.ase2000Obj.currentEMSAPI == EMSAPIType.DNP3API)
            {
                status = DNP3EmulatorCommons.IssueReadCommandOnDNP3ClientAndVerifyParameterValue("ModeStatus", true, "4", ASE2000API.EasyConnectDNP3NodeType.Circuit1, true);
                if (status.ErrorOccurred)
                {
                    TestContext.WriteLine("Error!\r\nDetails: "+status.ReturnedMessage);
                    return status;
                }
            }
            else if (CommonModel.EMSCommonsV104.ase2000Obj.currentEMSAPI == EMSAPIType.IEC61850)
            {
                status = DNP3EmulatorCommons.IssueReadCommandOnASE61850ClientAndVerifyParameterValue("ModeStatus", true, true, "4", ASE2000API.EasyConnectIEC61850NodeType.Circuit1);
                if (status.ErrorOccurred)
                {
                    TestContext.WriteLine("Error!\r\nDetails: " + status.ReturnedMessage);
                    return status;
                }
            }

            TestContext.WriteLine("Step 4: Send the 'VoltageInjectCmd' point value as 100 from DNP3 Emulator.");
            if (CommonModel.EMSCommonsV104.ase2000Obj.currentEMSAPI == EMSAPIType.DNP3API)
            {
                status = DNP3EmulatorCommons.SendCommandOnDNP3Client("VoltageInjectCmd", true, "100", ASE2000API.EasyConnectDNP3NodeType.Circuit1, false, 5000);
                if (status.ErrorOccurred)
                {
                    TestContext.WriteLine("Error!\r\nDetails: " + status.ReturnedMessage);
                    return status;
                }
            }
            else if (CommonModel.EMSCommonsV104.ase2000Obj.currentEMSAPI == EMSAPIType.IEC61850)
            {
                status = DNP3EmulatorCommons.SendCommandOnASE61850Client("VoltageInjectCmd", true, "100", ASE2000API.EasyConnectIEC61850NodeType.Circuit1, false, 5000);
                if (status.ErrorOccurred)
                {
                    TestContext.WriteLine("Error!\r\nDetails: " + status.ReturnedMessage);
                    return status;
                }
            }

            TestContext.WriteLine("Step 5: Verify on SmartInterface that all devices switch to 100 percent voltage inductive injection mode.");
            status = CommonModel.EMSCommonsV104.VerifyCircuitInInductiveInjection();
            if (status.ErrorOccurred)
            {
                TestContext.WriteLine("Error!\r\nDetails: " + status.ReturnedMessage);
                return status;
            }
            Thread.Sleep(1000);

            TestContext.WriteLine("Step 6: Issue a read command on ASE 2000 and verify 'ModeStatus' = 0 on DNP3 Emulator.");
            if (CommonModel.EMSCommonsV104.ase2000Obj.currentEMSAPI == EMSAPIType.DNP3API)
            {
                status = DNP3EmulatorCommons.IssueReadCommandOnDNP3ClientAndVerifyParameterValue("ModeStatus", true, "0", ASE2000API.EasyConnectDNP3NodeType.Circuit1, true);
                if (status.ErrorOccurred)
                {
                    TestContext.WriteLine("Error!\r\nDetails: " + status.ReturnedMessage);
                    return status;
                }
            }
            else if (CommonModel.EMSCommonsV104.ase2000Obj.currentEMSAPI == EMSAPIType.IEC61850)
            {
                status = DNP3EmulatorCommons.IssueReadCommandOnASE61850ClientAndVerifyParameterValue("ModeStatus", true, true, "0", ASE2000API.EasyConnectIEC61850NodeType.Circuit1, 0, true);
                if (status.ErrorOccurred)
                {
                    TestContext.WriteLine("Error!\r\nDetails: " + status.ReturnedMessage);
                    return status;
                }
            }
            Thread.Sleep(1000);

            //Revert Settings
            TestContext.WriteLine("Reverting system to normal state.");
            if (CommonModel.EMSCommonsV104.ase2000Obj.currentEMSAPI == EMSAPIType.DNP3API)
            {
                status = DNP3EmulatorCommons.SendCommandOnDNP3Client("BinaryBypassCmd", false, "1", ASE2000API.EasyConnectDNP3NodeType.Circuit1, default, default);
                if (status.ErrorOccurred)
                {
                    TestContext.WriteLine("Error!\r\nDetails: " + status.ReturnedMessage);
                    return status;
                }
            }
            else if (CommonModel.EMSCommonsV104.ase2000Obj.currentEMSAPI == EMSAPIType.IEC61850)
            {
                status = DNP3EmulatorCommons.SendCommandOnASE61850Client("BinaryBypassCmd", true, "1", ASE2000API.EasyConnectIEC61850NodeType.Circuit1, default, default);
                if (status.ErrorOccurred)
                {
                    TestContext.WriteLine("Error!\r\nDetails: " + status.ReturnedMessage);
                    return status;
                }
            }

            return status;
        }
    }
}
