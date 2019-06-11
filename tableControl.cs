using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class tableControl : MonoBehaviour
{
    int position1, position2, force1, force2, forceScale = 10;
    public int midPosition1 = 10000, midPosition2 = 10000, maxPosition1, maxPosition2;
    float positionToAngle = 0.005f, springK = 0.003f, starttime;
    Vector3 ballposition;
    bool autoForces=true, homed = false;

    // Start is called before the first frame update
    void Start()
    {
        Serial.WriteLn("<wake " + 1 + "\r");
        Serial.WriteLn("<wake " + 2 + "\r");
        starttime = Time.time;
    }
    // Update is called once per frame
    void FixedUpdate()
    {
        if (Home())
        {
            ballposition = GameObject.Find("Ball").transform.position;
            force1 = (int)(ballposition.x * forceScale - (position1 - midPosition1) * springK);
            force2 = (int)(-ballposition.z * forceScale - (position2 - midPosition2) * springK); //make negative due to coordinate system orientation
            if (!autoForces)
            {
                force1 = 0;
                force2 = 0;
            }
            Serial.WriteLn("<f " + 1 + " " + force1 + "\r");
            Serial.WriteLn("<f " + 2 + " " + force2 + "\r");
            transform.eulerAngles = new Vector3((position2 - midPosition2) * positionToAngle, 0, (position1 - midPosition1) * positionToAngle);
        }
        
    }
    void OnSerialLine(string line)
    {
        string[] parsed = line.Split(null);
        if (parsed[0] == ">f")
        {
            switch (int.Parse(parsed[1]))
            {
                case 1: position1 = int.Parse(parsed[3]); break;
                case 2: position2 = int.Parse(parsed[3]); break;
            }
        }
    }
    bool Home()
    {
        int homeingForce = 20;
        int positionFactor = 200;
        if (!homed && autoForces)
        {
            if ((Time.time - starttime) < 2)
            {
                Serial.WriteLn("<f " + 1 + " " + -homeingForce + "\r");
                Serial.WriteLn("<f " + 2 + " " + -homeingForce + "\r");
            }
            else if ((Time.time - starttime) < 4)
            {
                Serial.WriteLn("<f " + 1 + " " + homeingForce + "\r");
                Serial.WriteLn("<f " + 2 + " " + homeingForce + "\r");
                maxPosition1 = position1;
                maxPosition2 = position2;
            }
            else
            {
                midPosition1 = maxPosition1 / 2;
                midPosition2 = maxPosition2 / 2;
                if (Mathf.Abs(position1 - midPosition1) > positionFactor)
                {
                    int force = (midPosition1 - position1) / positionFactor;
                    Serial.WriteLn("<f " + 1 + " " + force + "\r");
                }
                else
                {
                    Serial.WriteLn("<f " + 1 + " " + 0 + "\r");
                }

                if (Mathf.Abs(position2 - midPosition2) > positionFactor)
                {
                    int force = (midPosition2 - position2) / positionFactor;
                    Serial.WriteLn("<f " + 2 + " " + force + "\r");
                }
                else
                {
                    Serial.WriteLn("<f " + 2 + " " + 0 + "\r");
                }

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

    void OnApplicationQuit()
    {
        Serial.WriteLn("<sleep " + 1 + "\r");
        Serial.WriteLn("<sleep " + 2 + "\r");
    }

    void OnGUI()
    {
        autoForces = GUILayout.Toggle(autoForces, "Automatic Actuator Forces");
    }
}
