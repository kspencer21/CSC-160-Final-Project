using UnityEngine;
using UnityEngine.UIElements;
using System;

public class MovingPlatform : MonoBehaviour
{   
    private static Rigidbody2D rb;

    [SerializeField] public Vector2 radius = new Vector2(4f, 0f); // The x and y radii of the platform's motion
    [SerializeField] public float period = 4f; // How long the platform takes to get back to its original position (moving in the same direction) in seconds
    [SerializeField] public MovementType type = MovementType.LINEAR;

    private static float ticker = 0f;

    public Vector2 acceleration;
    private Vector2 lastVelocity;
    private Vector2 currentVelocity;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // reset ticker
        ticker = 0f;

        lastVelocity = rb.linearVelocity;
    }

    // Update is called once per frame
    void Update()
    {

    }

    void FixedUpdate()
    {
        ticker += Time.fixedDeltaTime;

        acceleration = lastVelocity - currentVelocity;
        currentVelocity = rb.linearVelocity;

        if (type == MovementType.LINEAR)
        {
            rb.linearVelocity = new Vector3((float)(2 * Math.PI * radius.x / period * Math.Cos(2 * Math.PI * ticker / period)), (float)(2 * Math.PI * radius.y / period * Math.Cos(2 * Math.PI * ticker / period)), 0);
        } else if (type == MovementType.CIRCULAR)
        {
            
        } else if (type == MovementType.CUSTOM) // the platform is using some phase offset to create an ellipse instead of a line or circle
        {
            
        }

        lastVelocity = rb.linearVelocity;

        
        // print("MovingPlatform.cs: " + acceleration);
    }


    public enum MovementType
    {
        LINEAR,
        CIRCULAR,
        CUSTOM
    }
}
