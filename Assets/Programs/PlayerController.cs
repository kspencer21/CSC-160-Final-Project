using UnityEngine;
using UnityEngine.UIElements;

public class PlayerController : MonoBehaviour
{
    public const double maxHold = 3.0d; // number of seconds after holding space that currentJumpForce = pounceForce
    
    public static Keys keys = new Keys();
    public static Rigidbody2D rb;

    public static Vector2 jumpForce = new Vector2(0.0d, 500.0d);
    public static Vector2 pounceForce = new Vector2(300.0d, 200.0d);
    public double walkSpeed = 1.0d;
    public double sprintSpeed = 2.0d;
    public double climbTime = 3.0d; // amount of time in seconds that a player can climb

    public double pounceTicker = 0; // amount of time in seconds that the space bar (jump key) has been held

    public static Vector2 currentJumpForce = new Vector2(jumpForce.x, jumpForce.y);

    
    // ----- ##### ----- ##### ----- ##### -----

    /// <summary>
    /// Updates keys object when a key is first pressed
    /// </summary>
    /// <param name="ev">The event of the keypress</param>
    void OnKeyDown(KeyDownEvent ev)
    {
        switch(ev.keyCode)
        {
            case KeyCode.Space:
                keys.space = true;
                break;
            case KeyCode.W:
                keys.w = true;
                break;
            case KeyCode.A:
                keys.a = true;
                break;
            case KeyCode.S:
                keys.s = true;
                break;
            case KeyCode.D:
                keys.d = true;
                break;
            case KeyCode.LeftShift:
                keys.leftShift = true;
                break;
            case KeyCode.LeftControl:
                keys.leftCtrl = true;
                break;
            case KeyCode.Tab:
                keys.tab = true;
                break;
        }
    }

    /// <summary>
    /// Updates keys object when a key is released
    /// </summary>
    /// <param name="ev">The event of the key release</param>
    void OnKeyUp(KeyUpEvent ev)
    {
        switch(ev.keyCode)
        {
            case KeyCode.Space:
                keys.space = false;
                // updateJumpPower(pounceTicker);
                // jump(currentJumpForce);
                break;
            case KeyCode.W:
                keys.w = false;
                break;
            case KeyCode.A:
                keys.a = false;
                break;
            case KeyCode.S:
                keys.s = false;
                break;
            case KeyCode.D:
                keys.d = false;
                break;
            case KeyCode.LeftShift:
                keys.leftShift = false;
                break;
            case KeyCode.LeftControl:
                keys.leftCtrl = false;
                break;
            case KeyCode.Tab:
                keys.tab = false;
                break;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="t">The amount of time that the space key was held</param>
    void updateJumpPower(double t)
    {
        this.pounceTicker = 0;

        if (t >= maxHold)
        {
            currentJumpForce.x = pounceForce.x;
            currentJumpForce.y = pounceForce.y;
        } else
        {
            currentJumpForce.x = ((pounceForce.x - jumpForce.x) / maxHold * t) + jumpForce.x;
            currentJumpForce.y = ((pounceForce.y - jumpForce.y) / maxHold * t) + jumpForce.y;
        }
    }

    /// <summary>
    /// Make the player jump
    /// </summary>
    /// <param name="power">A Vector2 representative of the jump power</param>
    void jump(Vector2 power)
    {
        // rb.AddForce(new Vector3((float)power.x, (float)power.y, 0), ForceMode2D.Force);
        rb.AddForce(new Vector3(1000, 100, 0), ForceMode2D.Force);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        if (keys.space)
        {
            pounceTicker += Time.deltaTime;
        }
    }

    void FixedUpdate()
    {
        if (Input.GetKey(KeyCode.Space))
        {
            pounceTicker += Time.deltaTime;
        }
        if (Input.GetKeyUp(KeyCode.Space))
        {
            if (pounceTicker >= maxHold)
            {
                currentJumpForce.x = pounceForce.x;
                currentJumpForce.y = pounceForce.y;
            } else
            {
                currentJumpForce.x = ((pounceForce.x - jumpForce.x) / maxHold * pounceTicker) + jumpForce.x;
                currentJumpForce.y = ((pounceForce.y - jumpForce.y) / maxHold * pounceTicker) + jumpForce.y;
            }

            rb.AddForce(new Vector3((float)currentJumpForce.x, (float)currentJumpForce.y, 0), ForceMode2D.Force);

            pounceTicker = 0;
        }
    }


    // ----- ##### ----- ##### ----- ##### -----

    public class Vector2
    {
        public double x;
        public double y;
        
        public Vector2(double x, double y)
        {
            this.x = x;
            this.y = y;
        }
    }

    public class Keys
    {
        public bool space;
        public bool w;
        public bool a;
        public bool s;
        public bool d;
        public bool leftShift;
        public bool leftCtrl;
        public bool tab;

        public Keys()
        {
            this.space = false;
            this.w = false;
            this.a = false;
            this.s = false;
            this.d = false;
            this.leftShift = false;
            this.leftCtrl = false;
            this.tab = false;
        }
    }


}
