using UnityEngine;
using UnityEngine.UIElements;

public class PlayerController : MonoBehaviour
{
    public const float maxHold = 1.5f; // number of seconds after holding space that currentJumpForce = pounceForce
    public const float minHold = 0.2f;
    
    // public static Keys keys = new Keys();
    private static Rigidbody2D rb;

    private static Vector2 jumpForce = new Vector2(0.0f, 15.0f);
    private static Vector2 pounceForce = new Vector2(40.0f, 5.0f);
    private float walkSpeed = 1.0f;
    private float sprintSpeed = 2.0f;
    private float climbTime = 3.0f; // amount of time in seconds that a player can climb

    private float pounceTicker = 0.0f; // amount of time in seconds that the space bar (jump key) has been held

    private static Vector2 currentJumpForce = new Vector2(0.0f, 0.0f);
    private static Vector2 velocityForce = new Vector2(0.0f, 0.0f);

    
    // ----- ##### ----- ##### ----- ##### -----

    /// <summary>
    /// 
    /// </summary>
    /// <param name="t">The amount of time that the space key was held</param>
    void updateJumpPower(float t)
    {
        this.pounceTicker = 0;

        if (t >= maxHold)
        {
            currentJumpForce.x = pounceForce.x;
            currentJumpForce.y = pounceForce.y;
        } else
        {
            currentJumpForce.x = (float)(((pounceForce.x - jumpForce.x) / maxHold * t) + jumpForce.x);
            currentJumpForce.y = (float)(((pounceForce.y - jumpForce.y) / maxHold * t) + jumpForce.y);
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.W))
        {
            pounceTicker += Time.deltaTime;
            
            // make it so that the player cannot move while charging a pounce
            velocityForce.x = 0;
            velocityForce.y = 0;
        }
        if (Input.GetKeyUp(KeyCode.Space) || Input.GetKeyUp(KeyCode.W))
        {
            // default to the pounceForce values if the jump button has been held longer than the charge time (maxHold)
            if (pounceTicker >= maxHold)
            {
                currentJumpForce.x = pounceForce.x;
                currentJumpForce.y = pounceForce.y;
            } else if (pounceTicker <= minHold)
            {
                currentJumpForce.x = jumpForce.x;
                currentJumpForce.y = jumpForce.y;
            } else 
            {
                currentJumpForce.x = (float)(((pounceForce.x - jumpForce.x) / (maxHold - minHold) * (pounceTicker - minHold)) + jumpForce.x);
                currentJumpForce.y = (float)(((pounceForce.y - jumpForce.y) / (maxHold - minHold) * (pounceTicker - minHold)) + jumpForce.y);
            }

            rb.AddForce(new Vector3(currentJumpForce.x, currentJumpForce.y, 0), ForceMode2D.Impulse);

            pounceTicker = 0;

            currentJumpForce.x = 0;
            currentJumpForce.y = 0;
        }
    }

    void FixedUpdate()
    {
        
    }


    // ----- ##### ----- ##### ----- ##### -----

    /// <summary>
    /// 2 dimensional vector class
    /// Contains x and y components
    /// </summary>
    // public class Vector2
    // {
    //     public double x;
    //     public double y;
        
    //     /// <summary>
    //     /// Vector2 constructor
    //     /// </summary>
    //     /// <param name="x"></param>
    //     /// <param name="y"></param>
    //     public Vector2(double x, double y)
    //     {
    //         this.x = x;
    //         this.y = y;
    //     }
    // }

    // public class Keys
    // {
    //     public bool space;
    //     public bool w;
    //     public bool a;
    //     public bool s;
    //     public bool d;
    //     public bool leftShift;
    //     public bool leftCtrl;
    //     public bool tab;

    //     public Keys()
    //     {
    //         this.space = false;
    //         this.w = false;
    //         this.a = false;
    //         this.s = false;
    //         this.d = false;
    //         this.leftShift = false;
    //         this.leftCtrl = false;
    //         this.tab = false;
    //     }
    // }


}
