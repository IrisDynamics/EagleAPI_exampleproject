/**@file Serial.cs
 * @brief This file contains the Serial class used to establish read write capabilities using the USB com ports.
 * Troubleshooting
 * ---------------
 * You may get the following error:
 * error CS0234: The type or namespace name `Ports' does not exist in the namespace `System.IO'. 
 * Are you missing an assembly reference?
 * Solution: 
 * for newer versions of unity (2019.2.3f1 for example, any that have the option .NET 4.x and .NET Standard 2.0)
 * Menu Edit | Project Settings | Player | Other Settings | API Compatibility Level: .Net 4.x
 * for older versions of unity (note .NET Standard 2.0 is not the same as .NET 2.0, if there is no .NET 2.0 option select .NET 4.x
 *  Menu Edit | Project Settings | Player | Other Settings | API Compatibility Level: .Net 2.0 
 */


using UnityEngine;
using System.Collections;
using System.Collections.Generic;
// If you get error CS0234 on this line, see troubleshooting section above
using System.IO.Ports;

/**\class Serial
 *@brief This component helps sending and receiving data from a serial port.
 * It detects line breaks and notifies the attached gameObject of new lines as they arrive.
 *  
 * Usage 1: Receive data when you expect line breaks
 * -------
 * 
 * - drop this script to a gameObject
 * - will call EagleAPI.Receive(line) when a new line is received to parse the EagleAPI command.
 *
 * Usage 2: Send data
 * -------
 * - from any script, call the static functions Serial.Write()
 * 
 */
public class Serial : MonoBehaviour
{
    bool connectedflag = false;             //!< Set true when a serial connection has been made
    bool wasconnected = false;              //!< Previous connection state 
    public static int portAttempt = 1;      //!< Used to cycle through the comm ports to find the desired port
    public static bool correctPort = false; //!< set to true in EagleAPI when the right port has been confirmed
    private List<string> linesIn = new List<string>();  //!< buffer of the incomming lines
    public static string port_name = "";     //!< name of currently connected serial port.

    /**Gets the lines count.
     * \return The lines count
     */
    public int linesCount { get { return linesIn.Count; } }

    #region Private vars

    /// buffer data as they arrive, until a new line is received
    private string BufferIn = "";

    /// flag to detect whether coroutine is still running to workaround coroutine being stopped after saving scripts while running in Unity
    private int nCoroutineRunning = 0;

    #endregion

    #region Static vars

    /// Serial port used for read write
    public static SerialPort s_serial;

    /// Enable debug info.
    private static bool s_debug = false;

    private static float s_lastDataIn = 0;
    private static float s_lastDataCheck = 0;

    /// Serial port baud rate
    static int speed = 115200;
    #endregion
    
    /**Called when unity or unity app closed. Close the serial port so other applications can use it
     */
    public void OnApplicationQuit()
    {
        if (s_serial != null)
        {
            if (s_serial.IsOpen)
            {
                if (s_debug)
                {
                    Debug.Log("closing serial port");
                }
                s_serial.Close();
            }
            s_serial = null;
        }
    }
    /**Monobehaviour function called every frame checks and reestablishes the connection and runs the read coroutine loop 
     */
    void Update()
    {
        if (wasconnected != connectedflag)
        {
            if (connectedflag)
            {
                GetPortName();
                if (!checkOpen()) Open();
            }
            else
            {
                correctPort = false;
            }
            wasconnected = connectedflag;
        }

        if (s_serial != null)
        {
            // Will (re)open if device disconnected and reconnected
            checkOpen();
            if (!(connectedflag = Open()))
            {
               s_serial = null;
                if (!checkOpen()) Open();
                //OnEnable();
            }

            if (nCoroutineRunning == 0)
            {
                switch (Application.platform)
                {

                    case RuntimePlatform.WindowsEditor:
                    case RuntimePlatform.WindowsPlayer:
                    case RuntimePlatform.WebGLPlayer:

                        // Each instance has its own coroutine but only one will be active
                        StartCoroutine(ReadSerialLoopWin());
                        break;

                    default:
                        // Each instance has its own coroutine but only one will be active
                        StartCoroutine(ReadSerialLoop());
                        break;

                }
            }
            else
            {
                if (nCoroutineRunning > 1)
                {
                    if (s_debug)
                    {
                        Debug.Log(nCoroutineRunning + " coroutines in " + name);
                    }
                }

                nCoroutineRunning = 0;
            }
        }
    }

    /**Read coroutine for non windows editors
     */ 
    public IEnumerator ReadSerialLoop()
    {

            if (!enabled)
            {
                if (s_debug)
                {
                    Debug.Log("behaviour not enabled, stopping coroutine");
                }
                yield break;
            }
            nCoroutineRunning++;

            s_lastDataCheck = Time.time;
            try
            {
                while (s_serial.BytesToRead > 0)
                {  // BytesToRead crashes on Windows -> use ReadLine or ReadByte in a Thread or Coroutine

                    string serialIn = s_serial.ReadExisting();
                    receivedData(serialIn);

                s_lastDataIn = s_lastDataCheck;
                }

            }
            catch (System.Exception e)
            {
                if (s_debug)
                {
                    Debug.LogError("System.Exception in serial.ReadExisting: " + e.ToString());
                }
            }

            if (s_serial.IsOpen && s_serial.BytesToRead == -1)
            {
                s_serial.Close();
            }

           yield return null;

    }
    /**Read coroutine for windows editors
     */
    public IEnumerator ReadSerialLoopWin()
    {
        if (s_debug)
        {
            Debug.Log("Start listening on com port: " + s_serial.PortName);
        }

            if (!enabled)
            {
                Debug.Log("behaviour not enabled, stopping coroutine");
                yield break;
            }
            nCoroutineRunning++;

            string serialIn = "";
            s_lastDataCheck = Time.time;
            try
            {
                s_serial.ReadTimeout = 1;

                while (s_serial.IsOpen)
                {  // BytesToRead crashes on Windows -> use ReadLine or ReadByte in a Thread or Coroutine
                    char c = (char)s_serial.ReadByte(); // ReadByte crashes on mac if device is removed (or Leonardo reset)
                    serialIn += c;
                }
            }
            catch (System.TimeoutException)
            {
            }
            catch (System.IO.IOException e)
            {
                Debug.LogError(e);
                // may happen when device is reset or disconnected. Close the port to attempt reopening later
                s_serial.Close();
            }
            catch (System.Exception e)
            {
                Debug.LogError("System.Exception in serial.ReadByte: " + e.ToString());
            }

            if (serialIn.Length > 0)
            {
                receivedData(serialIn);
                s_lastDataIn = s_lastDataCheck;
            }

            yield return null;

    }

    /** return all received lines and clear them
   * Useful if you need to process all the received lines, even if there are several since last call
   * \return List of all received lines
   */
    public List<string> GetLines()
    {

        List<string> lines = new List<string>(linesIn);

        linesIn.Clear();

        return lines;
    }

    /**Send data to the serial port.
     * \param message Full message to send
     */
    public static void Write(string message)
    {
        if (checkOpen())
        {
            s_serial.Write(message);
        }
    }

    /**Verify if the serial port is opened
     * \return If port is opened true false otherwise
     */
    public static bool checkOpen()
    {

        if (s_serial == null)
        {

            int portSpeed = speed;
            string portName = GetPortName();
            if (portName == "")
            {
                if (s_debug)
                {
                    Debug.Log("Error: Couldn't find serial port.");
                }
                return false;
            }
            else
            {

                switch (Application.platform)
                {
                    case RuntimePlatform.WindowsEditor:
                    case RuntimePlatform.WindowsPlayer:
                    case RuntimePlatform.WebGLPlayer:
                        // Needed to open port above COM9 on Windows
                        // Note: only possible with new SerialPort(). Changing portName of an existing SerialPort will throw a ArgumentException: value
                        portName = @"\\.\" + portName;
                        break;
                }

                if (s_debug)
                {
                    Debug.Log("Opening serial port: " + portName + " at " + portSpeed + " bauds");
                    port_name = portName;
                }
            }

            if (s_serial != null && s_serial.IsOpen)
            {
                s_serial.Close();
            }

            s_serial = new SerialPort(portName, portSpeed);
        }
        return s_serial.IsOpen;
    }

    /**Opens serial port if not open
     * \return Result of checkOpen() function
     */
    public static bool Open()
    {
        if (!s_serial.IsOpen)
        {
            try
            {

                s_serial.Open();
                s_serial.DtrEnable = true;

                // clear input buffer from previous garbage
                s_serial.DiscardInBuffer();

            }
            catch (System.Exception e)
            {
                if (s_debug)
                {
                    Debug.LogError("System.Exception in serial.Open(): " + e.ToString());
                }
            }
        }

        return checkOpen();
    }

    /**Data has been received, send it to the EagleAPI parser
     */
    protected void receivedData(string data)
    {
        
        /// prepend pending buffer to received data and split by line
        string[] lines = (BufferIn + data).Split('\n');

        /// If last line is not empty, it means the line is not complete (new line did not arrive yet), 
        /// We keep it in buffer for next data.
        int nLines = lines.Length;
        BufferIn = lines[nLines - 1];
        // Loop until the penultimate line (don't use the last one: either it is empty or it has already been saved for later)
        for (int iLine = 0; iLine < nLines - 1; iLine++)
        {
            string line = lines[iLine];
            //SendMessage("OnSerialLine", line);
            EagleAPI.Receive(line);
        }


    }

    static string GetPortName()
    {
        string[] portNames_config = { "/dev/tty.usb", "/dev/ttyUSB", "/dev/cu.usb", "/dev/cuUSB" };

        if (s_debug)
        {
            Debug.Log("Prefered port names:\n" + string.Join("\n", portNames_config));
        }

        List<string> portNames = new List<string>();

        switch (Application.platform)
        {

            case RuntimePlatform.OSXPlayer:
            case RuntimePlatform.OSXEditor:
            case RuntimePlatform.LinuxEditor:
            case RuntimePlatform.LinuxPlayer:

                portNames.AddRange(System.IO.Ports.SerialPort.GetPortNames());

                if (portNames.Count == 0)
                {
                    portNames.AddRange(System.IO.Directory.GetFiles("/dev/", "cu.*"));
                }
                break;

            case RuntimePlatform.WindowsEditor:
            case RuntimePlatform.WindowsPlayer:
            case RuntimePlatform.WebGLPlayer:
            default:

                portNames.AddRange(System.IO.Ports.SerialPort.GetPortNames());
                break;
        }

        if (s_debug)
        {
            Debug.Log(portNames.Count + "available ports: \n" + string.Join("\n", portNames.ToArray()));
        }

        foreach (string name in portNames_config) 
        {
            string foundName = portNames.Find(s => s.Contains(name));
            if (foundName != null)
            {
                if (s_debug)
                {
                    Debug.Log("Found port " + foundName);
                }
                return foundName;
            }
        }

        // Defaults to last port in list (most chance to be an Arduino port)
        if (portNames.Count > 0)
        {
            if (portAttempt > portNames.Count) portAttempt = 1;
            return port_name = portNames[portNames.Count - portAttempt];// 1];
        }
            
        else
            return "";
    }

}



