/**
 * 
 
 * @mainpage Introduction
 * This is an app used to control the actuators force or position using sliders.
 * Communication to an Eagle controller over usb using EagleAPI protocol.
 * @section Usage
 * Ensure an eagle controller is connected to a usb port and powered according to the instructions in the Eagle Controller Reference Manual.
 * When started, the app will search for an eagle device over the connected comms port until it receives a ']response' reply.
 * When connected to the right port, the CommPort will turn white. Note that the app will not be able to connect if the IrisControls software is still running.
 * The slider at the top left will select an actuator to target, these numbers are based on how the actuators are enumerated by the eagle controller.
 * Before sending forces, ensure the system ready command has been send with either a physical button, sending and EagleAPI.SystemReady() command (can use the serial box and send [ready), or
 * by pressing the system ready button in IrisControls.
 * When the Force Control button is pressed, this app will disable position control and start streaming force commands of the amount specified by the sending force, to the target actuator.
 * When the Position Control button is pressed, this app will enable position control and start streaming position control commands to the target position specified, to the target actuator.
 * The commands are streamed at 1kHz.
 * The actuator's position in millimeters is availble under the target actuator, in addition to other relevant acutator information.
 * Ensure the actuator's shaft has been moved through it's range of motion or is in the zero position before sending the system ready command on a new powerup. This will ensure that the position reading is accurate
 * and a reasonable zero position is established. If the zero position is not as expected, send the serial command '[rp ActID' (where the ActID is your target actuator) or use the EagleAPI.ResetPosition() function.
 * 
 * The Force effects available are a spring effect, a sinusoidal force, and a constant force, which can be layered. 
 * 
 * Force Control Sliders
 * - Spring_Constant
 * This is the spring rate higher values indicate stronger spring force/mm (since force is not a defined unit)
 * 
 * - Spring_Center
 * This is the center position for spring force calculations, the center of a dca series shaft is at approximately 75mm
 * 
 * - Sine_Magnitude
 * This is the magnitdute of the sine wave, ie the force at the peaks,
 * 
 * - Sine_Frequency
 * Frequency of sine wave oscillation in Hz
 * 
 * - Constant_Force
 * Magnitude of constant force
 * 
 * Position Control Sliders
 * - Target Position
 * This is the target position in mm from the zero position of the actuator that it should try to move to, if this is not working ensure that the position controller has been tuned using IrisControls software 
 * 
 * 
 */
/**@file ActuatorDemoApp.cs
* @brief Control actuators using EagleAPI
* 
* @author Rebecca McWilliam <rmcwilliam@irisdynamics.com>
 */
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

/**\class ActuatorDemoApp
 * @brief Layer various feedback effects onto actuators using sliders.
 * 
 * Uses the Serial.cs script on same game object and uses EagleAPI to send and parse commands.
 */
public class ActuatorDemoApp : MonoBehaviour
{
    int target_actuator = 0;            //!< Actuator to send commands to 
    int last_target_actuator = 0;       //!< Last slider position used to check if the target actuator has changed to enabled position control
    long actuator_position;             //!< Target actuator's last received position
    float send_force;                   //!< Combined effects force used to send to the actuator (is limited by max force)
    float spring_force;                 //!< force due to spring effect
    float sine_force;                   //!< force due to sine wave effect
    float springCenter = 0;             //!< origin position of spring in mm, used in determining spring force
    float springK = 0;                  //!< spring constant in force/mm,  used in determining spring force
    float sine_mag = 0;                 //!< magnitude of sine wave, used in determining sine force
    float sine_freq = 0;                //!< frequency of sine wave force effect
    float const_force = 0;              //!< Constant force to send actuator
    int maxForce = 150;                 //!< maximum force to be commanded to the actuator.
    bool forceControl = true;           //!< enable the effects to the actuator (i.e. start streaming force commands with effects
    DateTime startTime;                 //!< time at start of app
    float last_second;                  //!<Time when the last second calculation was performed used to calculate packets per second
    float last_step;                    //!< Time at the last fixed time step  used to calculate timestep length
    float waiting_for_response;         //!< Time that a handshake command was sent if this is longer than a certain amount of time the port is abandonned and a new port is attempted
    GUIStyle style = new GUIStyle();    //!< Used to change the comms to green when successful port is found.
    bool extended_servo_flag = false;   //!< flag set every second to send an extended servo command if in force control mode
    string stringCommand;               //!< Command entered in the serial command text box
    int target_position = 0;            //!< target position for position control
    bool positionControl = false;       //!< Toggle to turn on position control
    bool pcenableflag = true;           //!< When true send a pc enable command (either on or off depending on circumstance)

    bool initial_handshake = true;

    /**@brief Start is called before the first frame update.
     * 
     * Sets up the window size and sends the initial handshake to check if the right port is being used by the Serial class
     */
    void Start()
    {
        Screen.SetResolution(800,600, false);
        last_second = Time.time;
        style.normal.textColor = Color.white;
    }

    
        // /**Called when unity or unity app closed. Close the serial port so other applications can use it
    //  */
    public void OnApplicationQuit()
    {
     //   Serial.OnApplicationQuit();
    }

    void Update(){
      //  Serial.Update();
    }

    /** @brief Updates at a fixed timestep, calculates physics for force control effects or position control
     */
    void FixedUpdate()
    {
        if (initial_handshake){
            EagleAPI.Handshake();       ///check if there is an Eagle controller on the connected port
            waiting_for_response = Time.time;
            initial_handshake = false;
        }
      //  if (Serial.correctPort){
            last_step = Time.time;
            ///send an extended force command every second whne in force control mode to update temperature adn voltage etc.
            if ((Time.time - last_second) > 1)
            {
                last_second = Time.time;
                extended_servo_flag = true;
            }
            ///calculate spring force
            spring_force = -(EagleAPI.actuators[target_actuator].position - springCenter*1000f) * springK/1000f;

            ///calculate sine force
            sine_force = sine_mag * Mathf.Sin(2 * Mathf.PI * sine_freq * Time.time); 

            ///layer force effects
            send_force = spring_force + sine_force + const_force;

            ///Cap the maximum force 
            if (Mathf.Abs(send_force) > maxForce) send_force = Mathf.Sign(send_force) * maxForce;

            ///If the actuator position is invalid do not send a force
            if (EagleAPI.actuators[target_actuator].position > 500000) send_force = 0;
            ///If the position Control button is toggled on enable postion contoller if needed and send a position control command to the target position.
            if (positionControl)
            {
                if (pcenableflag)
                {
                    EagleAPI.actuators[target_actuator].EnablePositionControl(1);
                    pcenableflag = false;
                }
                EagleAPI.actuators[target_actuator].PositionControl(target_position);
            }
            ///If the force Control button is toggled on disabled the position controller if needed and send a force command to the actuator using the layered effects.
            else if (forceControl)
            {
                if (pcenableflag)
                {
                    EagleAPI.actuators[target_actuator].EnablePositionControl(0);
                    pcenableflag = false;
                }
                if (extended_servo_flag) { EagleAPI.actuators[target_actuator].ExtendedForce((int)send_force); extended_servo_flag = false; }
                else EagleAPI.actuators[target_actuator].Force((int)send_force);
            }
            ///If neither force conttrol of position control are toggled on send a 0 force so the acutator position is still streamed
            else EagleAPI.actuators[target_actuator].Force(0);
    //   }
        
    }

    /**
     * @brief Update the app's gui and take actions based on user interaction with gui.
     * Additional buttons can be added to easily send EagleAPI commands<br>
     * if (GUI.Button(new Rect(200, 200, 75, 20), "Reset Position"))    EagleAPI.actuators[target_actuator].ResetPosition();
     */
    private void OnGUI()
    {
        // ///Search for a serial port that has an eagle controller, stop and white light when correct port found
        switch (EagleAPI.correctPort)
        {
            case false:
                style.normal.textColor = Color.black;
                if ((Time.time - waiting_for_response) > 0.05)
                {
                    EagleAPI.Handshake();
                    waiting_for_response = Time.time;
                }
                break;
            case true:
                style.normal.textColor = Color.white;
                break;
        }

        ///Comm port that is being used by the Serial class
        GUI.Label(new Rect(Screen.width - 250, Screen.height -100, 250, 40), "Port:     " + Serial.GetPortName(), style);

        ///Send a system ready command
        if (GUI.Button(new Rect(50, 250, 150, 20), "System Ready")) EagleAPI.SystemReady();

        ///Actuator information, target acutator, position, actuator force, temperature etc.
        GUI.Label(new Rect(50, 50, 250, 40), "Target Actuator:               " + target_actuator.ToString());
        target_actuator = Mathf.RoundToInt(GUI.HorizontalSlider(new Rect(50, 30, 150, 20), target_actuator, 0, 5));
        if (target_actuator != last_target_actuator) pcenableflag = true;
        GUI.Label(new Rect(50, 75, 250, 20), "Actuator Position (mm):    " + (EagleAPI.actuators[target_actuator].position / 1000f).ToString("0.00")); //position is received in micrometers
        GUI.Label(new Rect(50, 100, 250, 20), "Actuator Force:                " + EagleAPI.actuators[target_actuator].force.ToString("0"));
        GUI.Label(new Rect(50, 125, 250, 20), "Errors:                            " + EagleAPI.actuators[target_actuator].errors.ToString());
        GUI.Label(new Rect(50, 150, 250, 20), "Temperature (C):              " + EagleAPI.actuators[target_actuator].temperature.ToString());
        GUI.Label(new Rect(50, 175, 250, 20), "Voltage (V):                     " + EagleAPI.actuators[target_actuator].voltage.ToString("0.00"));
        GUI.Label(new Rect(50, 200, 250, 20), "Power (W):                      " + EagleAPI.actuators[target_actuator].power.ToString("0.00"));

        ///Force Control section, spring constant and center, sine magnitude and frequency.
        if(forceControl = GUI.Toggle(new Rect(500, 50, 250, 20), forceControl, "ForceControl", "Button"))
        {
            if (positionControl) pcenableflag = true;
            positionControl = false;
        }
        GUI.Label(new Rect(300, 75, 250, 20), "Spring Constant(force/mm): " + springK.ToString("0.00"));
        springK = (GUI.HorizontalSlider(new Rect(500, 80, 250, 20), springK, 0, 5));
        GUI.Label(new Rect(300, 100, 250, 20), "Spring Center(mm):             " + springCenter.ToString("0.00"));
        springCenter = (GUI.HorizontalSlider(new Rect(500, 105, 250, 20), springCenter, 0, 200));
        GUI.Label(new Rect(300, 125, 250, 20), "Sine Magnitude:                  " + sine_mag.ToString("0.00"));
        sine_mag = (GUI.HorizontalSlider(new Rect(500, 130, 250, 20), sine_mag, 0, 50));
        GUI.Label(new Rect(300, 150, 250, 20), "Sine Frequency (Hz):           " + sine_freq.ToString("0"));
        sine_freq = (GUI.HorizontalSlider(new Rect(500, 155, 250, 20), sine_freq, 0, 50));
        GUI.Label(new Rect(300, 175, 250, 20), "Constant Force:                  " + const_force.ToString("0"));
        const_force = (GUI.HorizontalSlider(new Rect(500, 180, 250, 20), const_force, -30, 30));

        ///Position control sections
        if(positionControl = GUI.Toggle(new Rect(500, 250, 250, 20), positionControl, "Position Control", "Button"))
        {
            if (forceControl) pcenableflag = true;
            forceControl = false;
        }
        GUI.Label(new Rect(300,275, 250, 20), "Target Position(mm):          " + target_position.ToString("0"));
        target_position = (int)(GUI.HorizontalSlider(new Rect(500, 280,250, 20), target_position, 0, 150));
        
        GUI.Label(new Rect(50, Screen.height - 175, 150, 20), "Received: " + EagleAPI.lastResponse);
        GUI.Label(new Rect(50, Screen.height - 150, 150, 20), "Sent: " + EagleAPI.lastSent);
        ///General serial command sending box
        GUI.Label(new Rect(50, Screen.height - 100, 150, 20), "Send Serial Command");
        stringCommand = GUI.TextField(new Rect(50, Screen.height-75, 150, 20), stringCommand);
        if (GUI.Button(new Rect(200, Screen.height - 75, 75, 20), "Send"))
        {
            Serial.Write(stringCommand + "\r");
        }
    }
}
