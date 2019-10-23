
// * This component helps sending and receiving data from a serial port.
// * It detects line breaks and notifies the attached gameObject of new lines as they arrive.
// * 
// * Usage 1: Receive data when you expect line breaks
// * -------
// * 
// * - drop this script to a gameObject
// * - create a script on the same gameObject to receive new line notifications
// * - add the OnSerialLine() function, here is an example
// *
// * void OnSerialLine(string line)
//{
//    *Debug.Log("Got a line: " + line);
//    *  }
// *
// * Usage 2: Send data
// * -------
// *
// * - from any script, call the static functions Serial.Write() or Serial.WriteLn()
// *
// * Troubleshooting
// * ---------------
// *
// * You may get the following error:
// * error CS0234: The type or namespace name `Ports' does not exist in the namespace `System.IO'. 
// * Are you missing an assembly reference?
//* Solution: 
// for older versions of unity
// * Menu Edit | Project Settings | Player | Other Settings | API Compatibility Level: .Net 2.0 
// for newer versions of unity (2018.3.10 for example)
// * Menu Edit | Project Settings | Player | Other Settings | API Compatibility Level: .Net 4.x
// */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
// If you get error CS0234 on this line, see troubleshooting section above
using System.IO.Ports;
using System;

public class Serial : MonoBehaviour
{
    public bool showconnection = true;
    bool connectedflag = false;
    bool toggleflag = true;
    bool wasconnected = false;
    string buttonname = "Disconnected";
    float starttime;
    public static int portAttempt = 1;
    public static bool correctPort = false;
    public static int packets = 0;
    private List<string> linesIn = new List<string>();
    public static string port_name = "";
    /// <summary>
    /// Gets the lines count.
    /// </summary>
    /// <value>The lines count.</value>
    public int linesCount { get { return linesIn.Count; } }

    #region Private vars

    // buffer data as they arrive, until a new line is received
    private string BufferIn = "";

    // flag to detect whether coroutine is still running to workaround coroutine being stopped after saving scripts while running in Unity
    private int nCoroutineRunning = 0;

    #endregion

    #region Static vars

    // Only one serial port shared among all instances and living after all instances have been destroyed
    public static SerialPort s_serial;

    // All instances of this component
    private static List<Serial> s_instances = new List<Serial>();

    // Enable debug info.
    private static bool s_debug = false;

    private static float s_lastDataIn = 0;
    private static float s_lastDataCheck = 0;

    static int speed = 115200;
    #endregion

    void OnEnable()
    {
        s_instances.Clear();
        s_instances.Add(this);

        if (toggleflag)
            if (!checkOpen())
                if (Open())
                {
                    buttonname = "Connected";
                }
    }

    public void OnDisable()
    {
        linesIn.Clear();
        connectedflag = false;
        //s_instances.s_serial.Close();
        s_instances.Remove(this);
    }
    public void OpenSerialLine()
    {
        OnApplicationQuit();
        toggleflag = false;
        connectedflag = false;
        buttonname = "Disconnected";
    }

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

    void Update()
    {
        if (s_serial != null)
        {
            // Will (re)open if device disconnected and reconnected
            if (toggleflag)
            {
                checkOpen();
                if (!(connectedflag = Open()))
                {
                    s_serial = null;
                    OnEnable();
                }
            }

            if (nCoroutineRunning == 0)
            {
                switch (Application.platform)
                {

                    case RuntimePlatform.WindowsEditor:
                    case RuntimePlatform.WindowsPlayer:
                    case RuntimePlatform.WebGLPlayer:
                        //				case RuntimePlatform.OSXEditor:
                        //				case RuntimePlatform.OSXPlayer:

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

    public IEnumerator ReadSerialLoop()
    {

        while (toggleflag)
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
                    // Dispatch new data to each instance
                    foreach (Serial inst in s_instances)
                    {
                        inst.receivedData(serialIn);
                    }

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
                // This happens when Leonardo is reset
                // Close the serial port here, it will be reopened later when available
                s_serial.Close();
            }

            yield return null;
        }

    }

    public IEnumerator ReadSerialLoopWin()
    {
        if (s_debug)
        {
            Debug.Log("Start listening on com port: " + s_serial.PortName);
        }

        while (toggleflag)
        {

            if (!enabled)
            {
                Debug.Log("behaviour not enabled, stopping coroutine");
                yield break;
            }

            //Debug.Log ("ReadSerialLoopWin ");
            nCoroutineRunning++;
            //Debug.Log ("nCoroutineRunning: " + nCoroutineRunning);
            //Debug.Log ("Still listening on com port: " + s_serial.PortName + " open (" + s_serial.IsOpen + ") with coroutine from " + this);

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

                //Debug.Log("just read some data: " + serialIn);

                // Dispatch new data to each instance
                foreach (Serial inst in s_instances)
                {
                    inst.receivedData(serialIn);
                }
                s_lastDataIn = s_lastDataCheck;
            }

            yield return null;
        }

    }

    /// return all received lines and clear them
    /// Useful if you need to process all the received lines, even if there are several since last call
    public List<string> GetLines(bool keepLines = false)
    {

        List<string> lines = new List<string>(linesIn);

        if (!keepLines)
            linesIn.Clear();

        return lines;
    }

    /// <summary>
    /// Send data to the serial port.
    /// </summary>
    public static void Write(string message)
    {
        if (checkOpen())
        {
           //Debug.Log(message);
            s_serial.Write(message);
            packets++;
        }
            
           

    }

    /// <summary>
    /// Send data to the serial port and append a new line character (\n)
    /// </summary>
    public static void WriteLn(string message = "")
    {
        Write(message + "\n");
        //Debug.Log(message);
    }
    /// <summary>
    /// Verify if the serial port is opened and opens it if necessary
    /// </summary>
    /// <returns><c>true</c>, if port is opened, <c>false</c> otherwise.</returns>
    /// <param name="portSpeed">Port speed.</param>
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

                //if (s_debug)
                //{
                    Debug.Log("Opening serial port: " + portName + " at " + portSpeed + " bauds");
                port_name = portName;
                //}
            }

            if (s_serial != null && s_serial.IsOpen)
            {
                s_serial.Close();
            }

            s_serial = new SerialPort(portName, portSpeed);
        }
        return s_serial.IsOpen;
    }

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

    // Data has been received, do what this instance has to do with it
    protected void receivedData(string data)
    {
        
        // prepend pending buffer to received data and split by line
        string[] lines = (BufferIn + data).Split('\n');

        // If last line is not empty, it means the line is not complete (new line did not arrive yet), 
        // We keep it in buffer for next data.
        int nLines = lines.Length;
        BufferIn = lines[nLines - 1];
        // Loop until the penultimate line (don't use the last one: either it is empty or it has already been saved for later)
        for (int iLine = 0; iLine < nLines - 1; iLine++)
        {
            string line = lines[iLine];
            //SendMessage("OnSerialLine", line);
            EagleAPI.Receive(line);
            
            //todo try  might speed things up.
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

        //if (s_debug)
        //{
            Debug.Log(portNames.Count + "available ports: \n" + string.Join("\n", portNames.ToArray()));
        //}

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
            return portNames[portNames.Count - portAttempt];// 1];
        }
            
        else
            return "";
    }


    void OnGUI()
    {
        bool lasttoggle = toggleflag;
        GUI.depth = 0;

        if (showconnection)
        {
            toggleflag = GUI.Toggle(new Rect(Screen.width - 200, Screen.height - 60, 100, 50), toggleflag, buttonname, "Button");
        }
        
        if (lasttoggle != toggleflag)
        {
            if (!toggleflag)
            {
                connectedflag = false;
                OnDisable();
            }

        }
        if (wasconnected != connectedflag)
        {
            if (connectedflag)
            {
                buttonname = "Connected";
                OnEnable();
            }
            else
            {
                buttonname = "Disconnected";
                starttime = Time.time;
            }
            wasconnected = connectedflag;
        }
        if (((Time.time - starttime) > 1.5) & !connectedflag&toggleflag)
        {
            OnDisable();
        }
    }
}



