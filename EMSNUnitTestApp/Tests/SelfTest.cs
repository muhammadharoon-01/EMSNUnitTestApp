using EMSASE2000AutomateAPI;
using EMSNUnitTestApp.Commons;
using PS19.ATM.ReturnStatus;
using System.Diagnostics;

namespace EMSNUnitTestApp.Tests
{
    public class SelfTest
    {
        #region fields
        static int dnp3MonitorScreen = 2;
        static int dnp3XMousePosition = 3100;
        static int dnp3YMousePosition = 630;
        #endregion

        #region Methods
        public Status PerformSelfTest()
        {
            Status status = new Status();
            try
            {
                status = InitializeSensorSimulatorAndGPIO();
                if (status.ErrorOccurred){ return status; }
                Thread.Sleep(1000);

                status = DNP3EmulatorConfiguration();
                if (status.ErrorOccurred) { return status; }
                Thread.Sleep(1000);

                status = DNP3EmulatorSelfTest();
                if (status.ErrorOccurred) { return status; }
                Thread.Sleep(1000);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                return status;
            }
            return status;
        }

        public Status DNP3EmulatorConfiguration()
        {
            Status status = new Status();
            try
            {
                TestContext.WriteLine("Configure DNP3 Emulator.");

                CommonModel.EMSCommonsV104.ase2000Obj = new ASE2000API();
                CommonModel.EMSCommonsV104.ase2000Obj.gatewayIP = "172.19.17.91";
                CommonModel.EMSCommonsV104.ase2000Obj.redundantGatewayIP = "172.19.17.31";
                CommonModel.EMSCommonsV104.ase2000Obj.subClient = "V104";

                ASE2000API.currentGatewayIP = CommonModel.EMSCommonsV104.ase2000Obj.gatewayIP;
                CommonModel.EMSCommonsV104.ase2000Obj.IEC61850GatewayIP = CommonModel.EMSCommonsV104.ase2000Obj.gatewayIP;
                CommonModel.EMSCommonsV104.ase2000Obj.DNP3GatewayIP = CommonModel.EMSCommonsV104.ase2000Obj.gatewayIP;

                //Set Screen for DNP3 Emulator.
                status = CommonModel.EMSCommonsV104.ase2000Obj.SetMonitorScreen(dnp3MonitorScreen, dnp3XMousePosition, dnp3YMousePosition);
                if (status.ErrorOccurred)
                {
                    TestContext.WriteLine("Error!\r\nDetails: " + status.ReturnedMessage);
                    return status;
                }

                status = CommonModel.EMSCommonsV104.ase2000Obj.SetCurrentConfiguration(3);
                if (status.ErrorOccurred)
                {
                    TestContext.WriteLine("Error!\r\nDetails: " + status.ReturnedMessage);
                    return status;
                }

                status = CommonModel.EMSCommonsV104.ase2000Obj.LoadProfileAndMappingData();
                if (status.ErrorOccurred)
                {
                    TestContext.WriteLine("Error!\r\nDetails: " + status.ReturnedMessage);
                    return status;
                }
            }
            catch (Exception ex)
            {
                TestContext.WriteLine("Error!\r\nDetails: " + ex.Message);
                Debug.WriteLine(ex.ToString());
                return status;
            }
            return status;
        }

        public Status DNP3EmulatorSelfTest()
        {
            Status status = new Status();
            try
            {
                TestContext.WriteLine("Open DNP3 Emulator.");
                //Start DNP3 Console App.
                status = CommonModel.EMSCommonsV104.ase2000Obj.StartDNP3EmulatroApp();
                if (status.ErrorOccurred)
                {
                    TestContext.WriteLine("Error!\r\nDetails: " + status.ReturnedMessage);
                    return status;
                }
                Thread.Sleep(1000);

                TestContext.WriteLine("Disable Temporary Lockout on circuit if exist.");
                //Reset temporary lockout.
                status = CommonModel.EMSCommonsV104.ase2000Obj.SendDigitalEMSCommandViaDNP3Api("TempLockoutResetCmd", 0, ASE2000API.EasyConnectDNP3NodeType.Circuit1);
                if (status.ErrorOccurred)
                {
                    TestContext.WriteLine("Error!\r\nDetails: " + status.ReturnedMessage);
                    return status;
                }

                Thread.Sleep(3000);

                TestContext.WriteLine("Check if circuit is monitoring mode.");
                //Send Circuit to monitoring mode.
                status = CommonModel.EMSCommonsV104.ase2000Obj.GetAnalogEMSTelemetryDataViaDNP3Api("ModeStatus", ASE2000API.EasyConnectDNP3NodeType.Circuit1);
                if (status.ErrorOccurred)
                {
                    if (status.ErrorOccurred && status.ReturnedMessage != "4.00")
                    {
                        TestContext.WriteLine("Send circuit to monitoring mode.");
                        status = CommonModel.EMSCommonsV104.ase2000Obj.SendDigitalEMSCommandViaDNP3Api("BinaryBypassCmd", 0, ASE2000API.EasyConnectDNP3NodeType.Circuit1);
                        if (status.ErrorOccurred)
                        {
                            TestContext.WriteLine("Error!\r\nDetails: " + status.ReturnedMessage);
                            return status;
                        }
                    }
                    TestContext.WriteLine("Error!\r\nDetails: " + status.ReturnedMessage);
                    return status;
                }
                Thread.Sleep(3000);

                TestContext.WriteLine("Verify Device Control Switch is set to 'SI&EMS/DeviceControlCoperative'.");
                //Set Device Control Switch SI&EMS.
                status = CommonModel.EMSCommonsV104.ase2000Obj.GetDigitalEMSTelemetryDataViaDNP3Api("BinaryDeviceControlCoperative", ASE2000API.EasyConnectDNP3NodeType.Circuit1);
                if (status.ErrorOccurred)
                {
                    TestContext.WriteLine("Error!\r\nDetails: " + status.ReturnedMessage);
                    return status;
                }
                Thread.Sleep(1000);
            }
            catch (Exception ex)
            {
                TestContext.WriteLine("Error!\r\nDetails: " + status.ReturnedMessage);
                Debug.WriteLine(ex.ToString());
                return status;
            }
            return status;
        }

        public Status InitializeSensorSimulatorAndGPIO()
        {
            Status status = new Status();
            try
            {
                TestContext.WriteLine("Initialize DAQ Cards.");

                CommonModel.EMSCommonsV104.ase2000Obj = new ASE2000API();
                CommonModel.EMSCommonsV104.ase2000Obj.subClient = "V104";

                status = CommonModel.EMSCommonsV104.InitializeGPIO();
                if (status.ErrorOccurred)
                {
                    TestContext.WriteLine("Error!\r\nDetails: " + status.ReturnedMessage);
                    return status;
                }
                Thread.Sleep(1000);

                status = CommonModel.EMSCommonsV104.InitializeSensorSimulator("iSeries", CommonModel.EMSCommonsV104.ase2000Obj.subClient);
                if (status.ErrorOccurred)
                {
                    TestContext.WriteLine("Error!\r\nDetails: " + status.ReturnedMessage);
                    return status;
                }
                Thread.Sleep(1000);

                TestContext.WriteLine("Apply default sensor values on SWFD's of phase 2.");
                //Apply Line Current at Phase 2
                status = CommonModel.EMSCommonsV104.ApplyLineCurrentSB(500);
                if (status.ErrorOccurred)
                {
                    TestContext.WriteLine("Error!\r\nDetails: " + status.ReturnedMessage);
                    return status;
                }
                Thread.Sleep(100);

                //Apply VSL Voltage at Phase 2
                status = CommonModel.EMSCommonsV104.ApplyVSLVolatgeSB(500);
                if (status.ErrorOccurred)
                {
                    TestContext.WriteLine("Error!\r\nDetails: " + status.ReturnedMessage);
                    return status;
                }
                Thread.Sleep(100);

                //Apply SV Line Current at Phase 2
                status = CommonModel.EMSCommonsV104.ApplyLineCurrentOnSV(500);
                if (status.ErrorOccurred)
                {
                    TestContext.WriteLine("Error!\r\nDetails: " + status.ReturnedMessage);
                    return status;
                }
                Thread.Sleep(100);

                //Apply DC Link Volatage at Phase 2
                status = CommonModel.EMSCommonsV104.ApplyDCLink1OnSV(220);
                if (status.ErrorOccurred)
                {
                    TestContext.WriteLine("Error!\r\nDetails: " + status.ReturnedMessage);
                    return status;
                }

                //Apply DC Link 2 Volatage at Phase 2
                status = CommonModel.EMSCommonsV104.ApplyDCLink2OnSV(220);
                if (status.ErrorOccurred)
                {
                    TestContext.WriteLine("Error!\r\nDetails: " + status.ReturnedMessage);
                    return status;
                }
            }
            catch (Exception ex)
            {
                TestContext.WriteLine("Error!\r\nDetails: " + ex.Message);
                Debug.WriteLine(ex.ToString());
                return status;
            }
            return status;
        }
        #endregion

    }
}