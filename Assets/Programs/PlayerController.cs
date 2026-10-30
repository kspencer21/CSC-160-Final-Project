using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private class Vector2
    {
        readonly double x;
        readonly double y;
        
        private Vector2(double x, double y)
        {
            this.x = x;
            this.y = y;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        const double jumpForce = 1.0d;
        const double walkSpeed = 1.0d;
        const double sprintSpeed = 2.0d;

        Vector2 pos = Vector2(0, 0);
        Vector2 speed = Vector2(0, 0);
    }

    // Update is called once per frame
    static void Update()
    {
        
    }
}
