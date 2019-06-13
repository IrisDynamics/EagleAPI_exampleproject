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

    //check find out which actuators are availble without reinitializing them
    public static void Enumerate()
    {
        for (int i =0; i<actuators.Length; i++)
        {
            actuators[i].Sleep();
        }
    }

    //initialize all actuators, this will configure and calibrate the actuators to their default settings
    //the Eagle Controller does this automatically on start up
    //this will also update the enumerated variable of the actuators
    public static void Initialize()
    {
        Serial.WriteLn("<init" + "\r");
    }

    //parse upstream responses
    //call this function inside OnSerialLine(string line)
    public static void Receive(string line) 
    {
        string[] parsed = line.Split(null);             //serial line is space delimited
        string cmd = parsed[0];                         //the first element is always the command identifier
        error = "";                                     //reset the error when a new line is received
                                                        //whenever a response is received with a given actuator ID that Actuator object is updated
        if (false) { }
        else if (cmd == ">f")                           // an upstream force response has been received
        {
            int actID = int.Parse(parsed[1]);           
            actuators[actID].force = int.Parse(parsed[2]);
            actuators[actID].position = int.Parse(parsed[3]);
            actuators[actID].lastResponse = Time.time;
            actuators[actID].enumerated = true;
        }
        else if (cmd == ">exf")                          // an upstream force response has been received
        {
            int actID = int.Parse(parsed[1]);
            actuators[actID].force = int.Parse(parsed[2]);
            actuators[actID].position = int.Parse(parsed[3]);
            actuators[actID].temperature =float.Parse(parsed[3]);
            actuators[actID].lastResponse = Time.time;
            actuators[actID].enumerated = true;
        }
        else if (cmd == ">sleep")                      // an upstream sleep response has been received
        {
            int actID = int.Parse(parsed[1]);
            actuators[actID].lastResponse = Time.time;
            actuators[actID].enumerated = true;
        }
        else if (cmd == ">wake")                       // an upstream wake response has been received
        {
            int actID = int.Parse(parsed[1]);
            actuators[actID].lastResponse = Time.time;
            actuators[actID].enumerated = true;
        }
        else if (cmd == ">pol")                       // an upstream polarity response has been received
        {
            int actID = int.Parse(parsed[1]);
            actuators[actID].lastResponse = Time.time;
            actuators[actID].polarity = parsed[2];
            actuators[actID].enumerated = true;
        }
        else if (cmd == ">rp")                        // an upstream reset position response has been received
        {
            int actID = int.Parse(parsed[1]);
            actuators[actID].lastResponse = Time.time;
            actuators[actID].enumerated = true;
        }
        else if (cmd == ">t")                         //an upstream temperature response has been received
        {
            int actID = int.Parse(parsed[1]);
            actuators[actID].lastResponse = Time.time;
            actuators[actID].temperature = float.Parse(parsed[2]);
            actuators[actID].enumerated = true;
        }
        else if (cmd == ">state")                    // an upstream state response has been received
        {
            int actID = int.Parse(parsed[1]);
            actuators[actID].lastResponse = Time.time;
            actuators[actID].state = parsed[2];
            actuators[actID].enumerated = true;
        }
        else if (cmd == ">info")                     // an upstream info response has been received
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
        else if (cmd == ">invalid_act")             // The targetted actuator is not enumerated.
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
        else if (cmd == ">invalid_arg")            // The argument sent with the command was invalid
        {
            error = parsed[3] + "is not a valid argument for" + parsed[2] + "command";
        }
        else if (cmd == ">init")
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
    public int actID, force, position;
    public float temperature, lastResponse;
    public string state, polarity, actuatorInfo;
    public bool enumerated;
    public Actuator(int actuatorID)  //Constructor
    {
        actID = actuatorID;
    }

    public void Force(int forceArg=0)         //send a downstream Force Command (actuator must first be enabled with Wake command to output a force)
    {
        Serial.WriteLn("<f " + actID + " " + forceArg + "\r");
    }

    public void ExtendedForce(int forceArg=0)  //send a downstream Extended Force Command (actuator must first be enabled to output a force)
    {
        Serial.WriteLn("<exf " + actID + " " + forceArg + "\r");
    }

    public void Sleep()                         //send a downstream Sleep Command (disable actuator forces)
    {
        Serial.WriteLn("<sleep " + actID + "\r");
    }

    public void Wake()                          //send a downstream Wake Command (enable actuator forces)
    {
        Serial.WriteLn("<wake " + actID + "\r");
    }

    public void Polarity()                     //send a downstream Polarity Command (change the positive direction)
    {
        Serial.WriteLn("<pol " + actID + "\r");
    }

    public void ResetPosition()               //send a downstream Reset Position Command (set the current position to 0)
    {
        Serial.WriteLn("<rp " + actID + "\r");
    }

    public void Temperature()                 //send a downstream Temperature Request (ask for the temperature in Celcius)
    {
        Serial.WriteLn("<t " + actID + "\r");
    }

    public void State()                       //send a downstream State Request (Disabled, Enabled, Calibration, Servo, Extended_Servo Info_and_Config
    {
        Serial.WriteLn("<state " + actID + "\r");
    }

    public void Info()                        //send a downstream Info Request to receive configuration and ID information
    {
        Serial.WriteLn("<info " + actID + "\r");
    }
}