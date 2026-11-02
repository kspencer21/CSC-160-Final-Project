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

    private class Keys
    {
        readonly boolean space;
        readonly boolean w;
        readonly boolean a;
        readonly boolean s;
        readonly boolean d;
        readonly boolean leftShift;
        readonly boolean leftCrtl;
        readonly boolean tab;

        private Keys()
        {
            this.space = false;
            this.w = false;
            this.a = false;
            this.s = false;
            this.d = false;
            this.leftShift = false;
            this.leftCrtl = false;
            this.tab = false;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    static void Start()
    {
        Keys keys = new Keys();
        
        Vector2 maxJumpForce = Vector2(1.0, 1.0);
        Vector2 minJumpForce = Vector2(3.0, 0.33);
        double walkSpeed = 1.0d;
        double sprintSpeed = 2.0d;
        double climbTime = 3.0d; // amount of time in seconds that a player can climb

        Vector2 velocity = Vector2(0, 0);

        int pounceTicker = 0;
    }

    /// <summary>
    /// Updates keys object when a key is first pressed
    /// </summary>
    /// <param name="ev">The event of the keypress</param>
    static void OnKeyDown(KeyDownEvent ev)
    {
        switch(ev.keyCode)
        {
            case Space:
                keys.space = true;
                break;
            case W:
                keys.w = true;
                break;
            case A:
                keys.a = true;
                break;
            case S:
                keys.s = true;
                break;
            case D:
                keys.d = true;
                break;
            case LeftShift:
                keys.leftShift = true;
                break;
            case LeftControl:
                keys.leftCtrl = true;
                break;
            case Tab:
                keys.tab = true;
                break;
        }
    }

    /// <summary>
    /// Updates keys object when a key is released
    /// </summary>
    /// <param name="ev">The event of the key release</param>
    static void OnKeyUp(KeyUpEven ev)
    {
        switch(ev.keyCode)
        {
            case Space:
                keys.space = false;
                break;
            case W:
                keys.w = false;
                break;
            case A:
                keys.a = false;
                break;
            case S:
                keys.s = false;
                break;
            case D:
                keys.d = false;
                break;
            case LeftShift:
                keys.leftShift = false;
                break;
            case LeftControl:
                keys.leftCtrl = false;
                break;
            case Tab:
                keys.tab = false;
                break;
        }
    }

    /// <summary>
    /// Make the player jump
    /// </summary>
    static void Jump()
    {
        
    }

    // Update is called once per frame
    static void Update()
    {
        switch(null)
        {
            case keys.space:
                pounceTicker++;
                break;
        }
    }
}
