using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spring : MonoBehaviour
{
    int actID = 0;   //
    int springCenter = 10000;    //location of the spring center 
    float springK = 0.005f;        //spring constant will control the strength of the spring
    int actuator_position;
    float maxForce = 50;

    void Start() //this function is called once when the Unity game is started
    {
        EagleAPI.actuators[actID].Wake();
    }

    void OnSerialLine(string line) //this function is called from Serial.cs when a new serial line is received
    {
        Debug.Log(line);
        EagleAPI.Receive(line);
    }

    void FixedUpdate() //this function at a fixed time interval, can be changed in project settings
    {
        actuator_position = EagleAPI.actuators[actID].position;
        float force = -(actuator_position - springCenter) * springK;
        if (Mathf.Abs(force) > maxForce) force = Mathf.Sign(force) * maxForce;
        EagleAPI.actuators[actID].Force((int)force); // sending 0 force to the first actuator in order to receive a position response         
    }

    void OnApplicationQuit()
    {
        EagleAPI.actuators[actID].Sleep();
    }
}
