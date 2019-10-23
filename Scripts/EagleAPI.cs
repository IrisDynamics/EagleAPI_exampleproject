/**@file EagleAPI.cs
 * This is the Eagle API library that allows easy sending and parsing of all EagleAPI commands
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**\class EagleAPI
 * Contains serial commands to send to Eagle Controller.
 */
static class EagleAPI
{
    public static string error;
    public static string availableActuators;
    public static Actuator[] actuators = new Actuator[8] {new Actuator(0), new Actuator(1), new Actuator(2),
                                                        new Actuator(3), new Actuator(4), new Actuator(5),
                                                        new Actuator(6), new Actuator(7)};
   

    /**initialize all actuators, this will configure and calibrate the actuators to their default settings
    *the Eagle Controller does this automatically on start up
    *this will also update the enumerated variable of the actuators
    */
    public static void Enumerate()
    {
        Serial.Write("[init" + "\r"); 
    }

    ///Handshake command used to confirm that the 
    public static void Handshake()
    {
        Serial.Write("[handshake\r");
    }
    ///Signals the actuators have been moved to their zero position and are ready to exert forces
    public static void SystemReady()
    {
        Serial.Write("[ready\r");
    }

    /**Parse responses from eagle controller
     * This function is called by the receivedData function of the Serial class
     * \param line Incomming serial line to be parsed
     */
    public static void Receive(string line) 
    {
        string[] parsed = line.Split(null);             ///serial line is space delimited
        string cmd = parsed[0];                         ///the first element is always the command identifier
        error = "";                                     ///reset the error when a new line is received
                                                        ///whenever a response is received with a given actuator ID that Actuator object is updated
        if (false) { }
        else if (cmd == "]response")                           /// If a handshake response is received set the correct port has been found.
        {
            Serial.correctPort = true;
        }
        else if (cmd == "]f")                           /// If a force response has been received update the relevant actuator parameters
        {
            
            try
            {
                int actID = int.Parse(parsed[1]);
                actuators[actID].force = int.Parse(parsed[2]);
                actuators[actID].position = long.Parse(parsed[3]);
                actuators[actID].lastResponse = Time.time;
                actuators[actID].enumerated = true;

            }
            catch (System.Exception)
            {
                Debug.LogError("BAD string: " + line.ToString());
            }
        }
        else if (cmd == "]pc")                           /// If a position control response has been received update the relevant actuator parameters
        {
            try
            {
                int actID = int.Parse(parsed[1]);
                actuators[actID].force = int.Parse(parsed[2]);
                actuators[actID].position = long.Parse(parsed[3]);
                actuators[actID].lastResponse = Time.time;
                actuators[actID].enumerated = true;

            }
            catch (System.Exception)
            {
                Debug.LogError(line.ToString());
            }
            

        }
        else if (cmd == "]exf")                         /// If an extended force response has been received update the relevant actuator parameters
        {
            try
            {
                int actID = int.Parse(parsed[1]);
                actuators[actID].force = int.Parse(parsed[2]);
                actuators[actID].position = long.Parse(parsed[3]);
                actuators[actID].errors = int.Parse(parsed[4]);
                actuators[actID].temperature = int.Parse(parsed[5]);
                actuators[actID].voltage = int.Parse(parsed[6]) / 1000f;    
                actuators[actID].power = int.Parse(parsed[7]);
                actuators[actID].lastResponse = Time.time;
                actuators[actID].enumerated = true;

            }
            catch (System.Exception)
            {
                Debug.LogError(line.ToString());
            }
            
            
        }
        else if (cmd == "]sleep")                      /// If a sleep response has been received update the relevant actuator parameters
        {
            try
            {
                int actID = int.Parse(parsed[1]);
                actuators[actID].lastResponse = Time.time;
                actuators[actID].enumerated = true;
            }
            catch (System.Exception)
            {
                Debug.LogError(line.ToString());
            }
            
        }
        else if (cmd == "]wake")                       /// If a wake response has been received update the relevant actuator parameters
        {
            try
            {
                int actID = int.Parse(parsed[1]);
                actuators[actID].lastResponse = Time.time;
                actuators[actID].enumerated = true;
            }
            catch (System.Exception)
            {
                Debug.LogError(line.ToString());
            }
        }
        else if (cmd == "]pol")                       /// If a polarity response has been received update the relevant actuator parameters
        {

            try
            {
                int actID = int.Parse(parsed[1]);
                actuators[actID].lastResponse = Time.time;
                actuators[actID].polarity = int.Parse(parsed[2]);
                actuators[actID].enumerated = true;
            }
            catch (System.Exception)
            {
                Debug.LogError(line.ToString());
            }
        }
        else if (cmd == "]rp")                        /// If a reset position response has been received update the relevant actuator parameters
        {
            try
            {
                int actID = int.Parse(parsed[1]);
                actuators[actID].lastResponse = Time.time;
                actuators[actID].enumerated = true;
            }
            catch (System.Exception)
            {
                Debug.LogError(line.ToString());
            }

        }
        else if (cmd == "]t")                         /// If a temperature response has been received update the relevant actuator parameters
        {
            try
            {
                int actID = int.Parse(parsed[1]);
                actuators[actID].lastResponse = Time.time;
                actuators[actID].temperature = float.Parse(parsed[2]);
                actuators[actID].enumerated = true;
            }
            catch (System.Exception)
            {
                Debug.LogError(line.ToString());
            }
            
        }
        else if (cmd == "]info")                     /// If a info response has been received update the relevant actuator parameters
        {
            int actID = int.Parse(parsed[1]);
            actuators[actID].lastResponse = Time.time;
            actuators[actID].actuatorInfo = "";
            for (int i = 2; i < parsed.Length; i++)
            {
                actuators[actID].actuatorInfo += parsed[i] + "\n";
            }
            actuators[actID].enumerated = true;
        }
        else if (cmd == "]invalid_act")             /// The targetted actuator is not enumerated.                                                    */
        {
            error = "Target actuator " + parsed[1] + " not available";
            int actID;
            if (int.TryParse(parsed[1], out actID))
            {
                if ((actID>=0) && (actID < actuators.Length))
                {
                    actuators[actID].enumerated = false;
                }
                
            }
            
        }
        else if (cmd == "]invalid_arg")            /// The argument sent with the command was invalid
        {
            error = parsed[3] + "is not a valid argument for" + parsed[2] + "command";
        }
        else if (cmd == "]init")
        {
            for (int i=1; i < (parsed.Length); i++)
            {
                int actID;
                if (int.TryParse(parsed[i], out actID)){
                    actuators[actID].enumerated = true;
                }
            }
        }

        availableActuators = "";                   //Update the list of enumerated actuators
        for(int i = 0; i<actuators.Length; i++)
        {
            if (actuators[i].enumerated)
            {
                availableActuators += i.ToString() + " ";
            }
        }
    }
}

/**\class Actuator 
 * Object that is used to keep track of infomation pertaining to a specific actuator
 * This object is updated from the EagleAPI Receive function
 */
class Actuator
{
    public int actID, force, polarity, errors;
    public long position;
    public float temperature, voltage, power, lastResponse;
    public string actuatorInfo;
    public bool enumerated;
    /**Constructor
     * \param actuatorID This is the id used by the eagle controller as described in the Eagle Controller Reference Manual
     */
    public Actuator(int actuatorID)  
    {
        actID = actuatorID;
    }

    /**Send a Force Command 
     * This will be overriden if position control is enabled
     * \param forceArg Magnitude of the force command between -220 and 220
     */
    public void Force(int forceArg=0)         
    {
        Serial.Write("[f " + actID.ToString() + " " + forceArg.ToString() + "\r");
    }
    /**Send an Exteneded Force Command 
     * This will be overriden if position control is enabled
     * \param forceArg Magnitude of the force command between -220 and 220
     */
    public void ExtendedForce(int forceArg=0)  
    {
        Serial.Write("[exf " + actID + " " + forceArg + "\r");
    }
    ///Send a sleep command to disabled acutator
    public void Sleep()                        
    {
        Debug.Log("sleep");
        Serial.Write("[sleep " + actID + "\r");
    }
    ///Send a wake command to enable actuator that has been disabled (automatically enabled on start up)
    public void Wake()                         
    {
        Serial.Write("[wake " + actID + "\r");
    }

    /**Send a polarity command to change the direction of increasing position/positive force
     * \param pol Either positive polarity (0) or negative polarity(1)
     */
    public void Polarity(int pol)                     
    {
        Serial.Write("[pol " + actID + " " + pol+ "\r");
    }
    /**Semd a reset position command to make the current position zero
     */
    public void ResetPosition()              
    {
        Serial.Write("[rp " + actID + "\r");
    }
    /**Send a position control command
     * \param target_pos Target position in millimeters from the zero position
     */
    public void PositionControl(int target_pos)               
    {
        Serial.Write("[pc " + actID + " " + target_pos+  "\r");
    }

    /**Send an enabled position control command
     * The position controller must me tuned in iris controls before it will track positions
     * \param enable Either enable(1) or disable (0) the position controller
     */
    public void EnablePositionControl(int enable) { 
        Serial.Write("[pcen " + actID + " " + enable + "\r");
    }

    /**Send a temperature command
     */
    public void Temperature()                
    {
        Serial.Write("[t " + actID + "\r");
    }

    /**Send a clear errors command (not available on all eagle controllers)
     */
    public void ClearErrors()                 
    {
        Serial.Write("[ce " + actID + "\r");
    }
    /**Send a get info command to find out about the actuator's build and serial numbers.
     */
    public void Info()                       
    {
        Serial.Write("[info " + actID + "\r");
    }
}