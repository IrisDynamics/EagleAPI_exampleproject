using System.Collections;
using System.Collections.Generic;
using UnityEngine;


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

    public static void SystemReady()
    {
        Serial.Write("[ready\r");
    }

    public static void CheckCorrectPort()
    {
        if (Serial.correctPort)
        {
            Debug.Log("port success");
        }
        else
        {
            Debug.Log("increment Port");
            Serial.s_serial = null;
            Serial.portAttempt++;
            Serial.checkOpen();
        }
    }
    //parse upstream responses
    //call this function inside OnSerialLine(string line)
    public static void Receive(string line) 
    {
        //eDebug.Log(line);
        string[] parsed = line.Split(null);             //serial line is space delimited
        string cmd = parsed[0];                         //the first element is always the command identifier
        error = "";                                     //reset the error when a new line is received
                                                        //whenever a response is received with a given actuator ID that Actuator object is updated
        if (false) { }
        else if (cmd == "]response")                           // an upstream handshake response
        {
            Serial.correctPort = true;
            Debug.Log("response");
        }
        else if (cmd == "]f")                           // an upstream force response has been received
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
        else if (cmd == "]pc")                           // an upstream force response has been received
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
        else if (cmd == "]exf")                          // an upstream force response has been received
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
        else if (cmd == "]sleep")                      // an upstream sleep response has been received
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
        else if (cmd == "]wake")                       // an upstream wake response has been received
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
        else if (cmd == "]pol")                       // an upstream polarity response has been received
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
        else if (cmd == "]rp")                        // an upstream reset position response has been received
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
        else if (cmd == "]t")                         //an upstream temperature response has been received
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
        else if (cmd == "]info")                     // an upstream info response has been received
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
        else if (cmd == "]invalid_act")             // The targetted actuator is not enumerated.
                                                    // check the string EagleAPI.availableActuators for a list of enumerated actuator ID
                                                    // to update the list of available actuators run EagleAPI.Enumerate() or EagleAPI.Initialize()
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
        else if (cmd == "]invalid_arg")            // The argument sent with the command was invalid
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

class Actuator
{
    public int actID, force, polarity, errors;
    public long position, velocity, last_position;
    public float temperature, voltage, power, lastResponse;
    public string state, actuatorInfo;
    public bool enumerated;
    public Actuator(int actuatorID)  //Constructor
    {
        actID = actuatorID;
    }

    public void Force(int forceArg=0)         //send a downstream Force Command (actuator must first be enabled with Wake command to output a force)
    {
        Serial.Write("[f " + actID.ToString() + " " + forceArg.ToString() + "\r");
    }

    public void ExtendedForce(int forceArg=0)  //send a downstream Extended Force Command (actuator must first be enabled to output a force)
    {
        Serial.Write("[exf " + actID + " " + forceArg + "\r");
    }

    public void Sleep()                         //send a downstream Sleep Command (disable actuator forces)
    {
        Debug.Log("sleep");
        Serial.Write("[sleep " + actID + "\r");
    }

    public void Wake()                          //send a downstream Wake Command (enable actuator forces)
    {
        Serial.Write("[wake " + actID + "\r");
    }

    public void Polarity(int pol)                     //send a downstream Polarity Command (change the positive direction)
    {
        Serial.Write("[pol " + actID + " " + pol+ "\r");
    }

    public void ResetPosition()               //send a downstream Reset Position Command (set the current position to 0)
    {
        Serial.Write("[rp " + actID + "\r");
    }

    public void PositionControl(int target_pos)               //send a downstream Reset Position Command (set the current position to 0)
    {
        Serial.Write("[pc " + actID + " " + target_pos+  "\r");
    }

    public void EnablePositionControl(int enable)               //send a downstream Reset Position Command (set the current position to 0)
    {
        Serial.Write("[pcen " + actID + " " + enable + "\r");
    }

    public void Temperature()                 //send a downstream Temperature Request (ask for the temperature in Celcius)
    {
        Serial.Write("[t " + actID + "\r");
    }

    public void ClearErrors()                 //send a downstream clear error request, will clear any errors that are no longer present
    {
        Serial.Write("[ce " + actID + "\r");
    }

    public void Info()                        //send a downstream Info Request to receive configuration and ID information
    {
        Serial.Write("[info " + actID + "\r");
    }
}