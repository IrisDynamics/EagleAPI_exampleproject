﻿//This script is used to control the table rotation with two actuators to keep the "Ball" game object from falling off
//this script should be added to a table game object
//In the comments, there is alternative code that does not use the EagleAPI library

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class tableControl : MonoBehaviour
{
    int position1, force1, maxPosition1;
    int position2, force2, maxPosition2;
    int midPosition1 = 10000, midPosition2 = 10000, forceScale = 10; 
    float positionToAngle = 0.005f, springK = 0.003f, starttime;
    Vector3 ballposition;
    public bool autoForces = true;
    bool homed = false;

    // Start is called before the first frame update
    void Start()
    {
        //Enable the actuators
        EagleAPI.actuators[0].Wake();                   // Serial.WriteLn("<wake " + 0 + "\r");
        EagleAPI.actuators[1].Wake();                   // Serial.WriteLn("<wake " + 1 + "\r");
        starttime = Time.time;
    }
    // Update is called once per frame
    void FixedUpdate()
    {
        if (Home())
        {
            ballposition = GameObject.Find("Ball").transform.position;                           //get the position of the Ball
            force1 = (int)(ballposition.x * forceScale - (position1 - midPosition1) * springK);
            force2 = (int)(-ballposition.z * forceScale - (position2 - midPosition2) * springK); //make negative due to coordinate system orientation
            //if automatic forces are off don't send the actuators a force
            if (!autoForces)
            {
                force1 = 0;
                force2 = 0;
            }
            EagleAPI.actuators[0].Force(force1);           // Serial.WriteLn("<f " + 0 + " " + force1 + "\r");
            EagleAPI.actuators[1].Force(force2);           // Serial.WriteLn("<f " + 1 + " " + force2 + "\r");
            //change the table angle based on the actuators's position
            transform.eulerAngles = new Vector3((position2 - midPosition2) * positionToAngle, 0, (position1 - midPosition1) * positionToAngle);
        }
    }
    void OnSerialLine(string line)
    {
        EagleAPI.Receive(line);                            //parse the incoming EagleAPI responses
        position1 = EagleAPI.actuators[0].position;        //update the actuator positions
        position2 = EagleAPI.actuators[1].position;
        //string[] parsed = line.Split(null);
        //if (parsed[0] == ">f")
        //{
        //    switch (int.Parse(parsed[1]))
        //    {
        //        case 1: position1 = int.Parse(parsed[3]); break;
        //        case 2: position2 = int.Parse(parsed[3]); break;
        //    }
        //}
    }
    bool Home()
    {
        int homingForce = 20;
        int positionFactor = 200;
        //only perform homing if the actuator has not been homed and automatic actuator forces are on
        if (homed) return true;
        else if (autoForces)
        {
            //start by moving the actuators to their minimum position
            if ((Time.time - starttime) < 2)
            {
                EagleAPI.actuators[0].Force(-homingForce);           //Serial.WriteLn("<f " + 0 + " " + -homingForce + "\r");
                EagleAPI.actuators[1].Force(-homingForce);           //Serial.WriteLn("<f " + 1 + " " + -homingForce + "\r");
            }
            //then move them to their maximum position
            else if ((Time.time - starttime) < 4)
            {
                EagleAPI.actuators[0].Force(homingForce);            //Serial.WriteLn("<f " + 0 + " " + homingForce + "\r");
                EagleAPI.actuators[1].Force(homingForce);            //Serial.WriteLn("<f " + 1 + " " + homingForce + "\r");
                maxPosition1 = position1;
                maxPosition2 = position2;
            }
            //move the actuators to calculated midpoint position
            else
            {
                midPosition1 = maxPosition1 / 2;
                midPosition2 = maxPosition2 / 2;
                if (Mathf.Abs(position1 - midPosition1) > positionFactor)
                {
                    int force = (midPosition1 - position1) / positionFactor;
                    EagleAPI.actuators[0].Force(force);              //Serial.WriteLn("<f " + 0 + " " + force + "\r");
                }
                else
                {
                    EagleAPI.actuators[0].Force();                   //Serial.WriteLn("<f " + 0 + " " + 0 + "\r");
                }

                if (Mathf.Abs(position2 - midPosition2) > positionFactor)
                {
                    int force = (midPosition2 - position2) / positionFactor;
                    EagleAPI.actuators[1].Force(force);              //Serial.WriteLn("<f " + 1 + " " + force + "\r");
                }
                else
                {
                    EagleAPI.actuators[1].Force(0);                 //Serial.WriteLn("<f " + 1 + " " + 0 + "\r");
                }
                //if the actuators are within a certain distance of their midpoint or the maximum time has elapsed complete homing
                if ((Mathf.Abs(position1 - midPosition1) <= positionFactor && Mathf.Abs(position2 - midPosition2) <= positionFactor) || ((Time.time - starttime) > 6))
                {
                    midPosition1 = position1;
                    midPosition2 = position2;
                    homed = true;
                    return true;
                }
            }
            return false;
        }
        else
        {
            return true;
        }
    }
    // This function is called when the game is closed
    void OnApplicationQuit()
    {
        //Disable the actuators
        EagleAPI.actuators[0].Sleep();                //Serial.WriteLn("<sleep " + 0 + "\r");
        EagleAPI.actuators[1].Sleep();                //Serial.WriteLn("<sleep " + 1 + "\r");
    }

    // This function is called multiple times per frame
    void OnGUI()
    {
        autoForces = GUILayout.Toggle(autoForces, "Automatic Actuator Forces", "Button");   //toggle button to toggle automatic forces

        if (autoForces && !homed)
        {
            GUI.Label(new Rect(Screen.width / 2 - 125, 200, 250, 50), "HOMING ACTUATORS: PLEASE WAIT"); //while the actuators are homing display this message
        }
        else if (GameObject.Find("Ball").GetComponent<Rigidbody>().velocity == Vector3.zero)
        {
            GUI.Label(new Rect(Screen.width / 2 - 125, 200, 250, 50), "MOVE BALL WITH ARROW KEYS"); //if the ball is not moving display this message
        }
    }
}
