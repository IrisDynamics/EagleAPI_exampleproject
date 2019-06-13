//This script is used to control a ball movement with inputs from the keyboard arrowkeys
//Add this script to a sphere game object name "Ball" 
//Ensure the gameobject has a rigidbody component

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ballMovement : MonoBehaviour
{
    Rigidbody rb;
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();    
    }
    // FixedUpdate is called at a fixed time interval
    void FixedUpdate()
    {
        if (rb.position.magnitude > 20)
        {
            rb.position = Vector3.zero;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        else
        {
            rb.AddForce(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        }
    }
}
