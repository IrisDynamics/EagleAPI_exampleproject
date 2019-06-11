//EagleAPI example usage
//see https://wiki.irisdynamics.com/index.php?title=Eagle_API for more infomation 

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EagleAPIexample : MonoBehaviour
{
    string cmd, stringCommand, forceArg, responseReceivedTime, force, position, temperature, polarity, state, actuatorInfo, activeActuator, error;
    string[] parsed;
    int actuatorID = 1;
    //string[] downstreamTextFields = new string[] { };
    string[] downstreamButtons = new string[] { "Force Command", "Extended Force Command","Actuator Polarity", "Actuator Info Request", "Temperature Request",
                                                "Sleep Request", "Wake Request" , "State Request", "Position Reset"};
    string[] upstreamLabels = new string[] { "Time of Last Response","Error", "Active Actuator", "Force", "Position", "Temperature", "State", "Polarity", "Info" };
    string[] upstreamTextFields;

    //this function is called whenever a line is received
    //parses the eagle controller response
    void OnSerialLine(string line)
    {
        error = "";
        responseReceivedTime = Time.time.ToString();
        Debug.Log(line);
        parsed = line.Split(null);
        cmd = parsed[0];
        if (false) {}
        else if (cmd == ">f")
        {
            activeActuator = parsed[1];
            force = parsed[2];
            position = parsed[3];
        }
        else if (cmd == ">exf")
        {
            activeActuator = parsed[1];
            force = parsed[2];
            position = parsed[3];
            temperature = parsed[4];
        }
        else if (cmd == ">sleep")
        {
            activeActuator = parsed[1];
        }
        else if (cmd == ">wake")
        {
            activeActuator = parsed[1];
        }
        else if (cmd == ">pol")
        {
            activeActuator = parsed[1];
            polarity = parsed[2];
        }
        else if (cmd == ">rp")
        {
            activeActuator = parsed[1];
        }
        else if (cmd == ">t")
        {
            activeActuator = parsed[1];
            temperature = parsed[2];
        }
        else if (cmd == ">state")
        {
            activeActuator = parsed[1];
            state = parsed[2];
        }
        else if (cmd == ">info")
        {
            activeActuator = parsed[1];
            actuatorInfo = "";
            for (int i = 2; i<parsed.Length; i++)
            {
                actuatorInfo += parsed[i] + "\n";
            }
        }
        else if (cmd == ">invalid_act")
        {
            error = "Target actuator "+parsed[1]+ " not available";
        }
        else if (cmd == ">invalid_arg")
        {
            error = parsed[3] + "is not a valid argument for" + parsed[2] + "command";
        }
    }

    //this function updates the gui 
    void OnGUI()
    {
        //Target Actuator Selection Slider
        GUI.Box(new Rect(Screen.width / 2-50, 30, 100, 20), "Target Actuator");
        actuatorID = Mathf.RoundToInt(GUI.HorizontalSlider(new Rect(Screen.width / 2- 50, 20, 100, 20), actuatorID, 1, 3));

        //General serial command sending box
        stringCommand = GUI.TextField(new Rect(50, 140, 100, 20), stringCommand);
        if (GUI.Button(new Rect(50, 180, 125, 20), "Send"))
        {
            Serial.WriteLn(stringCommand + "\r");
        }

        GUI.Label(new Rect(Screen.width / 2 - 150, 50, 100, 20), "Downstream");
        GUI.Label(new Rect(Screen.width / 2 + 100, 50, 100, 20), "Upstream");

        //Sending Downstream Commands

        forceArg = GUI.TextField(new Rect(Screen.width / 2 - 250, 110, 50, 30), forceArg);

        for (int i = 0; i< downstreamButtons.Length; i++)
        {
            if (GUI.Button(new Rect(Screen.width / 2 - 200, 100 + i*40, 175, 20), downstreamButtons[i]))
            {
                switch (i)
                {
                    case 0: Serial.WriteLn("<f " + actuatorID + " " + forceArg + "\r"); break;
                    case 1: Serial.WriteLn("<exf " + actuatorID + " " + forceArg + "\r"); break;
                    case 2: Serial.WriteLn("<pol " + actuatorID + " " + "\r"); break;
                    case 3: Serial.WriteLn("<info " + actuatorID + " " + "\r"); break;
                    case 4: Serial.WriteLn("<t " + actuatorID + " " + "\r"); break;
                    case 5: Serial.WriteLn("<sleep " + actuatorID + " " + "\r"); break;
                    case 6: Serial.WriteLn("<wake " + actuatorID + " " + "\r"); break;
                    case 7: Serial.WriteLn("<state " + actuatorID + " " + "\r"); break;
                    case 8: Serial.WriteLn("<rp " + actuatorID + "\r"); break;
                }
            }     
        }

        //Receiving upstream responses
        upstreamTextFields = new string[] { responseReceivedTime,error, activeActuator, force, position, temperature, state, polarity, actuatorInfo };
        for (int i=0; i<upstreamLabels.Length; i++)
        {
            GUI.Label(new Rect(Screen.width / 2 + 25, 100 + i*40, 250, 20), upstreamLabels[i]);
            GUI.Label (new Rect(Screen.width / 2 + 25, 120 + i * 40, 200, 100), upstreamTextFields[i]);
        }
    }
}
