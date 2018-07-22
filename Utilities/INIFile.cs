using IniParser;
using IniParser.Model;
using System;
using System.IO;

namespace HardwareSerialMonitor.Utilities
{
    class INIFile
    {

        public const string DEFAULT_SECTION = "DeviceConfig";
        public const string DEFAULT_FILENAME = "Config.ini";

        public static void CreateINIData()//create INI data file data
        {
            var data = new IniData();
            IniData createData = new IniData();
            FileIniDataParser iniParser = new FileIniDataParser();

            createData.Sections.AddSection(DEFAULT_SECTION);
            createData.Sections.GetSectionData(DEFAULT_SECTION).Comments.Add("This is the configuration file for the Application");
            createData.Sections.GetSectionData(DEFAULT_SECTION).Keys.AddKey("VendorID", "0000");
            createData.Sections.GetSectionData(DEFAULT_SECTION).Keys.AddKey("ProductID", "0000");
            createData.Sections.GetSectionData(DEFAULT_SECTION).Keys.AddKey("DeviceID", "0");
            iniParser.WriteFile(DEFAULT_FILENAME, createData);
        }

        public static void ModifyINIData(String name, String value) // Modify INI data file data
        {

            if (File.Exists(DEFAULT_FILENAME))
            {
                FileIniDataParser iniParser = new FileIniDataParser();
                IniData modifiedData = iniParser.ReadFile(DEFAULT_FILENAME);
                modifiedData[DEFAULT_SECTION][name] = value;
                iniParser.WriteFile(DEFAULT_FILENAME, modifiedData);
            }
            else
            {
                var data = new IniData();
                IniData createData = new IniData();
                FileIniDataParser iniParser = new FileIniDataParser();

                createData.Sections.AddSection(DEFAULT_SECTION);
                createData.Sections.GetSectionData(DEFAULT_SECTION).Comments.Add("This is the configuration file for the Application");
                createData.Sections.GetSectionData(DEFAULT_SECTION).Keys.AddKey(name, value);
                iniParser.WriteFile(DEFAULT_FILENAME, createData);
            }
        }

        public static String ReadINIData(String name, String defaultValue = null) //Read INI data
        {
            string readIniData = defaultValue;
            if (File.Exists(DEFAULT_FILENAME))
            {
                FileIniDataParser iniParser = new FileIniDataParser();
                IniData readData = iniParser.ReadFile(DEFAULT_FILENAME);
                readIniData = readData[DEFAULT_SECTION][name];
            }
            return readIniData;
        }
    }
}
