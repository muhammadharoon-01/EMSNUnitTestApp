using EMSASE2000AutomateAPI;
using PS19.ATM.ReturnStatus;

namespace EMSNUnitTestApp.Commons
{
    public static class DNP3EmulatorCommons
    {
        public static Status IssueReadCommandOnASE61850ClientAndVerifyParameterValue(string parameterName, bool isAnalog, bool isReturntypeInt, string expectedValue, ASE2000API.EasyConnectIEC61850NodeType easyConnectNodeType, int timeout = 0, bool compareByThreshold = false, double threshold = 0.005, bool failureIsPass = false)
        {
            Status status = new Status(true, "Could not Read Parameter Value from ASE 61850 Client", -1);
            try
            {
                string inputType = "Digital Input";
                if (isAnalog)
                {
                    inputType = "Analog Input";
                }
                MappingDataIEC61850 mappingData = CommonModel.EMSCommonsV104.ase2000Obj.GetIEC1850MMSTagFromProfileAndMappingData(inputType, parameterName, easyConnectNodeType);
                if (mappingData.AliasName != null)
                {
                    status = CommonModel.EMSCommonsV104.ase2000Obj.GetIECT61850VariableValueViaASE61850Client(ref mappingData);

                    bool expectedValueFound = false;
                    double dExpectedValue = 0.0;
                    if (status.ErrorOccurred == false)
                    {
                        if (isReturntypeInt)
                        {
                            status.ReturnedMessage = GetIntValue(status.ReturnedMessage);
                        }

                        if (compareByThreshold)
                        {
                            try
                            {
                                dExpectedValue = Convert.ToDouble(expectedValue);
                            }
                            catch
                            {
                                dExpectedValue = 0.0;
                            }
                            double dResult = 0.0;
                            try
                            {
                                dResult = Convert.ToDouble(status.ReturnedMessage);
                            }
                            catch
                            {
                                dResult = 0.0;
                            }
                            double thresholdVal = dResult * threshold;
                            if ((dResult >= (dExpectedValue - thresholdVal)) && (dResult <= (dExpectedValue + thresholdVal)))
                            {
                                expectedValueFound = true;
                            }

                        }
                        else
                        {
                            if (status.ReturnedMessage == expectedValue)
                            {
                                expectedValueFound = true;
                            }

                        }
                    }

                    if (expectedValueFound == true)
                    {
                        status.ReturnedMessage = $"Expected Value: {dExpectedValue} \n Actual Value: {status.ReturnedMessage}";
                    }
                    else
                    {
                        status.ErrorOccurred = true;
                        status.ReturnedMessage = $"Expected Value: {dExpectedValue} \n Actual Value: {status.ReturnedMessage}";
                        status.ReturnedValue = -1;
                    }

                    //if (status.ReturnedMessage != expectedValue)
                    //{
                    //    status.ErrorOccurred = true;
                    //}
                    if (timeout > 0)
                    {
                        Thread.Sleep(timeout);
                    }
                    if (failureIsPass == true)
                    {
                        status.ErrorOccurred = status.ErrorOccurred == true ? false : true;
                        status.ReturnedValue = 0;
                    }
                }
                else
                {
                    status.ReturnedMessage = "Could not retrieve mapping data from json persistent storage";
                }
            }
            catch (Exception ex)
            {
                status.ErrorOccurred = true;
            }
            return status;
        }
        public static Status IssueReadCommandOnDNP3ClientAndVerifyParameterValue(string parameterName, bool isAnalog, string expectedValue, ASE2000API.EasyConnectDNP3NodeType easyConnectNodeType, bool compareByThreshold = false, double threshold = 0.005, bool failureIsPass = false)
        {
            Status status = new Status(true, "Could not Read Parameter Value from Gateway", -1);
            try
            {
                for (int i = 0; i < 5; i++)
                {
                    if (isAnalog)
                    {
                        status = CommonModel.EMSCommonsV104.ase2000Obj.GetAnalogEMSTelemetryDataViaDNP3Api(parameterName, easyConnectNodeType);
                    }
                    else
                    {
                        status = CommonModel.EMSCommonsV104.ase2000Obj.GetDigitalEMSTelemetryDataViaDNP3Api(parameterName, easyConnectNodeType);
                    }
                    if (status.ErrorOccurred == true && (status.ReturnedMessage.Contains("Index was outside the bounds of the array") == true ||
                        status.ReturnedMessage.Contains("Index was out of range. Must be non-negative and less than the size of the collection") == true)
                        || status.ReturnedMessage.Contains("Could not obtain") == true)
                    {
                        CommonModel.EMSCommonsV104.ase2000Obj.StartDNP3EmulatroApp();
                        Thread.Sleep(1000);
                        continue;
                    }
                    else
                    {
                        break;
                    }
                }
                double dExpectedValue = 0.0;
                bool expectedValueFound = false;
                if (status.ErrorOccurred == false)
                {
                    if (compareByThreshold)
                    {
                        try
                        {
                            dExpectedValue = Math.Abs(Convert.ToDouble(expectedValue));
                        }
                        catch
                        {
                            dExpectedValue = 0.0;
                        }
                        double dResult = 0.0;
                        try
                        {
                            dResult = Math.Abs(Convert.ToDouble(status.ReturnedMessage));
                        }
                        catch
                        {
                            dResult = 0.0;
                        }
                        if (dResult != dExpectedValue)
                        {
                            double thresholdVal = dResult * threshold;
                            if ((dResult >= (dExpectedValue - thresholdVal)) && (dResult <= (dExpectedValue + thresholdVal)))
                            {
                                expectedValueFound = true;
                            }
                        }
                        else
                        {
                            expectedValueFound = true;
                        }
                    }
                    else
                    {
                        if (status.ReturnedMessage == expectedValue)
                        {
                            expectedValueFound = true;
                        }

                    }
                }

                if (status.ErrorOccurred == false && compareByThreshold)
                {
                    status.ReturnedMessage = $"Actual Value: {status.ReturnedMessage} Expected Value: {expectedValue}";
                }
                else
                {
                    status.ReturnedMessage = $"Actual Value: {status.ReturnedMessage} Expected Value: {expectedValue}";
                }


                if (!expectedValueFound)
                {
                    status.ErrorOccurred = true;
                    status.ReturnedValue = -1;
                }
                if (failureIsPass == true)
                {
                    status.ErrorOccurred = status.ErrorOccurred == true ? false : true;
                }
            }
            catch (Exception ex)
            {
                status.ErrorOccurred = true;
            }
            return status;
        }
        public static Status SendCommandOnDNP3Client(string parameterName, bool isAnalog, string valueToSend, ASE2000API.EasyConnectDNP3NodeType easyConnectNodeType, bool failureIsPass = false, int timeout = 0)
        {

            Status status = new Status(true, "Could not send command via ASE 61850 Client", -1);
            try
            {
                for (int i = 0; i < 5; i++)
                {
                    if (isAnalog)
                    {
                        status = CommonModel.EMSCommonsV104.ase2000Obj.SendAnalogEMSCommandViaDNP3Api(parameterName, valueToSend, easyConnectNodeType);
                    }
                    else
                    {
                        int value = Convert.ToInt32(valueToSend);
                        status = CommonModel.EMSCommonsV104.ase2000Obj.SendDigitalEMSCommandViaDNP3Api(parameterName, value, easyConnectNodeType);
                    }
                    if (status.ErrorOccurred == true && (status.ReturnedMessage.Contains("Failed to perform") == true ||
                        status.ReturnedMessage.Contains("Command not sent. Check parameter name or node type") == true) && failureIsPass == false)
                    {
                        CommonModel.EMSCommonsV104.ase2000Obj.StartDNP3EmulatroApp();
                        Thread.Sleep(1000);
                        continue;
                    }
                    else
                    {
                        break;
                    }
                }
                if (!status.ErrorOccurred)
                {
                    status.ReturnedMessage = "Command Sent Successfully.";
                }
                else
                {
                    status.ReturnedMessage = $"{status.ReturnedMessage}";
                }
                if (failureIsPass == true)
                {
                    status.ErrorOccurred = status.ErrorOccurred == true ? false : true;
                }
                if (timeout > 0)
                {
                    Thread.Sleep(timeout);
                }
            }
            catch (Exception ex)
            {
                status.ErrorOccurred = true;
            }
            return status;
        }
        public static Status SendCommandOnASE61850Client(string parameterName, bool isAnalog, string valueToSend, ASE2000API.EasyConnectIEC61850NodeType easyConnectNodeType, bool failureIsPass = false, int timeout = 0)
        {
            Status status = new Status(true, "Could not send command via ASE 61850 Client", -1);
            try
            {
                string outputType = "Digital Output";
                if (isAnalog)
                {
                    outputType = "Analog Output";
                }
                MappingDataIEC61850 mappingData = CommonModel.EMSCommonsV104.ase2000Obj.GetIEC1850MMSTagFromProfileAndMappingData(outputType, parameterName, easyConnectNodeType);
                if (mappingData.AliasName != null)
                {
                    status = CommonModel.EMSCommonsV104.ase2000Obj.SendIECT61850CommandViaASE61850Client(ref mappingData, valueToSend);
                    if (status.ReturnedMessage.Contains("Object Access Denied") == true || status.ReturnedMessage.Contains("Write Successful") == false)
                    {
                        status.ErrorOccurred = true;
                    }
                    if (failureIsPass == true)
                    {
                        status.ErrorOccurred = status.ErrorOccurred == true ? false : true;
                    }
                    if (timeout > 0)
                    {
                        Thread.Sleep(timeout);
                    }
                }
                else
                {
                    status.ReturnedMessage = "Could not retrieve mapping data from json persistent storage";
                }
            }
            catch (Exception ex)
            {
                status.ErrorOccurred = true;
            }
            return status;
        }
        public static string GetIntValue(string stringMessage)
        {
            string returnedValue = "";
            try
            {
                returnedValue = Convert.ToString(Convert.ToInt32(Convert.ToDouble(stringMessage)));
            }
            catch
            {
                returnedValue = "";
            }
            return returnedValue;
        }
    }
}
