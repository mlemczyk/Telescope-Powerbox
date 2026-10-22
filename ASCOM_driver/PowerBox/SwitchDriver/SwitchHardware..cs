// TODO fill in this information for your driver, then remove this line!
//
// ASCOM Switch hardware class for PowerBox
//
// Description:	 <To be completed by driver developer>
//
// Implements:	ASCOM Switch interface version: <To be completed by driver developer>
// Author:		(XXX) Your N. Here <your@email.here>

// TODO: Customise the SetConnected and InitialiseHardware methods as needed for your hardware

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ASCOM;
using ASCOM.Astrometry;
using ASCOM.Astrometry.AstroUtils;
using ASCOM.Astrometry.NOVAS;
using ASCOM.DeviceInterface;
using ASCOM.Utilities;

namespace ASCOM.PowerBox.Switch
{
    //
    // TODO Customise the InitialiseHardware() method with code to set up a communication path to your hardware and validate that the hardware exists
    //
    // TODO Customise the SetConnected() method with code to connect to and disconnect from your hardware
    // NOTE You should not need to customise the code in the Connecting, Connect() and Disconnect() members as these are already fully implemented and call SetConnected() when appropriate.
    //
    // TODO Replace the not implemented exceptions with code to implement the functions or throw the appropriate ASCOM exceptions.
    //

    /// <summary>
    /// ASCOM Switch hardware class for PowerBox.
    /// </summary>
    [HardwareClass()] // Class attribute flag this as a device hardware class that needs to be disposed by the local server when it exits.
    internal static class SwitchHardware
    {
        // Constants used for Profile persistence
        internal const string comPortProfileName = "COM Port";
        internal const string comPortDefault = "COM1";
        internal const string traceStateProfileName = "Trace Level";
        internal const string traceStateDefault = "true";

        private static string DriverProgId = ""; // ASCOM DeviceID (COM ProgID) for this driver, the value is set by the driver's class initialiser.
        private static string DriverDescription = ""; // The value is set by the driver's class initialiser.
        internal static string comPort; // COM port name (if required)
        private static bool connectedState; // Local server's connected state
        private static bool runOnce = false; // Flag to enable "one-off" activities only to run once.
        internal static Util utilities; // ASCOM Utilities object for use as required
        internal static AstroUtils astroUtilities; // ASCOM AstroUtilities object for use as required
        internal static TraceLogger tl; // Local server's trace logger object for diagnostic log with information that you specify

        private static List<Guid> uniqueIds = new List<Guid>(); // List of driver instance unique IDs


        // -------------------------------------------------------------
        private static float dewPointCelcius;
        private static float ambientTempCelcius;

        private static float twelveVolts;
        private static float fiveVolts;
        private static  float twentyFourVolts;

        private static  float scopeAmps;
        private static  float primaryHeaterAmps;
        private static float secondaryHeaterAmps;

        private static float primaryHeaterCelcius;
        private static float secondaryHeaterCelcius;
        private static float mirrorFanCelcius;

        private static int primaryHeaterDutyCycle; //0-100
        private static int secondaryHeaterDutyCycle; //0-100
        private static int mirrorFanDutyCycle; //0-100

        private static int mirrorFanMode; //-1 = auto, 0 = off, 1 = on

        private static long lastUpdateMillis;

        private static double averagePrimaryHeaterDiff = 0;
        private static long averagePrimaryHeaterDiffCount = 0;
        private static double averageSecondaryHeaterDiff = 0;
        private static long averageSecondaryHeaterDiffCount = 0;
        private static double averageMirrorDiff = 0;
        private static long averageMirrorDiffCount = 0;

        // -------------------------------------------------------------


        /// <summary>
        /// Initializes a new instance of the device Hardware class.
        /// </summary>
        static SwitchHardware()
        {
            try
            {
                // Create the hardware trace logger in the static initialiser.
                // All other initialisation should go in the InitialiseHardware method.
                tl = new TraceLogger("", "PowerBox.Hardware");
                tl.Enabled = true;

                // DriverProgId has to be set here because it used by ReadProfile to get the TraceState flag.
                DriverProgId = Switch.DriverProgId; // Get this device's ProgID so that it can be used to read the Profile configuration values

                // ReadProfile has to go here before anything is written to the log because it loads the TraceLogger enable / disable state.
                ReadProfile(); // Read device configuration from the ASCOM Profile store, including the trace state

                LogMessage("SwitchHardware", $"Static initialiser completed.");
            }
            catch (Exception ex)
            {
                try { LogMessage("SwitchHardware", $"Initialisation exception: {ex}"); } catch { }
                MessageBox.Show($"SwitchHardware - {ex.Message}\r\n{ex}", $"Exception creating {Switch.DriverProgId}", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }

        /// <summary>
        /// Place device initialisation code here
        /// </summary>
        /// <remarks>Called every time a new instance of the driver is created.</remarks>
        internal static void InitialiseHardware()
        {
            // This method will be called every time a new ASCOM client loads your driver
            LogMessage("InitialiseHardware", $"Start.");

            // Add any code that you want to run every time a client connects to your driver here

            // Add any code that you only want to run when the first client connects in the if (runOnce == false) block below
            if (runOnce == false)
            {
                LogMessage("InitialiseHardware", $"Starting one-off initialisation.");

                DriverDescription = Switch.DriverDescription; // Get this device's Chooser description

                LogMessage("InitialiseHardware", $"ProgID: {DriverProgId}, Description: {DriverDescription}");

                connectedState = false; // Initialise connected to false
                utilities = new Util(); //Initialise ASCOM Utilities object
                astroUtilities = new AstroUtils(); // Initialise ASCOM Astronomy Utilities object

                LogMessage("InitialiseHardware", "Completed basic initialisation");

                // Add your own "one off" device initialisation here e.g. validating existence of hardware and setting up communications
                // If you are using a serial COM port you will find the COM port name selected by the user through the setup dialogue in the comPort variable.

                LogMessage("InitialiseHardware", $"One-off initialisation complete.");
                runOnce = true; // Set the flag to ensure that this code is not run again
            }
        }

        // PUBLIC COM INTERFACE ISwitchV3 IMPLEMENTATION

        #region Common properties and methods.

        /// <summary>
        /// Displays the Setup Dialogue form.
        /// If the user clicks the OK button to dismiss the form, then
        /// the new settings are saved, otherwise the old values are reloaded.
        /// THIS IS THE ONLY PLACE WHERE SHOWING USER INTERFACE IS ALLOWED!
        /// </summary>
        public static void SetupDialog()
        {
            // Don't permit the setup dialogue if already connected
            if (IsConnected)
                MessageBox.Show("Already connected, just press OK");

            using (SetupDialogForm F = new SetupDialogForm(tl))
            {
                var result = F.ShowDialog();
                if (result == DialogResult.OK)
                {
                    WriteProfile(); // Persist device configuration values to the ASCOM Profile store
                }
            }
        }

        /// <summary>Returns the list of custom action names supported by this driver.</summary>
        /// <value>An ArrayList of strings (SafeArray collection) containing the names of supported actions.</value>
        public static ArrayList SupportedActions
        {
            get
            {
                LogMessage("SupportedActions Get", "Returning empty ArrayList");
                return new ArrayList();
            }
        }

        /// <summary>Invokes the specified device-specific custom action.</summary>
        /// <param name="ActionName">A well known name agreed by interested parties that represents the action to be carried out.</param>
        /// <param name="ActionParameters">List of required parameters or an <see cref="String.Empty">Empty String</see> if none are required.</param>
        /// <returns>A string response. The meaning of returned strings is set by the driver author.
        /// <para>Suppose filter wheels start to appear with automatic wheel changers; new actions could be <c>QueryWheels</c> and <c>SelectWheel</c>. The former returning a formatted list
        /// of wheel names and the second taking a wheel name and making the change, returning appropriate values to indicate success or failure.</para>
        /// </returns>
        public static string Action(string actionName, string actionParameters)
        {
            LogMessage("Action", $"Action {actionName}, parameters {actionParameters} is not implemented");
            throw new ActionNotImplementedException("Action " + actionName + " is not implemented by this driver");
        }

        /// <summary>
        /// Transmits an arbitrary string to the device and does not wait for a response.
        /// Optionally, protocol framing characters may be added to the string before transmission.
        /// </summary>
        /// <param name="Command">The literal command string to be transmitted.</param>
        /// <param name="Raw">
        /// if set to <c>true</c> the string is transmitted 'as-is'.
        /// If set to <c>false</c> then protocol framing characters may be added prior to transmission.
        /// </param>
        public static void CommandBlind(string command, bool raw)
        {
            CheckConnected("CommandBlind");
            // TODO The optional CommandBlind method should either be implemented OR throw a MethodNotImplementedException
            // If implemented, CommandBlind must send the supplied command to the mount and return immediately without waiting for a response

            throw new MethodNotImplementedException($"CommandBlind - Command:{command}, Raw: {raw}.");
        }

        /// <summary>
        /// Transmits an arbitrary string to the device and waits for a boolean response.
        /// Optionally, protocol framing characters may be added to the string before transmission.
        /// </summary>
        /// <param name="Command">The literal command string to be transmitted.</param>
        /// <param name="Raw">
        /// if set to <c>true</c> the string is transmitted 'as-is'.
        /// If set to <c>false</c> then protocol framing characters may be added prior to transmission.
        /// </param>
        /// <returns>
        /// Returns the interpreted boolean response received from the device.
        /// </returns>
        public static bool CommandBool(string command, bool raw)
        {
            CheckConnected("CommandBool");
            // TODO The optional CommandBool method should either be implemented OR throw a MethodNotImplementedException
            // If implemented, CommandBool must send the supplied command to the mount, wait for a response and parse this to return a True or False value

            throw new MethodNotImplementedException($"CommandBool - Command:{command}, Raw: {raw}.");
        }

        /// <summary>
        /// Transmits an arbitrary string to the device and waits for a string response.
        /// Optionally, protocol framing characters may be added to the string before transmission.
        /// </summary>
        /// <param name="Command">The literal command string to be transmitted.</param>
        /// <param name="Raw">
        /// if set to <c>true</c> the string is transmitted 'as-is'.
        /// If set to <c>false</c> then protocol framing characters may be added prior to transmission.
        /// </param>
        /// <returns>
        /// Returns the string response received from the device.
        /// </returns>
        public static string CommandString(string command, bool raw)
        {
            CheckConnected("CommandString");
            // TODO The optional CommandString method should either be implemented OR throw a MethodNotImplementedException
            // If implemented, CommandString must send the supplied command to the mount and wait for a response before returning this to the client

            throw new MethodNotImplementedException($"CommandString - Command:{command}, Raw: {raw}.");
        }

        /// <summary>
        /// Deterministically release both managed and unmanaged resources that are used by this class.
        /// </summary>
        /// <remarks>
        /// TODO: Release any managed or unmanaged resources that are used in this class.
        /// 
        /// Do not call this method from the Dispose method in your driver class.
        ///
        /// This is because this hardware class is decorated with the <see cref="HardwareClassAttribute"/> attribute and this Dispose() method will be called 
        /// automatically by the  local server executable when it is irretrievably shutting down. This gives you the opportunity to release managed and unmanaged 
        /// resources in a timely fashion and avoid any time delay between local server close down and garbage collection by the .NET runtime.
        ///
        /// For the same reason, do not call the SharedResources.Dispose() method from this method. Any resources used in the static shared resources class
        /// itself should be released in the SharedResources.Dispose() method as usual. The SharedResources.Dispose() method will be called automatically 
        /// by the local server just before it shuts down.
        /// 
        /// </remarks>
        public static void Dispose()
        {
            try { LogMessage("Dispose", $"Disposing of assets and closing down."); } catch { }

            try
            {
                // Clean up the trace logger and utility objects
                tl.Enabled = false;
                tl.Dispose();
                tl = null;
            }
            catch { }

            try
            {
                utilities.Dispose();
                utilities = null;
            }
            catch { }

            try
            {
                astroUtilities.Dispose();
                astroUtilities = null;
            }
            catch { }
        }

        /// <summary>
        /// Synchronously connects to or disconnects from the hardware
        /// </summary>
        /// <param name="uniqueId">Driver's unique ID</param>
        /// <param name="newState">New state: Connected or Disconnected</param>
        public static void SetConnected(Guid uniqueId, bool newState)
        {
            // Check whether we are connecting or disconnecting
            if (newState) // We are connecting
            {
                // Check whether this driver instance has already connected
                if (uniqueIds.Contains(uniqueId)) // Instance already connected
                {
                    // Ignore the request, the unique ID is already in the list
                    LogMessage("SetConnected", $"Ignoring request to connect because the device is already connected.");
                }
                else // Instance not already connected, so connect it
                {
                    // Check whether this is the first connection to the hardware
                    if (uniqueIds.Count == 0) // This is the first connection to the hardware so initiate the hardware connection
                    {
                        //
                        // Add hardware connect logic here
                        //
                        LogMessage("SetConnected", $"Connecting to hardware.");
                        if (IsConnected)
                        {
                            LogMessage("SetConnected", $"Connected!");
                        }
                        else
                        {
                            LogMessage("SetConnected", $"Not connected");
                            return;
                        }
                    }
                    else // Other device instances are connected so the hardware is already connected
                    {
                        // Since the hardware is already connected no action is required
                        LogMessage("SetConnected", $"Hardware already connected.");
                    }

                    // The hardware either "already was" or "is now" connected, so add the driver unique ID to the connected list
                    uniqueIds.Add(uniqueId);
                    LogMessage("SetConnected", $"Unique id {uniqueId} added to the connection list.");
                }
            }
            else // We are disconnecting
            {
                // Check whether this driver instance has already disconnected
                if (!uniqueIds.Contains(uniqueId)) // Instance not connected so ignore request
                {
                    // Ignore the request, the unique ID is not in the list
                    LogMessage("SetConnected", $"Ignoring request to disconnect because the device is already disconnected.");
                }
                else // Instance currently connected so disconnect it
                {
                    // Remove the driver unique ID to the connected list
                    uniqueIds.Remove(uniqueId);
                    LogMessage("SetConnected", $"Unique id {uniqueId} removed from the connection list.");

                    // Check whether there are now any connected driver instances 
                    if (uniqueIds.Count == 0) // There are no connected driver instances so disconnect from the hardware
                    {
                        //
                        // Add hardware disconnect logic here
                        if (IsConnected)
                        {
                            LogMessage("SetConnected", $"Connected!");
                        }
                        else
                        {
                            LogMessage("SetConnected", $"Not connected");
                        }
                    }
                    else // Other device instances are connected so do not disconnect the hardware
                    {
                        // No action is required
                        LogMessage("SetConnected", $"Hardware already connected.");
                    }
                }
            }

            // Log the current connected state
            LogMessage("SetConnected", $"Currently connected driver ids:");
            foreach (Guid id in uniqueIds)
            {
                LogMessage("SetConnected", $" ID {id} is connected");
            }
        }

        /// <summary>
        /// Returns a description of the device, such as manufacturer and model number. Any ASCII characters may be used.
        /// </summary>
        /// <value>The description.</value>
        public static string Description
        {
            // TODO customise this device description if required
            get
            {
                LogMessage("Description Get", DriverDescription);
                return DriverDescription;
            }
        }

        /// <summary>
        /// Descriptive and version information about this ASCOM driver.
        /// </summary>
        public static string DriverInfo
        {
            get
            {
                Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                // TODO customise this driver description if required
                string driverInfo = $"Information about the driver itself. Version: {version.Major}.{version.Minor}";
                LogMessage("DriverInfo Get", driverInfo);
                return driverInfo;
            }
        }

        /// <summary>
        /// A string containing only the major and minor version of the driver formatted as 'm.n'.
        /// </summary>
        public static string DriverVersion
        {
            get
            {
                Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                string driverVersion = $"{version.Major}.{version.Minor}";
                LogMessage("DriverVersion Get", driverVersion);
                return driverVersion;
            }
        }

        /// <summary>
        /// The interface version number that this device supports.
        /// </summary>
        public static short InterfaceVersion
        {
            // set by the driver wizard
            get
            {
                LogMessage("InterfaceVersion Get", "3");
                return Convert.ToInt16("3");
            }
        }

        /// <summary>
        /// The short name of the driver, for display purposes
        /// </summary>
        public static string Name
        {
            // TODO customise this device name as required
            get
            {
                string name = "PowerBox";
                LogMessage("Name Get", name);
                return name;
            }
        }

        #endregion

        #region ISwitch Implementation



        //private static short numSwitch = 1;
        private static short numSwitch = 23;
         

        /// <summary>
        /// The number of switches managed by this driver
        /// </summary>
        /// <returns>The number of devices managed by this driver.</returns>
        internal static short MaxSwitch
        {
            get
            {
                LogMessage("MaxSwitch Get", numSwitch.ToString());
                return numSwitch;
            }
        }

        /// <summary>
        /// Return the name of switch device n.
        /// </summary>
        /// <param name="id">The device number (0 to <see cref="MaxSwitch"/> - 1)</param>
        /// <returns>The name of the device</returns>
        internal static string GetSwitchName(short id)
        {
            Validate("GetSwitchName", id);

            LogMessage("GetSwitchName", $"GetSwitchName({id})");
            if (id == 0)
            {
                return "Dew point - C";
            }
            else if (id == 1)
            {
                return "Ambient temp - C";
            }
            else if (id == 2)
            {
                return "Primary heater - C";
            }
            else if (id == 3)
            {
                return "Secondary heater - C";
            }
            else if (id == 4)
            {
                return "Mirror - C";
            }
            else if (id == 5)
            {
                return "---";
            }
            else if (id == 6)
            {
                return "5V line - V";                
            }
            else if (id == 7)
            {
                return "12V line - V";
            }
            else if (id == 8)
            {
                return "24V line - V";
            }
            else if (id == 9)
            {
                return "---";
            }
            else if (id == 10)
            {
                return "Telescope - A";
            }
            else if (id == 11)
            {
                return "Primary heater - A";
            }
            else if (id == 12)
            {
                return "Secondary heater - A";
            }
            else if (id == 13)
            {
                return "---";
            }
            else if (id == 14)
            {
                return "Primary heater duty cycle - %";
            }
            else if (id == 15)
            {
                return "Secondary heater duty cycle - %";
            }
            else if (id == 16)
            {
                return "Mirror duty cycle - %";
            }
            else if (id == 17)
            {
                return "---";
            }
            else if (id == 18)
            {
                return "Mirror fan mode";
            }            
            else if (id == 19)
            {
                return "---";
            }
            else if (id == 20)
            {
                return "Average Primary diff - C";
            }
            else if (id == 21)
            {
                return "Average Secondary diff - C";
            }
            else if (id == 22)
            {
                return "Average Mirror diff - C";
            }                        
            else
            { 
                throw new MethodNotImplementedException("GetSwitchName");
             }
        }

        /// <summary>
        /// Set a switch device name to a specified value.
        /// </summary>
        /// <param name="id">The device number (0 to <see cref="MaxSwitch"/> - 1)</param>
        /// <param name="name">The name of the device</param>
        internal static void SetSwitchName(short id, string name)
        {
            Validate("SetSwitchName", id);
            LogMessage("SetSwitchName", $"SetSwitchName({id}) = {name} - not implemented");
            throw new MethodNotImplementedException("SetSwitchName");
        }

        /// <summary>
        /// Gets the description of the specified switch device. This is to allow a fuller description of
        /// the device to be returned, for example for a tool tip.
        /// </summary>
        /// <param name="id">The device number (0 to <see cref="MaxSwitch"/> - 1)</param>
        /// <returns>
        /// String giving the device description.
        /// </returns>
        internal static string GetSwitchDescription(short id)
        {
            Validate("GetSwitchDescription", id);
            LogMessage("GetSwitchDescription", $"GetSwitchDescription({id})");

            /*
            if (id == 0)
            {
                return "dewPointCelcius desc";
            }
            else if (id == 1)
            {
                return "ambientTempCelcius desc";
            }
            else if (id == 2)
            {
                return "twelveVolts desc";
            }
            else if (id == 3)
            {
                return "fiveVolts desc";
            }
            else if (id == 4)
            {
                return "twentyFourVolts desc";
            }
            else if (id == 5)
            {
                return "scopeAmps desc";
            }
            else if (id == 6)
            {
                return "primaryHeaterAmps desc";
            }
            else if (id == 7)
            {
                return "secondaryHeaterAmps desc";
            }
            else if (id == 8)
            {
                return "primaryHeaterCelcius desc";
            }
            else if (id == 9)
            {
                return "secondaryHeaterCelcius desc";
            }
            else if (id == 10)
            {
                return "primaryHeaterDutyCycle desc";
            }
            else if (id == 11)
            {
                return "secondaryHeaterDutyCycle desc";
            }
            else if (id == 12)
            {
                return "lastUpdateMillis desc";
            }
            else
            {
                throw new MethodNotImplementedException("GetSwitchDescription");
            }
            */
            return "";
            
        }

        /// <summary>
        /// Reports if the specified switch device can be written to, default true.
        /// This is false if the device cannot be written to, for example a limit switch or a sensor.
        /// </summary>
        /// <param name="id">The device number (0 to <see cref="MaxSwitch"/> - 1)</param>
        /// <returns>
        /// <c>true</c> if the device can be written to, otherwise <c>false</c>.
        /// </returns>
        internal static bool CanWrite(short id)
        {
            
            Validate("CanWrite", id);
            // default behavour is to report true
            //LogMessage("CanWrite", $"CanWrite({id}): {writable}");
            
            
            return false;
                        
        }        

        #region Boolean switch members

        /// <summary>
        /// Return the state of switch device id as a boolean
        /// </summary>
        /// <param name="id">The device number (0 to <see cref="MaxSwitch"/> - 1)</param>
        /// <returns>True or false</returns>
        internal static bool GetSwitch(short id)
        {
            Validate("GetSwitch", id);
            LogMessage("GetSwitch", $"GetSwitch({id}) - always returning true?");
            return true;            
        }

        /// <summary>
        /// Sets a switch controller device to the specified state, true or false.
        /// </summary>
        /// <param name="id">The device number (0 to <see cref="MaxSwitch"/> - 1)</param>
        /// <param name="state">The required control state</param>
        internal static void SetSwitch(short id, bool state)
        {
            Validate("SetSwitch", id);
            if (!CanWrite(id))
            {
                var str = $"SetSwitch({id}) - Cannot Write";
                LogMessage("SetSwitch", str);
                //throw new MethodNotImplementedException(str);
                return;
            }

            
           
            /*
            else if (id == 25)
            {
                //reset averages
                //set current value
                averagePrimaryHeaterDiff = (primaryHeaterCelcius - ambientTempCelcius);
                averagePrimaryHeaterDiffCount = 1;

                averageSecondaryHeaterDiff = (secondaryHeaterCelcius - ambientTempCelcius);
                averageSecondaryHeaterDiffCount = 1;

                averageMirrorDiff = (mirrorCelcius - ambientTempCelcius); 
                averageMirrorDiffCount = 1;

            }
            */
            LogMessage("SetSwitch", $"SetSwitch({id}) = {state}");
            //throw new MethodNotImplementedException("SetSwitch");
            return;
        }

        #endregion

        #region Analogue members

        /// <summary>
        /// Returns the maximum value for this switch device, this must be greater than <see cref="MinSwitchValue"/>.
        /// </summary>
        /// <param name="id">The device number (0 to <see cref="MaxSwitch"/> - 1)</param>
        /// <returns>The maximum value to which this device can be set or which a read only sensor will return.</returns>
        internal static double MaxSwitchValue(short id)
        {
            Validate("MaxSwitchValue", id);
            LogMessage("MaxSwitchValue", $"MaxSwitchValue({id})");

           

            /*
            if (id == 0)
            {
                return 12;
            }
            else if (id == 1)
            {
                return 24;
            }
            else if (id == 2)
            {
                return 1;
            }            
            else
            {
                throw new MethodNotImplementedException("MaxSwitchValue");
            }
            */
            return 1;
        }

        /// <summary>
        /// Returns the minimum value for this switch device, this must be less than <see cref="MaxSwitchValue"/>
        /// </summary>
        /// <param name="id">The device number (0 to <see cref="MaxSwitch"/> - 1)</param>
        /// <returns>The minimum value to which this device can be set or which a read only sensor will return.</returns>
        internal static double MinSwitchValue(short id)
        {
            Validate("MinSwitchValue", id);
            LogMessage("MinSwitchValue", $"MinSwitchValue({id})");


            /*
            if (id == 0)
            {
                return 0;
            }
            else if (id == 1)
            {
                return 1;
            }
            else if (id == 2)
            {
                return 0;
            }
            else
            {
                throw new MethodNotImplementedException("MinSwitchValue");
            }
            */
            return 0;
            
        }

        /// <summary>
        /// Returns the step size that this device supports (the difference between successive values of the device).
        /// </summary>
        /// <param name="id">The device number (0 to <see cref="MaxSwitch"/> - 1)</param>
        /// <returns>The step size for this device.</returns>
        internal static double SwitchStep(short id)
        {
            Validate("SwitchStep", id);
            LogMessage("SwitchStep", $"SwitchStep({id})");
            //throw new MethodNotImplementedException("SwitchStep");
            return 1;
        }

        private static void updateValues()
        {
            var request = (HttpWebRequest)WebRequest.Create("http://192.168.50.39");
            request.Timeout = 2000; //2000ms
            request.Method = "GET";
            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    LogMessage("updateValues", "Connected :D ");
                    connectedState = true;

                    //parse the body
                    // Gets the stream associated with the response.
                    Stream receiveStream = response.GetResponseStream();
                    Encoding encode = System.Text.Encoding.GetEncoding("utf-8");
                    // Pipes the stream to a higher level stream reader with the required encoding format.
                    StreamReader readStream = new StreamReader(receiveStream, encode);


                    string str = readStream.ReadToEnd();
                    LogMessage("updateValues", "Response body" + str);

                    string[] lines = str.Split('\n');

                    LogMessage("updateValues", "Parsing" + lines[2]);
                    //convert str into an space delimited array and read the values
                    string[] values = lines[2].Split(' ');

                    LogMessage("updateValues", "Parsing" + values[0]);
                    dewPointCelcius = float.Parse(values[0], CultureInfo.InvariantCulture.NumberFormat);
                    LogMessage("updateValues", "Parsing" + values[1]);
                    ambientTempCelcius = float.Parse(values[1], CultureInfo.InvariantCulture.NumberFormat);

                    LogMessage("updateValues", "Parsing" + values[2]);
                    fiveVolts = float.Parse(values[2], CultureInfo.InvariantCulture.NumberFormat);
                    LogMessage("updateValues", "Parsing" + values[3]);
                    twelveVolts = float.Parse(values[3], CultureInfo.InvariantCulture.NumberFormat);
                    LogMessage("updateValues", "Parsing" + values[4]);
                    twentyFourVolts = float.Parse(values[4], CultureInfo.InvariantCulture.NumberFormat);

                    LogMessage("updateValues", "Parsing" + values[5]);
                    scopeAmps = float.Parse(values[5], CultureInfo.InvariantCulture.NumberFormat);
                    LogMessage("updateValues", "Parsing" + values[6]);
                    primaryHeaterAmps = float.Parse(values[6], CultureInfo.InvariantCulture.NumberFormat);
                    LogMessage("updateValues", "Parsing" + values[7]);
                    secondaryHeaterAmps = float.Parse(values[7], CultureInfo.InvariantCulture.NumberFormat);

                    LogMessage("updateValues", "Parsing" + values[8]);
                    primaryHeaterCelcius = float.Parse(values[8], CultureInfo.InvariantCulture.NumberFormat);
                    LogMessage("updateValues", "Parsing" + values[9]);
                    secondaryHeaterCelcius = float.Parse(values[9], CultureInfo.InvariantCulture.NumberFormat);
                    LogMessage("updateValues", "Parsing" + values[10]);
                    mirrorFanCelcius = float.Parse(values[10], CultureInfo.InvariantCulture.NumberFormat);

                    LogMessage("updateValues", "Parsing" + values[11]);
                    primaryHeaterDutyCycle = int.Parse(values[11], CultureInfo.InvariantCulture.NumberFormat);
                    LogMessage("updateValues", "Parsing" + values[12]);
                    secondaryHeaterDutyCycle = int.Parse(values[12], CultureInfo.InvariantCulture.NumberFormat);
                    LogMessage("updateValues", "Parsing" + values[13]);
                    mirrorFanDutyCycle = int.Parse(values[13], CultureInfo.InvariantCulture.NumberFormat);

                    LogMessage("updateValues", "Parsing" + values[14]);
                    mirrorFanMode = int.Parse(values[14], CultureInfo.InvariantCulture.NumberFormat);

                    LogMessage("updateValues", "Parsing" + values[15]);
                    lastUpdateMillis = long.Parse(values[15], CultureInfo.InvariantCulture.NumberFormat);
                    LogMessage("updateValues", "new last update millis" + lastUpdateMillis);



                //            private static int mirrorDutyCycle; //0-100



                    // Releases the resources of the response.
                    response.Close();
                    // Releases the resources of the Stream.
                    readStream.Close();


                    // update running averages only when I have data!
                    //new average = ((oldAvg * oldCount) + new value) / (oldCount++)
                    //need to do it carefully to avoid overflow..
                    double primaryDiff = primaryHeaterCelcius - ambientTempCelcius;
                    LogMessage("updateValues", "primary diff" + primaryDiff);
                    averagePrimaryHeaterDiffCount++;
                    LogMessage("updateValues", "averagePrimaryHeaterDiffCount" + averagePrimaryHeaterDiffCount);
                    double oldPrimaryHeaterDiff = averagePrimaryHeaterDiff;
                    LogMessage("updateValues", "oldPrimaryHeaterDiff" + oldPrimaryHeaterDiff);
                    double primaryCountFactor = (averagePrimaryHeaterDiffCount - 1.0) / averagePrimaryHeaterDiffCount;
                    LogMessage("updateValues", "primaryCountFactor" + primaryCountFactor.ToString("0.0000"));
                    averagePrimaryHeaterDiff = (oldPrimaryHeaterDiff * primaryCountFactor);
                    LogMessage("updateValues", "averagePrimaryHeaterDiff1" + averagePrimaryHeaterDiff);
                    averagePrimaryHeaterDiff += (primaryDiff / averagePrimaryHeaterDiffCount);
                    LogMessage("updateValues", "averagePrimaryHeaterDiff" + averagePrimaryHeaterDiff);

                    double secondaryDiff = secondaryHeaterCelcius - ambientTempCelcius;
                    averageSecondaryHeaterDiffCount++;
                    double oldSecondaryHeaterDiff = averageSecondaryHeaterDiff;
                    double secondaryCountFactor = (averageSecondaryHeaterDiffCount - 1.0) / averageSecondaryHeaterDiffCount;
                    averageSecondaryHeaterDiff = (oldSecondaryHeaterDiff * secondaryCountFactor);
                    averageSecondaryHeaterDiff += (secondaryDiff / averageSecondaryHeaterDiffCount);

                    double mirrorDiff = mirrorFanCelcius - ambientTempCelcius;
                    LogMessage("updateValues", "Mirror diff" + mirrorDiff);
                    averageMirrorDiffCount++;
                    LogMessage("updateValues", "Mirror diff count" + averageMirrorDiffCount);                    
                    double oldMirrorDiff = averageMirrorDiff;
                    LogMessage("updateValues", "Mirror diff old" + oldMirrorDiff);
                    double mirrorCountFactor = (averageMirrorDiffCount - 1.0) / averageMirrorDiffCount;
                    averageMirrorDiff = (oldMirrorDiff * mirrorCountFactor);
                    averageMirrorDiff += (mirrorDiff / averageMirrorDiffCount);
                    LogMessage("updateValues", "Mirror diff new avg" + averageMirrorDiff);

                    /*
                    private static double averagePrimaryHeaterDiff = 0;
                private static long averagePrimaryHeaterDiffCount = 0;
                private static double averageSecondaryHeaterDiff = 0;
                private static long averageSecondaryHeaterDiffCount = 0;
                private static double averageMirrorDiff = 0;
                private static long averageMirrorDiffCount = 0;
                    */

                }
                else
                {
                    LogMessage("updateValues", "Bad response code :( " + response.StatusCode);
                    connectedState = false;
                }
            }
            catch (Exception e)
            {
                LogMessage("updateValues", "Blew up :( " + e.Message);
                connectedState = false;
            }


            

        }

        /// <summary>
        /// Returns the value for switch device id as a double
        /// </summary>
        /// <param name="id">The device number (0 to <see cref="MaxSwitch"/> - 1)</param>
        /// <returns>The value for this switch, this is expected to be between <see cref="MinSwitchValue"/> and
        /// <see cref="MaxSwitchValue"/>.</returns>
        internal static double GetSwitchValue(short id)
        {
            Validate("GetSwitchValue", id);
            LogMessage("GetSwitchValue", $"GetSwitchValue({id})");

            if (id == 0)
            {
                updateValues(); // only get updated values when the first one is requested
                return dewPointCelcius;
            }
            else if (id == 1)
            {
                return ambientTempCelcius;
            }
            else if (id == 2)
            {
                return primaryHeaterCelcius;
            }
            else if (id == 3)
            {
                return secondaryHeaterCelcius;
            }
            else if (id == 4)
            {
                return mirrorFanCelcius;
            }
            else if (id == 5)
            {
                return 0;
            }
            else if (id == 6)
            {
                return fiveVolts;
            }
            else if (id == 7)
            {
                return twelveVolts;
            }
            else if (id == 8)
            {
                return twentyFourVolts;
            }
            else if (id == 9)
            {
                return 0;
            }
            else if (id == 10)
            {
                return scopeAmps;
            }
            else if (id == 11)
            {
                return primaryHeaterAmps;
            }
            else if (id == 12)
            {
                return secondaryHeaterAmps;
            }
            else if (id == 13)
            {
                return 0;
            }
            else if (id == 14)
            {
                return primaryHeaterDutyCycle;
            }
            else if (id == 15)
            {
                return secondaryHeaterDutyCycle;
            }
            else if (id == 16)
            {
                return mirrorFanDutyCycle;
            }
            else if (id == 17)
            {
                return 0;
            }
            else if (id == 18)
            {
                return mirrorFanMode;
            }            
            else if (id == 19)
            {
                return 0;
            }
            else if (id == 20)
            {
                return averagePrimaryHeaterDiff;
            }
            else if (id == 21)
            {
                return averageSecondaryHeaterDiff;
            }
            else if (id == 22)
            {
                return averageMirrorDiff;
            }
            
            else
            {
                throw new MethodNotImplementedException("GetSwitchValue");
            }

        }

        /// <summary>
        /// Set the value for this device as a double.
        /// </summary>
        /// <param name="id">The device number (0 to <see cref="MaxSwitch"/> - 1)</param>
        /// <param name="value">The value to be set, between <see cref="MinSwitchValue"/> and <see cref="MaxSwitchValue"/></param>
        internal static void SetSwitchValue(short id, double value)
        {
            Validate("SetSwitchValue", id, value);
            if (!CanWrite(id))
            {
                LogMessage("SetSwitchValue", $"SetSwitchValue({id}) - Cannot write");
                return;
                //throw new ASCOM.MethodNotImplementedException($"SetSwitchValue({id}) - Cannot write");
            }          

            LogMessage("SetSwitchValue", $"SetSwitchValue({id}) = {value} - not implemented");
            //throw new MethodNotImplementedException("SetSwitchValue");
            return;
        }

        #endregion

        #region Async members

        /// <summary>
        /// Set a boolean switch's state asynchronously
        /// </summary>
        /// <exception cref="MethodNotImplementedException">When CanAsync(id) is false.</exception>
        /// <param name="id">Switch number.</param>
        /// <param name="state">New boolean state.</param>
        /// <remarks>
        /// <p style="color:red"><b>This is an optional method and can throw a <see cref="MethodNotImplementedException"/> when <see cref="CanAsync(short)"/> is <see langword="false"/>.</b></p>
        /// </remarks>
        public static void SetAsync(short id, bool state)
        {
            Validate("SetAsync", id);
            if (!CanAsync(id))
            {
                var message = $"SetAsync({id}) - Switch cannot operate asynchronously";
                LogMessage("SetAsync", message);
                throw new MethodNotImplementedException(message);
            }

            // Implement async support here if required
            LogMessage("SetAsync", $"SetAsync({id}) = {state} - not implemented");
            throw new MethodNotImplementedException("SetAsync");
        }

        /// <summary>
        /// Set a switch's value asynchronously
        /// </summary>
        /// <param name="id">Switch number.</param>
        /// <param name="value">New double value.</param>
        /// <p style="color:red"><b>This is an optional method and can throw a <see cref="MethodNotImplementedException"/> when <see cref="CanAsync(short)"/> is <see langword="false"/>.</b></p>
        /// <exception cref="MethodNotImplementedException">When CanAsync(id) is false.</exception>
        /// <remarks>
        /// <p style="color:red"><b>This is an optional method and can throw a <see cref="MethodNotImplementedException"/> when <see cref="CanAsync(short)"/> is <see langword="false"/>.</b></p>
        /// </remarks>
        public static void SetAsyncValue(short id, double value)
        {
            Validate("SetSwitchValue", id, value);
            if (!CanWrite(id))
            {
                LogMessage("SetSwitchValue", $"SetSwitchValue({id}) - Cannot write");
                throw new ASCOM.MethodNotImplementedException($"SetSwitchValue({id}) - Cannot write");
            }

            // Implement async support here if required
            LogMessage("SetSwitchValue", $"SetSwitchValue({id}) = {value} - not implemented");
            throw new MethodNotImplementedException("SetSwitchValue");
        }

        /// <summary>
        /// Flag indicating whether this switch can operate asynchronously.
        /// </summary>
        /// <param name="id">Switch number.</param>
        /// <returns>True if the switch can operate asynchronously.</returns>
        /// <exception cref="MethodNotImplementedException">When CanAsync(id) is false.</exception>
        /// <remarks>
        /// <p style="color:red"><b>This is a mandatory method and must not throw a <see cref="MethodNotImplementedException"/>.</b></p>
        /// </remarks>
        public static bool CanAsync(short id)
        {
            const bool ASYNC_SUPPORT_DEFAULT = false;

            Validate("CanAsync", id);

            // Default behaviour is not to support async operation
            LogMessage("CanAsync", $"CanAsync({id}): {ASYNC_SUPPORT_DEFAULT}");
            return ASYNC_SUPPORT_DEFAULT;
        }

        /// <summary>
        /// Completion variable for asynchronous switch state change operations.
        /// </summary>
        /// <param name="id">Switch number.</param>
        /// <exception cref="OperationCancelledException">When an in-progress operation is cancelled by the <see cref="CancelAsync(short)"/> method.</exception>
        /// <returns>False while an asynchronous operation is underway and true when it has completed.</returns>
        /// <remarks>
        /// <p style="color:red"><b>This is a mandatory method and must not throw a <see cref="MethodNotImplementedException"/>.</b></p>
        /// </remarks>
        public static bool StateChangeComplete(short id)
        {
            const bool STATE_CHANGE_COMPLETE_DEFAULT = true;

            Validate("StateChangeComplete", id);
            LogMessage("StateChangeComplete", $"StateChangeComplete({id}) - Returning {STATE_CHANGE_COMPLETE_DEFAULT}");
            return STATE_CHANGE_COMPLETE_DEFAULT;
        }

        /// <summary>
        /// Cancels an in-progress asynchronous state change operation.
        /// </summary>
        /// <param name="id">Switch number.</param>
        /// <exception cref="MethodNotImplementedException">When it is not possible to cancel an asynchronous change.</exception>
        /// <remarks>
        /// <p style="color:red"><b>This is an optional method and can throw a <see cref="MethodNotImplementedException"/>.</b></p>
        /// This method must be implemented if it is possible for the device to cancel an asynchronous state change operation, otherwise it must throw a <see cref="MethodNotImplementedException"/>.
        /// </remarks>
        public static void CancelAsync(short id)
        {
            Validate("CancelAsync", id);
            LogMessage("CancelAsync", $"CancelAsync({id}) - not implemented");
            throw new MethodNotImplementedException("CancelAsync");
        }

        #endregion

        #endregion

        #region Private methods

        /// <summary>
        /// Checks that the switch id is in range and throws an InvalidValueException if it isn't
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="id">The id.</param>
        private static void Validate(string message, short id)
        {
            if (id < 0 || id >= numSwitch)
            {
                LogMessage(message, string.Format("Switch {0} not available, range is 0 to {1}", id, numSwitch - 1));
                throw new InvalidValueException(message, id.ToString(), string.Format("0 to {0}", numSwitch - 1));
            }
        }

        /// <summary>
        /// Checks that the switch id and value are in range and throws an
        /// InvalidValueException if they are not.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="id">The id.</param>
        /// <param name="value">The value.</param>
        private static void Validate(string message, short id, double value)
        {
            Validate(message, id);
            var min = MinSwitchValue(id);
            var max = MaxSwitchValue(id);
            if (value < min || value > max)
            {
                LogMessage(message, string.Format("Value {1} for Switch {0} is out of the allowed range {2} to {3}", id, value, min, max));
                throw new InvalidValueException(message, value.ToString(), string.Format("Switch({0}) range {1} to {2}", id, min, max));
            }
        }

        #endregion

        #region Private properties and methods
        // Useful methods that can be used as required to help with driver development

        /// <summary>
        /// Returns true if there is a valid connection to the driver hardware
        /// </summary>
        private static bool IsConnected
        {
            get
            {
                // TODO check that the driver hardware connection exists and is connected to the hardware

                //TODO: ML: Check if I can hit the expected HTML url
                //http://192.168.50.39/

                var request = (HttpWebRequest)WebRequest.Create("http://192.168.50.39");
                request.Timeout = 100; //100ms
                request.Method = "HEAD";
                try
                {
                    var response = (HttpWebResponse)request.GetResponse();
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        LogMessage("IsConnected", "Connected :D ");
                        connectedState = true;
                    }
                    else
                    {
                        LogMessage("IsConnected", "Not Connected :( ");
                        connectedState = false;
                    }
                }
                catch (Exception e)
                {
                    LogMessage("IsConnected", "Not Connected :( ");
                    connectedState = false;

                }
                return connectedState;
            }
        }        

        /// <summary>
        /// Use this function to throw an exception if we aren't connected to the hardware
        /// </summary>
        /// <param name="message"></param>
        private static void CheckConnected(string message)
        {
            if (!IsConnected)
            {
                throw new NotConnectedException(message);
            }
        }

        /// <summary>
        /// Read the device configuration from the ASCOM Profile store
        /// </summary>
        internal static void ReadProfile()
        {
            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "Switch";
                tl.Enabled = Convert.ToBoolean(driverProfile.GetValue(DriverProgId, traceStateProfileName, string.Empty, traceStateDefault));
                comPort = driverProfile.GetValue(DriverProgId, comPortProfileName, string.Empty, comPortDefault);
            }
        }

        /// <summary>
        /// Write the device configuration to the  ASCOM  Profile store
        /// </summary>
        internal static void WriteProfile()
        {
            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "Switch";
                driverProfile.WriteValue(DriverProgId, traceStateProfileName, tl.Enabled.ToString());
                driverProfile.WriteValue(DriverProgId, comPortProfileName, comPort.ToString());
            }
        }

        /// <summary>
        /// Log helper function that takes identifier and message strings
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="message"></param>
        internal static void LogMessage(string identifier, string message)
        {
            tl.LogMessageCrLf(identifier, message);
        }

        /// <summary>
        /// Log helper function that takes formatted strings and arguments
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="message"></param>
        /// <param name="args"></param>
        internal static void LogMessage(string identifier, string message, params object[] args)
        {
            var msg = string.Format(message, args);
            LogMessage(identifier, msg);
        }
        #endregion
    }
}

