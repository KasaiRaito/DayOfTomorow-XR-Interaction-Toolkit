using UnityEngine;
using UnityEngine.XR.Content.Interaction;

public class Tank : MonoBehaviour
{
    public XRLever L_Lever;
    public XRLever R_Lever;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Debug.Log("Left: " + L_Lever.value);
        Debug.Log("Right: " + R_Lever.value);
    }
}