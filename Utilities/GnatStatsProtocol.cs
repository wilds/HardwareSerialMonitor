using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HardwareSerialMonitor.Utilities
{
    class GnatStatsProtocol
    {
        private OpenHardwareMonitor.Hardware.Computer thisComputer;                 //set 'thisComputer' as the name of the instance for the dll

        public GnatStatsProtocol()
        {
            thisComputer = new OpenHardwareMonitor.Hardware.Computer()
            {  //initialise the dll instance as a new one 
                CPUEnabled = true,                                //enable the datafield to be gathered
                GPUEnabled = true,
                HDDEnabled = true,
                MainboardEnabled = true,
                RAMEnabled = true,
            };
            thisComputer.Open();
        }

        public string BuildMessage() //function overload with 0 arguments, called when a with the default serial port on a timer
        {
            string cpuTemp = "";
            string gpuTemp = "";
            string gpuLoad = "";
            string cpuLoad = "";
            string ramUsed = "";
            string cpuName = "";
            string gpuName = "";
            string gpuCoreClock = "";
            string gpuMemoryClock = "";
            string gpuShaderClock = "";
            string cpuClock = "";
            int highestCPUClock = 0;
            // enumerating all the hardware
            foreach (OpenHardwareMonitor.Hardware.IHardware hw in thisComputer.Hardware)// for each hardware item thisComputer
            {
                //Debug.WriteLine("Hardware Name="+hw.Name);
                //Debug.WriteLine("Checking: " + hw.HardwareType);
                if (hw.HardwareType.ToString().IndexOf("CPU") > -1)
                {
                    cpuName = "CPU:";
                    cpuName += hw.Name;
                }
                else if (hw.HardwareType.ToString().IndexOf("Gpu") > -1)
                {
                    gpuName = "GPU:";
                    gpuName += hw.Name;
                }
                hw.Update();                                                            //update it

                // searching for all sensors and adding data to listbox
                foreach (OpenHardwareMonitor.Hardware.ISensor s in hw.Sensors)          //for each sensor in the sensors part of the hardware
                {
                    //Console.WriteLine("Sensor: " + s.Name + " Type: " + s.SensorType + " Value: " + s.Value);
                    if (s.SensorType == OpenHardwareMonitor.Hardware.SensorType.Temperature)   // if the sensor type is a temperature sensor
                    {
                        //Debug.WriteLine("s.Name=" + s.Name);
                        if (s.Value != null)                                                  //if the value is not null
                        {
                            int curTemp = (int)s.Value;                                       //create a new int and set its value to the temperature value

                            switch (s.Name)                                                   // create a switch based on the sensor name
                            {
                                case "CPU Package":                                           // if the name is "CPU package"
                                    cpuTemp = curTemp.ToString();                             // set the string cpuTemp to the int value above converted to a string 
                                    break;                                                    // break from the switch so it doesnt run the case below
                                case "GPU Core":                                              //if the name is "GPU Core"
                                    gpuTemp = curTemp.ToString();
                                    break;
                            }

                        }
                    }
                    if (s.SensorType == OpenHardwareMonitor.Hardware.SensorType.Clock)   // if the sensor type is a temperature sensor
                    {
                        //Debug.WriteLine("s.Name=" + s.Name);
                        if (s.Value != null)                                                  //if the value is not null
                        {
                            int clockSpeed = (int)s.Value;                                       //create a new int and set its value to the temperature value

                            switch (s.Name)                                                   // create a switch based on the sensor name
                            {
                                // break from the switch so it doesnt run the case below
                                case "GPU Core":                                              //if the name is "GPU Core"
                                    gpuCoreClock = "|GCC" + clockSpeed.ToString();
                                    break;
                                case "GPU Memory":                                              //if the name is "GPU Memory"
                                    gpuMemoryClock = "|GMC" + clockSpeed.ToString();
                                    break;
                                case "GPU Shader":                                              //if the name is "GPU Shader"
                                    gpuShaderClock = "|GSC" + clockSpeed.ToString();
                                    break;
                            }
                            if (s.Name.IndexOf("CPU Core") > -1)
                            {
                                if (clockSpeed > highestCPUClock) // run through each iteration of CPU Core and if the speed is higher than the last save it
                                {
                                    highestCPUClock = clockSpeed;
                                    cpuClock = "|CHC" + highestCPUClock.ToString() + "|";
                                }
                            }

                        }
                    }
                    if (s.SensorType == OpenHardwareMonitor.Hardware.SensorType.Load)           // if the sensor type is a load value
                    {
                        if (s.Value != null)                                                    // if the value is not null
                        {
                            int curLoad = (int)s.Value;                                         // create a new int and set its value to the sensor value
                            switch (s.Name)                                                     //create a switch based on the name again
                            {
                                case "CPU Total":                                               //if the name is "CPU Total"
                                    cpuLoad = curLoad.ToString();                               //set the string cpuLoad to the int value converted to a string
                                    break;
                                case "GPU Core":
                                    gpuLoad = curLoad.ToString();
                                    break;
                            }
                        }
                    }
                    if (s.SensorType == OpenHardwareMonitor.Hardware.SensorType.Data)           // if the sensor is a data value etc.etc.
                    {
                        if (s.Value != null)
                        {
                            switch (s.Name)
                            {
                                case "Used Memory":                                             //if the name is "used memory"
                                    decimal decimalRam = Math.Round((decimal)s.Value, 1);       // create a new decimal and set the value to the sensor value a rounded to 1 decimal place
                                    ramUsed = decimalRam.ToString();                            // set the ramused string to the decimal converted to a string
                                    break;
                            }
                        }
                    }
                }
                if (cpuTemp == "") // if there is no cpuTemp assigned from earlier functions, get the average cpu temp
                {
                    foreach (OpenHardwareMonitor.Hardware.ISensor s in hw.Sensors)          //for each sensor in the sensors part of the hardware
                    {
                        int numTemps = 0;
                        int averageTemp = 0;
                        try
                        {
                            if (s.SensorType == OpenHardwareMonitor.Hardware.SensorType.Temperature)   // if the sensor type is a temperature sensor
                            {

                                if (s.Name.IndexOf("CPU Core") > -1)
                                {
                                    averageTemp = averageTemp + (int)s.Value;
                                    numTemps++;
                                }

                            }

                            {
                                averageTemp = averageTemp / numTemps;
                                cpuTemp = averageTemp.ToString();
                            }
                        }
                        catch { }

                    }
                }
            }
            Debug.WriteLine("CPU Name:" + cpuName + " | GPU Name:" + gpuName);
            Debug.WriteLine(gpuCoreClock + gpuMemoryClock + gpuShaderClock + cpuClock);
            string stats = string.Empty;//create a new string and instantiate it as empty
            stats = "C" + cpuTemp + "c " + cpuLoad + "%|G" + gpuTemp + "c " + gpuLoad + "%|R" + ramUsed + "G|"; //write the strings to the new string along with separators and denotations the arduino can understand
            Debug.WriteLine(stats);//output the string to the debug console
            Debug.WriteLine(cpuName + gpuName);
            if (stats != string.Empty)//so long as its not empty
            {
                return stats + cpuName + gpuName + gpuCoreClock + gpuMemoryClock + gpuShaderClock + cpuClock;
                //SendToArduino(stats + cpuName + gpuName + gpuCoreClock + gpuMemoryClock + gpuShaderClock + cpuClock);//send the string to the function
                // sendToArduino(cpuName+gpuName);
            }
            return "";
        }
    }
}
