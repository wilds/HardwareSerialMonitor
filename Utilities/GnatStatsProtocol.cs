using OpenHardwareMonitor.Hardware;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace HardwareSerialMonitor.Utilities
{
    class GnatStatsProtocol
    {

        public unsafe struct Packet
        {

            public const int STRING_SIZE = 32;

            public fixed byte pcName[STRING_SIZE];

            public fixed byte cpuName[STRING_SIZE];
            public float cpuTemp;
            public float cpuLoad;
            public float cpuClock;

            public fixed byte gpuName[STRING_SIZE];
            public float gpuTemp;
            public float gpuLoad;
            public float gpuCoreClock;
            public float gpuMemoryClock;
            public float gpuShaderClock;

            public float ramLoad;
            public float ramUsed;
            public float ramAvailable;

            public void SetPcName(string name)
            {
                fixed (byte* p = Encoding.ASCII.GetBytes(name.PadRight(STRING_SIZE)))
                fixed (byte* b = pcName)
                {
                    Buffer.MemoryCopy(p, b, STRING_SIZE, Math.Min(name.Length, STRING_SIZE));
                }
            }
            public string GetPcName()
            {
                fixed (byte* b = pcName)
                {
                    return DecodeAscii(b);
                }
            }

            public void SetCpuName(string name)
            {
                fixed (byte* p = Encoding.ASCII.GetBytes(name.PadRight(STRING_SIZE)))
                fixed (byte* b = cpuName)
                {
                    Buffer.MemoryCopy(p, b, STRING_SIZE, Math.Min(name.Length, STRING_SIZE));
                }
            }
            public string GetCpuName()
            {
                fixed (byte* b = cpuName)
                {
                    return DecodeAscii(b);
                }
            }

            public void SetGpuName(string name)
            {
                fixed (byte* p = Encoding.ASCII.GetBytes(name.PadRight(STRING_SIZE)))
                fixed (byte* b = gpuName)
                {
                    Buffer.MemoryCopy(p, b, STRING_SIZE, Math.Min(name.Length, STRING_SIZE));
                }
            }

            public string GetGpuName()
            {
                fixed (byte* b = gpuName)
                {
                    return DecodeAscii(b);
                }
            }

            public static byte[] GetBytes(Packet str)
            {
                int size = Marshal.SizeOf(str);
                byte[] arr = new byte[size];

                IntPtr ptr = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(str, ptr, true);
                Marshal.Copy(ptr, arr, 0, size);
                Marshal.FreeHGlobal(ptr);
                return arr;
            }

            public static Packet FromBytes(byte[] arr)
            {
                Packet str = new Packet();

                int size = Marshal.SizeOf(str);
                IntPtr ptr = Marshal.AllocHGlobal(size);

                Marshal.Copy(arr, 0, ptr, size);

                str = (Packet)Marshal.PtrToStructure(ptr, str.GetType());
                Marshal.FreeHGlobal(ptr);

                return str;
            }

            private unsafe static string DecodeAscii(byte* buffer)
            {
                return new string((sbyte*)buffer);
            }

            // Safer
            private static string DecodeAscii(byte[] buffer)
            {
                int count = Array.IndexOf<byte>(buffer, 0, 0);
                if (count < 0) count = buffer.Length;
                return Encoding.ASCII.GetString(buffer, 0, count);
            }
        }

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



        public Packet BuildMessage()
        {
            Packet packet = new Packet();

            // enumerating all the hardware
            foreach (OpenHardwareMonitor.Hardware.IHardware hw in thisComputer.Hardware)// for each hardware item thisComputer
            {
                hw.Update();
                switch (hw.HardwareType)
                {
                    case HardwareType.Mainboard:
                        packet.SetPcName(hw.Name);
                        break;
                    case HardwareType.CPU:
                        packet.SetCpuName(hw.Name);
                        packet.cpuTemp = hw.Sensors.FirstOrDefault(i => i.SensorType == SensorType.Temperature && i.Name.ToUpper().Contains("PACKAGE"))?.Value ?? 0f;
                        packet.cpuClock = hw.Sensors.Where(i => i.SensorType == SensorType.Clock).Max(i => i.Value) ?? 0f;
                        packet.cpuLoad = hw.Sensors.FirstOrDefault(i => i.SensorType == SensorType.Load && i.Name.ToUpper().Contains("TOTAL"))?.Value ?? 0f;
                        break;
                    case HardwareType.GpuNvidia:
                    case HardwareType.GpuAti:
                        packet.SetGpuName(hw.Name);
                        packet.gpuTemp = hw.Sensors.FirstOrDefault(i => i.SensorType == SensorType.Temperature)?.Value ?? 0f;
                        packet.gpuCoreClock = hw.Sensors.FirstOrDefault(i => i.SensorType == SensorType.Clock && i.Name.ToUpper().Contains("CORE"))?.Value ?? 0f;
                        packet.gpuMemoryClock = hw.Sensors.FirstOrDefault(i => i.SensorType == SensorType.Clock && i.Name.ToUpper().Contains("MEMORY"))?.Value ?? 0f;
                        packet.gpuShaderClock = hw.Sensors.FirstOrDefault(i => i.SensorType == SensorType.Clock && i.Name.ToUpper().Contains("SHADER"))?.Value ?? 0f;
                        packet.gpuLoad = hw.Sensors.FirstOrDefault(i => i.SensorType == SensorType.Load && i.Name.ToUpper().Contains("TOTAL"))?.Value ?? 0f;
                        break;
                    case HardwareType.RAM:
                        packet.ramLoad = hw.Sensors.FirstOrDefault(i => i.SensorType == SensorType.Load).Value ?? 0f;
                        packet.ramUsed = hw.Sensors.FirstOrDefault(i => i.SensorType == SensorType.Data && i.Name.ToUpper().Contains("USED"))?.Value ?? 0f;
                        packet.ramAvailable = hw.Sensors.FirstOrDefault(i => i.SensorType == SensorType.Data && i.Name.ToUpper().Contains("AVAILABLE"))?.Value ?? 0f;
                        break;
                }
            }
            return packet;
        }

        public string BuildStringMessage() //function overload with 0 arguments, called when a with the default serial port on a timer
        {
            Packet packet = BuildMessage();

            //Debug.WriteLine("CPU Name:" + cpuName + " | GPU Name:" + gpuName);
            //Debug.WriteLine(gpuCoreClock + gpuMemoryClock + gpuShaderClock + cpuClock);
            string stats = string.Empty;//create a new string and instantiate it as empty
            stats = "C" + packet.cpuTemp + "c " + packet.cpuLoad + "%|G" + packet.gpuTemp + "c " + packet.gpuLoad + "%|R" + packet.ramUsed + "G|"; //write the strings to the new string along with separators and denotations the arduino can understand
            //Debug.WriteLine(stats + cpuName + gpuName + gpuCoreClock + gpuMemoryClock + gpuShaderClock + cpuClock);
            if (stats != string.Empty)//so long as its not empty
            {
                return stats + packet.GetCpuName() + packet.GetGpuName() + packet.gpuCoreClock + packet.gpuMemoryClock + packet.gpuShaderClock + packet.cpuClock;
                //SendToArduino(stats + cpuName + gpuName + gpuCoreClock + gpuMemoryClock + gpuShaderClock + cpuClock);//send the string to the function
                // sendToArduino(cpuName+gpuName);
            }
            return string.Empty;
        }
    }
}
