using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spring : MonoBehaviour
{
    int actID = 0;   //
    int springCenter = 75*1000;    //location of the spring center in micrometers
    float springK = 0.005f;        //spring constant will control the strength of the spring
    float maxForce = 50;

    void Start() //this function is called once when the Unity game is started
    {
        EagleAPI.SystemeReady();
    }

    void FixedUpdate() //this function at a fixed time interval, can be changed in project settings
    {
        float force = -(EagleAPI.actuators[actID].position - springCenter) * springK;
        if (Mathf.Abs(force) > maxForce) force = Mathf.Sign(force) * maxForce;
        EagleAPI.actuators[actID].Force((int)force); // sending 0 force to the first actuator in order to receive a position response         
    }
}
