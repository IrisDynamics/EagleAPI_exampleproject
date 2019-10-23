/**@file ActuatorDemoApp.cs
 * 
 *Control actuators using EagleAPI
 * @mainpage Introduction
 * This is an app used to control the actuators force using sliders.
 * Communication to an Eagle controller over usb using EagleAPI protocol.
 * @section Usage
 * Ensure an eagle controller is connected to a usb port and powered.
 * When started, the app will search for an eagle device over the connected comms port until it receives a }response reply.
 * When connected to the right port, the CommPort will turn green.
 * The slider at the top right will select an actuator to target, these numbers are based on how the actuators are enumerated by the eagle controller.
 * Before sending forces, ensure the actuator has been enabled by sending an enable command. To test that the actuator is outputing forces as expected you can use the 
 * Force -15 button to send a small output force once. 
 * When the enableeffects button is pressed, this app will start streaming force commands of the amount specified by the sending force, to the target actuator.
 * The commands are streamed at 1kHz, the number of packets per second as well as the force calculation timestep is noted.
 * The actuator's position in millimeters is availble under the target actuator, note this is only updated when enbale effects is turned on.
 * If you would like to stream position without sending forces, either set all effects sliders to 0, or ensure the actuator is disabled, before pressing enableeffects.
 * Ensure the actuator's shaft has been moved through it's range of motion before enabling forces on a new powerup. This will ensure that the position reading is accurate
 * and a reasonable zero position is established.
 * 
 * There are two main effects available, a spring effect and a sinusoidal force., todo second sine wave for layering
 * 
 * Effects Sliders
 * - Spring Constant
 * This is the spring rate higher values indicate stronger spring force/mm (since force is not a defined unit)
 * 
 * - Spring Center
 * This is the center position for spring force calculations, the center of a dca series shaft is at approximately 75mm
 * 
 * - Sine Magnitude
 * This is the magnitdute of the sine wave, ie the force at the peaks,
 * 
 * - Sine Frequency
 * Frequency of sine wave oscillation in Hz
 */

using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

/**\class ActuatorDemoApp
 * Layer various feedback effects onto actuators using sliders.
 * 
 * Uses the Serial.cs script on same game object and uses EagleAPI to send and parse commands.
 */
public class ActuatorDemoApp : MonoBehaviour
{
    int target_actuator = 0;    //!< Actuator to send commands to 
    long actuator_position;  //!< Target actuator's last received position
    float send_force;       //!< Combined effects force used to send to the actuator (is limited by max force)
    float spring_force;     //!< force due to spring effect
    float sine_force;       //!< force due to sine wave effect
    float springCenter = 0; //!< center of spring in mm, used in determining spring force
    float springK = 0;      //!< spring constant in force/mm,  used in determining spring force
    float sine_mag = 0;     //!< magnitude of sine wave, used in determining sine force
    float sine_freq = 0;    //!< frequency of sine wave force effect
    float const_force = 0;   //!< Constant force to send actuator
    int maxForce = 150;      //!< maximum force to be commanded to the actuator.
    string[] command_buttons = new string[] {"System Ready",
                                             "Force",
                                             "Extended Force",
                                             "Position Control",
                                             "Enable Position Control",
                                             "Sleep",
                                             "Wake",
                                             "Polarity",
                                             "Reset Position",
                                             "Temperature",
                                             "Info",
                                             "Enumerate"}; //!<button names for commands
    int[] argument_slider = new int[12];//!<EagleAPI command arguments
    bool enableEffects = true;     //!< enable the effects to the actuator (i.e. start streaming force commands with effects
    DateTime referenceTime;     //!< date time object for the current time 
    DateTime startTime; //!< time at start of app
    float last_second;     //!<Time when the last second calculation was performed used to calculate packets per second
    float last_step;    //!< Time at the last fixed time step  used to calculate timestep length
    long elapsedTicks;  //!< used to determine the change in time where ticks are 100 nanaseconds.
    int packets_per_second = 0; //!< number of serial writes used per second, might not be accurate representation of packets per second
    float time_step = 0;    //!< Calculated time that a fixed timestep takes for force calculations
    float waiting_for_response;    //!< Time that a handshake command was sent if this is longer than a certain amount of time the port is abandonned and a new port is attempted
    GUIStyle style = new GUIStyle();    //!< Used to change the comms to green when successful port is found.
    bool extended_servo_flag = false;
    string stringCommand;
    int target_position = 0;
    bool positionControl = false;
    bool pcenableflag = true;
    /** Start is called before the first frame update
     * Starts the invoking of the function that will calculate the actuator physics
     */
    void Start()
    {
        Screen.SetResolution(800,600, false);
        last_second = Time.time;
        style.normal.textColor = Color.white;
        EagleAPI.Handshake();
        waiting_for_response = Time.time;
    }

    /** Updates at a fixed timestep. 
     * updates the actuator position and uses it to perform physics calculations for effects
     */
    void FixedUpdate()
    {
        referenceTime = DateTime.Now;
        elapsedTicks = referenceTime.Ticks - startTime.Ticks;
        time_step = (Time.time - last_step);
        last_step = Time.time;
        ///check how many packets have been sent in the last second.
        if ((Time.time - last_second) > 1)
        {
            last_second = Time.time;
            packets_per_second = Serial.packets;
            Serial.packets = 0;
            extended_servo_flag = true;
        }
        ///calculate spring force
        spring_force = -(EagleAPI.actuators[target_actuator].position - springCenter*1000f) * springK/1000f;

        ///calculate sine force
        sine_force = sine_mag * Mathf.Sin(2 * Mathf.PI * sine_freq * Time.time);  //(elapsedTicks * 100f/1000f/1000f/1000f)

        ///layer effects
        send_force = spring_force + sine_force + const_force;// + damping_force;

        if (Mathf.Abs(send_force) > maxForce) send_force = Mathf.Sign(send_force) * maxForce;
        if (EagleAPI.actuators[target_actuator].position > 500000) send_force = 0;
        ///send forces to the actuator if effects are not enabled still send 0 force so that a position response is returned
        if (positionControl)
        {
            if (pcenableflag)
            {
                EagleAPI.actuators[target_actuator].EnablePositionControl(1);
                pcenableflag = false;
            }
            EagleAPI.actuators[target_actuator].PositionControl(target_position);
        }
        if (enableEffects)
        {
            if (pcenableflag)
            {
                EagleAPI.actuators[target_actuator].EnablePositionControl(0);
                pcenableflag = false;
            }
            if (extended_servo_flag) { EagleAPI.actuators[target_actuator].ExtendedForce((int)send_force); extended_servo_flag = false; }
            else EagleAPI.actuators[target_actuator].Force((int)send_force);
        }
    }

    /**
     * Update the app's gui and take actions based on user interaction with gui
     */
    private void OnGUI()
    {
        //GUI.Label(new Rect(300, 20, 200, 20), "EagleAPI Single Command");
        /////Populate the buttons and send appropriate command on button press
        //for (int i = 0; i < command_buttons.Length; i++)
        //{
        //    if (GUI.Button(new Rect(300, 50 + i * 40, 150, 20), command_buttons[i]))
        //    {
        //        switch (i)
        //        {
        //            case 0: EagleAPI.SystemReady(); break;
        //            case 1: EagleAPI.actuators[target_actuator].Force(0);  break;
        //            case 2: EagleAPI.actuators[target_actuator].ExtendedForce(0); break;
        //            case 3: EagleAPI.actuators[target_actuator].PositionControl(0);break;
        //            case 4: EagleAPI.actuators[target_actuator].EnablePositionControl(1);break;
        //            case 5: EagleAPI.actuators[target_actuator].Sleep();break;
        //            case 6: EagleAPI.actuators[target_actuator].Wake(); break;
        //            case 7: EagleAPI.actuators[target_actuator].Polarity(1); break;
        //            case 8: EagleAPI.actuators[target_actuator].ResetPosition(); break;
        //            case 9: EagleAPI.actuators[target_actuator].Temperature(); break;
        //            case 10: EagleAPI.actuators[target_actuator].Info(); break;
        //            case 11: EagleAPI.Enumerate();  break;
        //        }
        //    }
        //}
        //GUI.Label(new Rect(500, 20, 200, 20), "Argument");
        //for (int i = 0; i < argument_slider.Length; i++)
        //{
        //    argument_slider[i] = (int)GUI.HorizontalSlider(new Rect(500, 55 + i * 40, 150, 20), (float)argument_slider[i], 0, 1);

        //}
        ///Search for a serial port that has an eagle controller, stop and green light when correct port found
        switch (Serial.correctPort)
        {
            case false:
                EagleAPI.Handshake();
                if ((Time.time - waiting_for_response) > 0.1)
                {
                    Debug.Log("increment Port");
                    Serial.s_serial = null;
                    Serial.portAttempt++;
                    Serial.checkOpen();
                    EagleAPI.Handshake();
                    waiting_for_response = Time.time;
                }
                style.normal.textColor = Color.black;
                break;
            case true:
                style.normal.textColor = Color.white;
                break;
        }

        ///Unity info, com port, packets send per second, force calculation timestep
        GUI.Label(new Rect(Screen.width - 250, Screen.height -100, 250, 40), "CommPort:     " + Serial.port_name, style);

        ///Actuator information, target acutator, position, force being sent
        GUI.Label(new Rect(50, 50, 250, 40), "Target Actuator:               " + target_actuator.ToString());
        target_actuator = Mathf.RoundToInt(GUI.HorizontalSlider(new Rect(50, 30, 150, 20), target_actuator, 0, 5)); //Target Actuator slider 
        GUI.Label(new Rect(50, 75, 250, 20), "Actuator Position (mm):    " + (EagleAPI.actuators[target_actuator].position / 1000f).ToString("0.00"));
        GUI.Label(new Rect(50, 100, 250, 20), "Actuator Force:                " + EagleAPI.actuators[target_actuator].force.ToString("0"));
        GUI.Label(new Rect(50, 125, 250, 20), "Errors:                            " + EagleAPI.actuators[target_actuator].errors.ToString());
        GUI.Label(new Rect(50, 150, 250, 20), "Temperature (C):              " + EagleAPI.actuators[target_actuator].temperature.ToString());
        GUI.Label(new Rect(50, 175, 250, 20), "Voltage (V):                     " + EagleAPI.actuators[target_actuator].voltage.ToString("0.00"));
        GUI.Label(new Rect(50, 200, 250, 20), "Power (W):                      " + EagleAPI.actuators[target_actuator].power.ToString("0.00"));
        ///Effects information, spring constant and center, sine magnitude and frequency.
        if(enableEffects = GUI.Toggle(new Rect(500, 50, 250, 20), enableEffects, "ForceControl", "Button"))
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

        ///Effects information, spring constant and center, sine magnitude and frequency.
        if(positionControl = GUI.Toggle(new Rect(500, 250, 250, 20), positionControl, "Position Control", "Button"))
        {
            if (enableEffects) pcenableflag = true;
            enableEffects = false;
        }
        GUI.Label(new Rect(300,275, 250, 20), "Target Position(mm):          " + target_position.ToString("0"));
        target_position = (int)(GUI.HorizontalSlider(new Rect(500, 280,250, 20), target_position, 0, 150));
        
        ///General serial command sending box
        GUI.Label(new Rect(50, Screen.height - 100, 150, 20), "Send Serial Command");
        stringCommand = GUI.TextField(new Rect(50, Screen.height-75, 150, 20), stringCommand);
        if (GUI.Button(new Rect(200, Screen.height - 75, 75, 20), "Send"))
        {
            Serial.Write(stringCommand + "\r");
        }
    }
}
