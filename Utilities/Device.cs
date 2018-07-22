using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Management;
using System.Linq;

namespace HardwareSerialMonitor.Utilities
{
    class Device
    {

        public static string GetDeviceName(string port)
        {

            DeviceInfo device = GetCOMDevice(port);
            if (device != null)
            {
                DeviceInfo deviceInfo = GetDeviceInfo(device.DeviceID);
                return deviceInfo.Caption;
            }
            return string.Empty;

        }

        public static string GetDeviceReference(string port)
        {
            DeviceInfo device = GetCOMDevice(port);
            if (device != null)
            {
                string[] splitted = device.DeviceID.ToUpper().Split('\\');
                return splitted[1];
            }
            return string.Empty;
        }

        public static string GetDeviceID(string port)
        {
            DeviceInfo device = GetCOMDevice(port);
            if (device != null)
            {
                string[] splitted = device.DeviceID.ToUpper().Split('\\');
                return splitted[2];
            }
            return string.Empty;
        }

        public static bool CheckDevice(string deviceReference, string deviceID)//function to check if the default device is already attached once the program has started
        {
            List<DeviceInfo> devices = GetCOMDevices();
            foreach (var device in devices)
            {
                string UsbDescription = device.DeviceID.ToString().ToUpper();//convert the device ID to the string
                if (UsbDescription.IndexOf(deviceReference) > -1)//if the Vid and Pid of the default device is in the string
                {
                    // Debug.WriteLine("USBDescription:" + UsbDescription);
                    return UsbDescription.IndexOf(deviceID) > -1;//mark it as attached
                }
            }
            return false;
        }

        // TODO refactoring
        public static string GetPortFromDevice(string deviceReference, string deviceID)//function to connect to the default device
        {
            //Debug.WriteLine("Attempting to Find Device");
            string[] portNames = SerialPort.GetPortNames(); //set a string array to the names of the ports
            string sInstanceName = string.Empty; // set an empty string to assign to the instance name of the serial port
            for (int y = 0; y < portNames.Length; y++) // for every port that's available (a foreach would have also done here)
            {
                try //set a try to catch any exceptions accessing the management object searcher or opening the ports (if another program or instance of this program is running and is using that port it will cause an error)
                {
                    ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\WMI", "SELECT * FROM MSSerial_PortName");//create a new ManagementObjectSearcher and instantiate it to the value results from the search function
                    foreach (ManagementObject queryObj in searcher.Get()) // for each result from the searcher above
                    {
                        sInstanceName = queryObj["InstanceName"].ToString().ToUpper(); //set sInstanceName to the resulting instance name
                        //Debug.WriteLine("sInstanceName:" + sInstanceName);
                        //Debug.WriteLine("Vid_Pid:" + deviceReference);
                        //Debug.WriteLine("devID:" + deviceID);
                        if (deviceID != string.Empty)
                        {
                            // Debug.WriteLine("Checking DEV-ID");
                            if ((sInstanceName.IndexOf(deviceReference) > -1) && (sInstanceName.IndexOf(deviceID) > -1))//if the string Vid_Pid is present in the string
                            {
                                return queryObj["PortName"].ToString();// set the sPortName to the portname in the query
                            }
                        }
                        else
                        {
                            if (sInstanceName.IndexOf(deviceReference) > -1) //if the string Vid_Pid is present in the string
                            {
                                return queryObj["PortName"].ToString();// set the sPortName to the portname in the query
                            }
                        }
                    }
                }


                catch (ManagementException e)
                {
                    Debug.WriteLine("An error occurred while querying for WMI data: " + e.Message); //catch exceptions and output the error
                }
            }
            return string.Empty; // self explanitory

        }

        public static string GetDeviceReference(string vendorID, string productID)
        {
            return "VID_" + vendorID + "&PID_" + productID;
        }

        public static bool SplitDeviceReference(string deviceReference, ref string vendorID, ref string productID)
        {
            Int32 indexOfVID = deviceReference.IndexOf("_") + 1;
            Int32 indexOfPID = deviceReference.IndexOf("_", indexOfVID) + 1;
            Debug.WriteLine("SplitDeviceReference:" + deviceReference + " " + deviceReference.Substring(indexOfVID));
            try
            {
                vendorID = deviceReference.Substring(indexOfVID, 4);
                productID = deviceReference.Substring(indexOfPID, 4);
                return true;
            }
            catch (Exception f)
            {
                Debug.WriteLine("Exception SplitDeviceReference:" + f);
                return false;
            }
        }

        public static List<DeviceInfo> GetCOMDevices()
        {
            List<DeviceInfo> devices = new List<DeviceInfo>();

            ManagementObjectCollection collection;
            using (var searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_PnPEntity WHERE Caption like '%(COM%'"))
                collection = searcher.Get();

            foreach (var device in collection)
            {
                devices.Add(new DeviceInfo(
                    //instanceName: (string)device.GetPropertyValue("InstanceName"),
                    deviceID: (string)device.GetPropertyValue("DeviceID"),
                    pnpDeviceID: (string)device.GetPropertyValue("PNPDeviceID"),
                    caption: (string)device.GetPropertyValue("Caption"),
                    description: (string)device.GetPropertyValue("Description")
                ));
            }

            collection.Dispose();
            return devices;
        }

        public static DeviceInfo GetDeviceInfo(string deviceID)
        {
            List<DeviceInfo> devices = new List<DeviceInfo>();

            ManagementObjectCollection collection;
            using (var searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_PnPEntity WHERE DeviceID LIKE '%"+ deviceID.Replace("\\", "\\\\")  + "%'"))
                collection = searcher.Get();

            foreach (var device in collection)
            {
                devices.Add(new DeviceInfo(
                    //instanceName: (string)device.GetPropertyValue("InstanceName"),
                    deviceID: (string)device.GetPropertyValue("DeviceID"),
                    pnpDeviceID: (string)device.GetPropertyValue("PNPDeviceID"),
                    caption: (string)device.GetPropertyValue("Caption"),
                    description: (string)device.GetPropertyValue("Description")
                ));
            }

            collection.Dispose();
            return devices.FirstOrDefault();
        }

        public static DeviceInfo GetCOMDevice(string port)
        {
            List<DeviceInfo> devices = new List<DeviceInfo>();

            ManagementObjectCollection collection;
            using (var searcher = new ManagementObjectSearcher("root\\WMI", "SELECT * FROM MSSerial_PortName WHERE PortName LIKE '%" + port + "%'"))
                collection = searcher.Get();


            foreach (var device in collection)
            {
                string deviceId = (string)device.GetPropertyValue("InstanceName");
                devices.Add(new DeviceInfo(
                    instanceName: (string)device.GetPropertyValue("InstanceName"),
                    deviceID: deviceId.Substring(0, deviceId.LastIndexOf('_')),
                    //pnpDeviceID: (string)device.GetPropertyValue("PNPDeviceID"),
                    //caption: (string)device.GetPropertyValue("Caption"),
                    //description: (string)device.GetPropertyValue("Description"),
                    portName: (string)device.GetPropertyValue("portName")
                ));
            }
            return devices.FirstOrDefault();
        }

    }

    public class DeviceInfo
    {
        public DeviceInfo(string instanceName = "", string deviceID = "", string pnpDeviceID = "", string caption = "", string description = "", string portName = "")
        {
            this.DeviceID = deviceID;
            this.PnpDeviceID = pnpDeviceID;
            this.Description = description;
            this.InstanceName = instanceName;
            this.PortName = portName;
            this.Caption = caption;
        }
        public string DeviceID { get; private set; }
        public string PnpDeviceID { get; private set; }
        public string Caption { get; private set; }
        public string Description { get; private set; }
        public string InstanceName { get; private set; }
        public string PortName { get; private set; }
    }

}
