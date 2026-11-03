using UnityEngine;
using UnityEngine.UIElements;

public class PlayerController : MonoBehaviour
{
    [SerializeField] public const float maxHold = 1.5f; // number of seconds after holding jump that currentJumpForce = pounceForce
    [SerializeField] public const float minHold = 0.2f; // number of seconds the player must hold a jump key for currentJumpForce to begin changing to pounceForce
    [SerializeField] public const float climbTime = 3.0f; // amount of time in seconds that a player can climb
    [SerializeField] public Vector2 jumpForce = new Vector2(0f, 15f);
    [SerializeField] public Vector2 pounceForce = new Vector2(40f, 5f);
    [SerializeField] public Vector2 speedForce = new Vector2(5f, 0f); // speedForce determines how much the translational movement is affected by acceleration (via forces)
    [SerializeField] public Vector2 walkSpeed = new Vector2(3f, 0f); // walkSpeed determines how much the translational movement is immediate (direct translation)
    [SerializeField] public Vector2 sprintSpeed = new Vector2(5f, 0f);
    
    private static Rigidbody2D rb;
    private static Collider2D collider;
    private static Renderer rend;

    private static Direction1D facing = Direction1D.RIGHT;
    private static bool isOnGround = false;

    private float pounceTicker = 0.0f; // amount of time in seconds that the space bar (jump key) has been held
    private static Vector3 lastPos;
    private static Vector3 currentPos;

    private static Vector2 currentJumpForce = new Vector2(0f, 0f);
    private static Vector2 currentVelocity = new Vector2(0f, 0f);
    

    
    // ----- ##### ----- ##### ----- ##### -----

    /// <summary>
    /// Updates the currentJumpForce
    /// </summary>
    /// <param name="t">The amount of time that the space key was held</param>
    void updateJumpForce(float t)
    {
        this.pounceTicker = 0;

        // default to the pounceForce values if the jump button has been held longer than maxHold (charge time)
        if (t >= maxHold)
        {
            currentJumpForce.x = pounceForce.x;
            currentJumpForce.y = pounceForce.y;
        } else if (t <= minHold) // default to the jumpForce values if the jump button is not held longer than minHold (normal jump window)
        {
            currentJumpForce.x = jumpForce.x;
            currentJumpForce.y = jumpForce.y;
        } else // adjust jump force based on the amount of time the jump button is held
        {
            currentJumpForce.x = (int)facing * (float)(((pounceForce.x - jumpForce.x) / (maxHold - minHold) * (t - minHold)) + jumpForce.x);
            currentJumpForce.y = (float)(((pounceForce.y - jumpForce.y) / (maxHold - minHold) * (t - minHold)) + jumpForce.y);
        }
    }

    /// <summary>
    /// Update the pounceTicker variable
    /// Ensure that the player cannot move while charging a pounce
    /// </summary>
    void chargePounce()
    {
        if (Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.W)) {
            // update pounceTicker based on how much time has passed
            pounceTicker += Time.deltaTime;
            
            // make it so that the player cannot move while charging a pounce
            currentVelocity.x = 0;
            currentVelocity.y = 0;
        }
    }
    
    /// <summary>
    /// Make the player jump
    /// </summary>
    void jump()
    {
        if (Input.GetKeyUp(KeyCode.Space) || Input.GetKeyUp(KeyCode.W)) {
            // update currentJumpForce accordingly
            updateJumpForce(pounceTicker);

            // jump
            rb.AddForce(new Vector3(currentJumpForce.x, currentJumpForce.y, 0), ForceMode2D.Impulse);

            // reset the pounceTicker
            pounceTicker = 0;
        }
    }

    /// <summary>
    /// Update the facing variable based on the left/right movement keys that are pressed
    /// </summary>
    void updateFacing()
    {
        if (Input.GetKey(KeyCode.A)) { facing = Direction1D.LEFT; }
        if (Input.GetKey(KeyCode.D)) { facing = Direction1D.RIGHT; }
    }

    /// <summary>
    /// Update player velocity based on keypresses
    /// </summary>
    /// <remarks>
    /// XOR is used so that velocity is nullified if both left (A) and right (D) keys are pressed in addition to if neither are pressed
    /// Directionality is handled by the facing variable. currentVelocity determines the speed at which the player is moving
    /// </remarks>
    void setVelocity()
    {
        if (Input.GetKey(KeyCode.A) ^ Input.GetKey(KeyCode.D))
        {
            currentVelocity.x = walkSpeed.x;
        } else
        {
            currentVelocity.x = 0;
        }
    }

    /// <summary>
    /// Move the player based on their current speed
    /// </summary>
    /// <param name="force">The "force" speed to be applied</param>
    /// <param name="speed">The immediately translational speed to be applied</param>
    void move(Vector2 force, Vector2 speed)
    {
        // only execute if ONE left/right directional key is pressed and neither jump key is pressed
        if ((Input.GetKey(KeyCode.A) ^ Input.GetKey(KeyCode.D)) && !(Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.W)))
        {
            // apply force
            rb.AddForce(new Vector3((int)facing * force.x, force.y, 0), ForceMode2D.Force);

            // apply immediate translation
            transform.Translate(new Vector3((int)facing * speed.x * Time.deltaTime, speed.y * Time.deltaTime, 0));
        }
    }



    // ----- ##### ----- ##### ------ ##### -----

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        collider = GetComponent<Collider2D>();
        rend = GetComponent<Renderer>();

        lastPos = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        // prepare relative variables (e.g. currentPos (corresponding to lastPos))
        currentPos = transform.position;

        // update isOnGround
        isOnGround = collider.IsTouchingLayers(3);


        // facing variable setter
        updateFacing();

        // jump and pounce mechanics
        chargePounce(); 
        jump();

        // left and right movement mechanics
        setVelocity();
        move(speedForce, currentVelocity);


        // debug
        print(isOnGround);

        // prepare for next tick
        currentVelocity.x = 0;
        currentVelocity.y = 0;
        lastPos = transform.position;
    }

    void FixedUpdate()
    {
        
    }


    // ----- ##### ----- ##### ----- ##### -----

    public enum Direction1D
    {
        LEFT = -1,
        RIGHT = 1
    }
    
}
