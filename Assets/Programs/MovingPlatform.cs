using UnityEngine;
using UnityEngine.UIElements;
using System;

public class MovingPlatform : MonoBehaviour
{    
    [SerializeField] public Vector2 radius = new Vector2(4f, 0f); // The x and y radii of the platform's motion
    [SerializeField] public float period = 4f; // How long the platform takes to get back to its original position (moving in the same direction) in seconds
    [SerializeField] public MovementType type = MovementType.LINEAR;

    private static float ticker = 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        ticker += Time.deltaTime;

        if (type == MovementType.LINEAR)
        {
            transform.Translate(new Vector3((float)(2 * Math.PI * radius.x * Time.deltaTime / period * Math.Cos(2 * Math.PI * ticker / period)), (float)(2 * Math.PI * radius.y * Time.deltaTime / period * Math.Cos(2 * Math.PI * ticker / period)), 0));
            // need to change from translation to physics velocity so that friction is applied to an entity standing on the platform
        } else if (type == MovementType.CIRCULAR)
        {
            
        }

    }


    public enum MovementType
    {
        LINEAR,
        CIRCULAR
    }
}
