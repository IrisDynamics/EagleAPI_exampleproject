using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EagleAPI
{
    public static string ActID, force, position, temperature, state, polarity, actuatorInfo, invalidAct, invalidArg;

    public class Force : EagleAPI
    {
        public static void send(int actuatorID, int forceArg)
        {
            Serial.WriteLn("<f " + actuatorID + " " + forceArg + "\r");
        }
        public static bool receive(string[] parsedline)
        {
            if (parsedline[0] == ">f")
            {
                ActID = parsedline[1];
                force = parsedline[2];
                position = parsedline[3];
                return true;
            }
            else return false;
        }
    }

    public class ForceExtended : EagleAPI
    {
        public static void send(int actuatorID, string forceArg)
        {
            Serial.WriteLn("<exf " + actuatorID + " " + forceArg + "\r");
        }
        public static bool receive(string[] parsedline)
        {
            if (parsedline[0] == ">exf")
            {
                ActID = parsedline[1];
                force = parsedline[2];
                position = parsedline[3];
                temperature = parsedline[4];
                return true;
            }
            else return false;
        }
    }

    public class Sleep : EagleAPI
    {
        public static void send(int actuatorID)
        {
            Serial.WriteLn("<sleep " + actuatorID + "\r");
        }
        public static bool receive(string[] parsedline)
        {
            if (parsedline[0] == ">sleep")
            {
                ActID = parsedline[1];
                return true;
            }
            else return false;
        }
    }
    public class Wake : EagleAPI
    {
        public static void send(int actuatorID)
        {
            Serial.WriteLn("<wake " + actuatorID + "\r");
        }
        public static bool receive(string[] parsedline)
        {
            if (parsedline[0] == ">wake")
            {
                ActID = parsedline[1];
                return true;
            }
            else return false;
        }
    }

    public class Polarity : EagleAPI
    {
        public static void send(int actuatorID)
        {
            Serial.WriteLn("<pol " + actuatorID + "\r");
        }
        public static bool receive(string[] parsedline)
        {
            if (parsedline[0] == ">pol")
            {
                ActID = parsedline[1];
                polarity = parsedline[2];
                return true;
            }
            else return false;
        }
    }

    public class ResetPosition : EagleAPI
    {
        public static void send(int actuatorID)
        {
            Serial.WriteLn("<rp " + actuatorID + "\r");
        }
        public static bool receive(string[] parsedline)
        {
            if (parsedline[0] == ">rp")
            {
                ActID = parsedline[1];
                return true;
            }
            else return false;
        }
    }

    public class Temperature : EagleAPI
    {
        public static void send(int actuatorID)
        {
            Serial.WriteLn("<temp " + actuatorID + "\r");
        }
        public static bool receive(string[] parsedline)
        {
            if (parsedline[0] == ">temp")
            {
                ActID = parsedline[1];
                temperature = parsedline[2];
                return true;
            }
            else return false;
        }
    }

    public class State : EagleAPI
    {
        public static void send(int actuatorID)
        {
            Serial.WriteLn("<state " + actuatorID + "\r");
        }
        public static bool receive(string[] parsedline)
        {
            if (parsedline[0] == ">state")
            {
                ActID = parsedline[1];
                state = parsedline[2];
                return true;
            }
            else return false;
        }
    }

    public class Info : EagleAPI
    {
        public static void send(int actuatorID)
        {
            Serial.WriteLn("<info " + actuatorID + "\r");
        }
        public static bool receive(string[] parsedline)
        {
            if (parsedline[0] == ">info")
            {
                ActID = parsedline[1];
                actuatorInfo = "";
                for (int i = 2; i < parsedline.Length; i++)
                {
                    actuatorInfo += parsedline[i] + "\n";
                }
                return true;
            }
            else return false;
        }
    }
}