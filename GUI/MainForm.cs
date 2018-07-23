using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.IO.Ports;
using System.Management;
using System.Reflection;
using System.Diagnostics;
using HardwareSerialMonitor.Properties;
using System.Runtime.InteropServices;
using HardwareSerialMonitor.Utilities;
using static HardwareSerialMonitor.Utilities.GnatStatsProtocol;

namespace HardwareSerialMonitor
{
    public partial class MainForm : Form
    {
        private static Int32 dataCheckInterval = 3000;                              // set the interval for the timers below to send data to the arduino

        private static string defaultVendorID = Convert.ToString(INIFile.ReadINIData("VendorID", "0000"));      //set the device VID, this will be the default one to connect to (sparkfun pro micro)
        private static string defaultProductID = Convert.ToString(INIFile.ReadINIData("ProductID", "0000"));    //set the device PID, this will be the default one to connect to (sparkfun pro micro)        
        private static string defaultDeviceID = Convert.ToString(INIFile.ReadINIData("DeviceID", "0"));         //this is a device ID string to contain the exact device ID

        private string automaticDeviceReference = Device.GetDeviceReference(defaultVendorID, defaultProductID);  //add the VID and PID together into string to check against the com port description later
        private string manualDeviceReference = string.Empty;                                                 //VID and PID string for the Com port in manual selection mode

        
        private bool isAttached = false;                                            // boolean to denote whether the default device is attached to the computer
        private bool manualIsAttached = false;                                      // boolean to denote whether the manual device is attached to the computer
        private string manualPortSelected = string.Empty;                           // string to store the selected port in Manual mode

        private bool isConnected = false;                                           // boolean to denote whether the default device is connected and port opened
        private static SerialPort mySerialPort;                                     //Port for the default device

        private NotifyIcon ApplicationIcon;                                         // notify icon for the notification bar
        private Icon trayIcon;                                                      // an icon instance to assign to the nofication bar

        private Timer connectionTimer;                                // create a timer to check if the default device has been connected and send data to it
        private ContextMenuStrip menu = new ContextMenuStrip();                     //create a menu strip for the notification icon
        private ToolStripMenuItem automaticPortSelecMenuItem;

        private GnatStatsProtocol gnatStatsProtocol = new GnatStatsProtocol();

        public MainForm()
        {

            InitializeComponent();                              //Initialise the Form and its components

            trayIcon = Resources.TrayIcon;         //set this file as the tray icon
            ApplicationIcon = new NotifyIcon        //initialise the Notification icon in the notify bar
            {
                Icon = trayIcon,                    //set the image icon as the icon set above
                Visible = true,                     //make it visible
                BalloonTipIcon = ToolTipIcon.Info,  //add icon bubble fields
                BalloonTipText = "Hardware LCD Monitor",
                BalloonTipTitle = "Hardware LCD Monitor"
            };

            this.WindowState = FormWindowState.Minimized;  //start minimized
            this.ShowInTaskbar = false; // dont show icon on the taskbar
            this.Hide(); //Hide

            ApplicationIcon.ContextMenuStrip = menu;            //add the menu items to the menu strip
            ApplicationIcon.MouseUp += ApplicationIcon_MouseUp; //set an event manager for mouse right click 

            connectionTimer = new Timer
            {
                Interval = dataCheckInterval       //sets the connectionTimer to "tick" in milliseconds to the int32 assigned
            };
            connectionTimer.Tick += ConnectionTimer_Tick;     //event manager for each "tick"
            connectionTimer.Start();                           //start the timer

            UsbDeviceNotifier.RegisterUsbDeviceNotification(this.Handle); // handle a notifier for usb devices

            isAttached = Device.CheckDevice(automaticDeviceReference, defaultDeviceID); // check if the device is already attached (handler only looks after devices added or removed)

            CreateMenuItems();                                  //run the function to create the menu items for the notification icon
        }

        private void ConnectionTimer_Tick(object sender, EventArgs e)
        {
            if (!isConnected)
            {
                if (IsAutomaticPortSelectionEnabled() && isAttached)
                {
                    isConnected = TryConnectPort(Device.GetPortFromDevice(automaticDeviceReference, defaultDeviceID));
                } else if (manualIsAttached)
                {
                    isConnected = TryConnectPort(manualPortSelected);

                }
            }

            if (isConnected)
            {
                ApplicationIcon.Icon = Resources.TrayIconGreen;
                SendToSerialPort(gnatStatsProtocol.BuildMessage());
            }
            else
                ApplicationIcon.Icon = Resources.TrayIconRed;

        }

        private void ApplicationIcon_MouseUp(object sender, MouseEventArgs e)//the event manager for mouse right click on the notification icon
        {
            InvalidateMenu(menu);//run the function to wipe the menu and refresh it.
            if (e.Button == MouseButtons.Left) //if its a left click
            {
                MethodInfo mi = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic); // create and instantiate method to act as a right click
                mi.Invoke(ApplicationIcon, null);//invoke the method above
            }
        }

        void CreateMenuItems()
        {

            String autoText = (IsAutomaticPortSelectionEnabled()) && (mySerialPort != null) ? "Automatic - " + Device.GetDeviceName(mySerialPort.PortName.ToString()) : "Automatic Mode";
            automaticPortSelecMenuItem = new ToolStripMenuItem(autoText, null, AutomaticPortSelect_Click)
            {
                Checked = IsAutomaticPortSelectionEnabled()           // else set it as false
            }; 
            menu.Items.Add(automaticPortSelecMenuItem);               // add it to the menu

            // menu.Items.Add(new ToolStripMenuItem("Serial Ports"));               // add item to the contextmenu menu

            string[] ports = SerialPort.GetPortNames(); // set up a string array called ports and set it to the list of port names
            foreach (string port in ports) // for each string in the array
            {
                menu.Items.Add(new ToolStripMenuItem(Device.GetDeviceName(port), Resources.Serial, new EventHandler((sender, e) => ManualPortSelect_Click(sender, e, port)))
                    {
                        Checked = port == manualPortSelected                            // if the port is the port that was selected, mark it as checked
                    }
                );          //add the item to the contextmenu menu
            }

            menu.Items.Add(new ToolStripSeparator());              // at the separator to the contextmenu

            menu.Items.Add(new ToolStripMenuItem("Refresh", null, Refresh_Click));             // add the item to the menu

            menu.Items.Add(new ToolStripSeparator());              // add the separator to the menu
 
            menu.Items.Add(new ToolStripMenuItem("About", Resources.info, new System.EventHandler(About_Click)));

            menu.Items.Add(new ToolStripSeparator());              // add the separator to the menu

            menu.Items.Add(new ToolStripMenuItem("Exit", Resources.Exit, new System.EventHandler(Exit_Click)));
        }

        void InvalidateMenu(ContextMenuStrip menu)
        {
            menu.Items.Clear();//clear the context menu 
            CreateMenuItems(); //run the function to repopulate it
        }

        private void Refresh_Click(object sender, EventArgs e)
        {
            InvalidateMenu(menu);
            MethodInfo mi = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic); // create and instantiate method
            mi.Invoke(ApplicationIcon, null);//invoke the method above
        }

        private void About_Click(object sender, EventArgs e)
        {
            AboutBox aboutBox = new AboutBox(); //create a new version of the AboutBox form (used as a template)
            aboutBox.Show(); //display it
        }

        void Exit_Click(object sender, EventArgs e)
        {
            ClosePort();

            this.Dispose();

            Application.Exit();//if the application has been closed
        }

        public new void Dispose()
        {
            Dispose(true);
            ApplicationIcon.Icon = null;
        }

        private void AutomaticPortSelect_Click(object sender, EventArgs e)// automatic port selection handler
        {

            manualPortSelected = string.Empty;          //set the portSelected as blank
            automaticPortSelecMenuItem.Checked = IsAutomaticPortSelectionEnabled();

            ClosePort();
            if (manualIsAttached)
            {
                isAttached = true;
                manualIsAttached = false;
                automaticDeviceReference = manualDeviceReference;//save the vid and pid from the manual selection
            }
        }

        void ManualPortSelect_Click(object sender, EventArgs e, string selected_port)
        {
            manualIsAttached = true;//if the manual port device is attached

            StoreDeviceInConfig(selected_port);

            Selected_Serial(selected_port);

        }

        void Selected_Serial(string selected_port)//an overload of the function above, called when the manual device is reattached
        {
            ClosePort();

            manualPortSelected = selected_port;
            automaticPortSelecMenuItem.Checked = IsAutomaticPortSelectionEnabled();

            TryConnectPort(selected_port);
        }

        private void SendToSerialPort(Packet packet)//function to send the data to the arduino over the com port
        {
            if (!mySerialPort.IsOpen)//if the port is not open
            {
                try
                {
                    mySerialPort.Open();//try open the port
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Error opening port: " + e.ToString());//catch any errors and output them
                }
            }
            try
            {
                byte[] data = Packet.GetBytes(packet);
                mySerialPort.Write(data, 0, data.Length);//try write to the manual port
            }
            catch (Exception e)
            {
                Debug.WriteLine("Error sending Serial Data: " + e.ToString());//catch any errors and output them
                string errorString = e.ToString();
                if (errorString.IndexOf("device is not connected") > -1)
                    Debug.WriteLine("Device Removed");
                isConnected = false;
            }

        }

        public void Usb_DeviceRemoved(string deviceNameID)
        {
            if (deviceNameID.IndexOf(automaticDeviceReference) > -1)
            {
                Debug.WriteLine(automaticDeviceReference);
                Debug.WriteLine("Default Device Removed");
                isAttached = false;
                if (IsAutomaticPortSelectionEnabled())
                {
                    isConnected = false;
                    try
                    {
                        mySerialPort.Dispose();
                        ClosePort();

                    }
                    catch { }//catch any errors but dont bother outputting them
                }
            } else if (deviceNameID.IndexOf(manualDeviceReference) > -1)
            {
                Debug.WriteLine("Manual Port Device Removed");
                manualIsAttached = false;

                if (!IsAutomaticPortSelectionEnabled())
                {
                    isConnected = false;
                    try
                    {
                        mySerialPort.Dispose();
                        ClosePort();

                    }
                    catch { }//catch any errors but dont bother outputting them
                }
            }
            else
            {
                Debug.WriteLine("Device:" + deviceNameID);
                Debug.WriteLine("device:" + manualDeviceReference);
            }

        }

        public void Usb_DeviceAdded(string deviceNameID)
        {
            if (deviceNameID.IndexOf(automaticDeviceReference) > -1)
            {
                Debug.WriteLine("Default Device Attached");
                System.Threading.Thread.Sleep(1000);//wait a second for the device's com port to come online
                isAttached = true;//set the automatic device as attached
            }
            if (deviceNameID.IndexOf(manualDeviceReference) > -1)
            {
                Debug.WriteLine("Manual Port Device Attached");
                System.Threading.Thread.Sleep(1000);//wait a second for the device's com port to come online
                manualIsAttached = true;
                if (!IsAutomaticPortSelectionEnabled())//if its in manual mode
                    Selected_Serial(manualPortSelected);//re assign the com port and open it
            }
        }

        protected override void WndProc(ref Message m)//function to handle the USB device notifications
        {
            base.WndProc(ref m);
            //Debug.WriteLine(m.ToString());
            if (m.Msg == UsbDeviceNotifier.WmDevicechange)
            {
                // Debug.WriteLine(m.ToString());
                switch ((int)m.WParam)
                {
                    case UsbDeviceNotifier.DbtDeviceremovecomplete:
                        DEV_BROADCAST_DEVICEINTERFACE hdrOut = (DEV_BROADCAST_DEVICEINTERFACE)m.GetLParam(typeof(DEV_BROADCAST_DEVICEINTERFACE));
                        // Debug.WriteLine("HDROut:" + hdrOut.dbcc_name);
                        Usb_DeviceRemoved(hdrOut.dbcc_name); // this is where you do your magic
                        break;
                    case UsbDeviceNotifier.DbtDevicearrival:
                        DEV_BROADCAST_DEVICEINTERFACE hdrIn = (DEV_BROADCAST_DEVICEINTERFACE)m.GetLParam(typeof(DEV_BROADCAST_DEVICEINTERFACE));
                        //Debug.WriteLine("HDRIn:" + hdrIn.dbcc_name);
                        Usb_DeviceAdded(hdrIn.dbcc_name); // this is where you do your magic
                        break;
                }
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]//sets the layout structure for the function below
        internal struct DEV_BROADCAST_DEVICEINTERFACE
        {
            // Data size.
            public int dbcc_size;
            // Device type.
            public int dbcc_devicetype;
            // Reserved data.
            public int dbcc_reserved;
            // Class GUID.
            public Guid dbcc_classguid;
            // Device name data.
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]//manage the data in the next line
            public string dbcc_name;
        }


        protected bool IsAutomaticPortSelectionEnabled()
        {
            return String.IsNullOrEmpty(manualPortSelected);
        }

        protected void StoreDeviceInConfig(string selected_port)
        {
            manualDeviceReference = Device.GetDeviceReference(selected_port); // save the device ID to a string using the function passing the port name
            defaultDeviceID = Device.GetDeviceID(selected_port);
            Device.SplitDeviceReference(manualDeviceReference, ref defaultVendorID, ref defaultProductID);


            INIFile.ModifyINIData("VendorID", defaultVendorID.ToString());
            INIFile.ModifyINIData("ProductID", defaultProductID.ToString());
            INIFile.ModifyINIData("DeviceID", defaultDeviceID.ToString());
        }

        public bool TryConnectPort(string selected_port)
        {
            Console.WriteLine("Selected port: " + selected_port);   // write the string to the console

            if (string.IsNullOrEmpty(selected_port))
                return false;

            mySerialPort = new SerialPort(selected_port, 9600, Parity.None, 8, StopBits.One)    // create a new port instance with values in the argument
            {
                ReadTimeout = 500,//set the timeouts
                WriteTimeout = 500
            };
            return OpenPort();
        }

        public bool OpenPort()
        {
            try
            {
                if (!mySerialPort.IsOpen)
                {
                    mySerialPort.Open();
                    isConnected = true;
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        public bool ClosePort()
        {
            try
            {
                if (mySerialPort.IsOpen)
                {
                    mySerialPort.Close();
                    isConnected = false;
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

    }
}
