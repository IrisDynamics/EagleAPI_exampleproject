//EagleAPI example usage
//see https://wiki.irisdynamics.com/index.php?title=Eagle_API for more infomation 

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EagleAPIexample : MonoBehaviour
{
    string stringCommand, forceArg; // TextField variables
    int target = 0;                 // Target actuator slider value

    string[] downstreamButtons = new string[] { "Force Command", "Extended Force Command","Actuator Polarity", "Actuator Info Request",
                                                "Temperature Request", "Sleep Request", "Wake Request" , "State Request", "Position Reset",
                                                "Available Actuators?", "Initialize"}; //buttons for downstream commands

    string[] upstreamLabels = new string[] { "Errors", "Available Actuators", "Selected Actuator ID",
                                             "Time of Last Response", "Force", "Position", "Temperature",
                                             "State", "Polarity", "Info" };   //labels identify upstream responses
    string[] upstreamTextFields;   //TextFields that will be updated information related to the target actuator


    //this function is called whenever a line is received
    //parses the eagle controller response
    void OnSerialLine(string line)
    {
        Debug.Log(line);                //Unity debug log is available along the bottom panel
        EagleAPI.Receive(line);         //parse the received line and update the Actuator information accordingly 
    }

    //this function updates the GUI and is called multiple times per frame
    void OnGUI()
    {
        //Target Actuator Selection Slider
        GUI.Box(new Rect(Screen.width / 2-50, 30, 100, 20), "Target Actuator");
        target = Mathf.RoundToInt(GUI.HorizontalSlider(new Rect(Screen.width / 2- 50, 20, 100, 20), target, 0, 2)); //only allow integers

        //General serial command sending box
        stringCommand = GUI.TextField(new Rect(50, 140, 100, 20), stringCommand);
        if (GUI.Button(new Rect(50, 180, 125, 20), "Send"))
        {
            Serial.WriteLn(stringCommand + "\r");
        }

        //Sending Downstream Commands

        //box to input desired force
        forceArg = GUI.TextField(new Rect(Screen.width / 2 - 250, 80, 50, 30), forceArg);

        //check if the argument in the text field is an integer
        int force = 0;
        int.TryParse(forceArg, out force);

        //when a button is pressed send send the appropriate downstream command
        for (int i = 0; i< downstreamButtons.Length; i++)
        {
            if (GUI.Button(new Rect(Screen.width / 2 - 200, 50 + i*40, 175, 20), downstreamButtons[i]))
            {
                switch (i)
                {
                    case 0: EagleAPI.actuators[target].Force(force); break;
                    case 1: EagleAPI.actuators[target].ExtendedForce(force); break;
                    case 2: EagleAPI.actuators[target].Polarity(); break;
                    case 3: EagleAPI.actuators[target].Info(); break;
                    case 4: EagleAPI.actuators[target].Temperature(); break;
                    case 5: EagleAPI.actuators[target].Sleep(); break;
                    case 6: EagleAPI.actuators[target].Wake(); break; 
                    case 7: EagleAPI.actuators[target].State(); break;
                    case 8: EagleAPI.actuators[target].ResetPosition(); break;
                    case 9: EagleAPI.Enumerate();break;
                    case 10: EagleAPI.Initialize(); break;
                }
            }     
        }

        ///Receiving upstream responses
        //update the text fields with the target actuator information and updates to the error and available actuators
        upstreamTextFields = new string[] { EagleAPI.error,
                                            EagleAPI.availableActuators,
                                            EagleAPI.actuators[target].actID.ToString(),
                                            EagleAPI.actuators[target].lastResponse.ToString(),
                                            EagleAPI.actuators[target].force.ToString(),
                                            EagleAPI.actuators[target].position.ToString(),
                                            EagleAPI.actuators[target].temperature.ToString(),
                                            EagleAPI.actuators[target].state,
                                            EagleAPI.actuators[target].polarity,
                                            EagleAPI.actuators[target].actuatorInfo };

        //show the information under the relevant label
        for (int i = 0; i < upstreamLabels.Length; i++)
        {
            GUI.Label(new Rect(Screen.width / 2 + 25, 50 + i * 40, 250, 20), upstreamLabels[i]);
            GUI.Label(new Rect(Screen.width / 2 + 25, 70 + i * 40, 200, 100), upstreamTextFields[i]);
        }
    }

    void OnApplicationQuit()
    {
        for(int i=0; i<EagleAPI.actuators.Length; i++)
        {
            EagleAPI.actuators[i].Sleep();
        }
    }
}
