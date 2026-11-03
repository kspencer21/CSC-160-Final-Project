using UnityEngine;
using UnityEngine.UIElements;

public class PlayerController : MonoBehaviour
{
    // constants
    private const float maxHold = 1f; // number of seconds after holding jump that currentJumpForce = pounceForce
    private const float minHold = 0.2f; // number of seconds the player must hold a jump key for currentJumpForce to begin changing to pounceForce
    private const float climbTime = 3.0f; // amount of time in seconds that a player can climb

    // serialized variables (can be modified in the editor)
    [SerializeField] public Vector2 jumpForce = new Vector2(0f, 15f);
    [SerializeField] public Vector2 pounceForce = new Vector2(40f, 5f);
    [SerializeField] public Vector2 speedForce = new Vector2(2f, 0f); // speedForce determines how much the translational movement is affected by acceleration (via forces)
    [SerializeField] public Vector2 walkSpeed = new Vector2(5f, 0f); // walkSpeed determines how much the translational movement is immediate (direct translation)
    [SerializeField] public Vector2 crawlSpeed = new Vector2(2f, 0f);
    [SerializeField] public Vector2 sprintSpeed = new Vector2(8f, 0f);
    
    // object/components
    private static Rigidbody2D rb;
    private static Collider2D collider;
    private static Renderer rend;

    // player states
    private static Direction1D facing = Direction1D.RIGHT;
    private static bool isOnGround = false;

    // Update() and FixedUpdate() interactions
    private static bool jumpRequest = false;
    private static bool moveRequest = false;

    // miscellaneous
    private float pounceTicker = 0.0f; // amount of time in seconds that the space bar (jump key) has been held
    private static Vector3 lastPos;
    private static Vector3 currentPos;

    // "current" values
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
        if (Input.GetKey(KeyCode.S)) {
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D))
            {
                pounceTicker = 0;
            } else // only run when the player is crouching but not crawling
            {
                // update pounceTicker based on how much time has passed
                pounceTicker += Time.deltaTime;
            }
        } else
        {
            pounceTicker = 0;
        }
    }
    
    /// <summary>
    /// Make the player jump
    /// </summary>
    void jump()
    {
        // update currentJumpForce accordingly
        updateJumpForce(pounceTicker);

        // jump
        rb.AddForce(new Vector3(currentJumpForce.x, currentJumpForce.y, 0), ForceMode2D.Impulse);

        // reset the pounceTicker
        pounceTicker = 0;
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
    /// When either A or D is pressed and S is pressed, the player moves at a crawling speed instead
    /// </remarks>
    void setVelocity()
    {
        if (Input.GetKey(KeyCode.A) ^ Input.GetKey(KeyCode.D))
        {
            if (Input.GetKey(KeyCode.S))
            {
                currentVelocity.x = crawlSpeed.x;
            } else
            {
                currentVelocity.x = walkSpeed.x;
            }
        } else
        {
            currentVelocity.x = 0;
        }
    }

    /// <summary>
    /// Move the player based on their current speed force
    /// </summary>
    /// <param name="force">The "force" speed to be applied</param>
    void moveForce(Vector2 force)
    {
        // apply force
        rb.AddForce(new Vector3((int)facing * force.x, force.y, 0), ForceMode2D.Force);
    }

    /// <summary>
    /// Move the player based on their current speed
    /// </summary>
    /// <param name="speed"></param>
    void moveTranslate(Vector2 speed)
    {
        // apply immediate translation
        transform.Translate(new Vector3((int)facing * speed.x * Time.deltaTime, speed.y * Time.deltaTime, 0));
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
        // isOnGround = collider.IsTouchingLayers(3);


        // facing variable setter
        updateFacing();

        // jump and pounce mechanics
        chargePounce(); 
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W)) 
        {
            jumpRequest = true;
        }

        // left and right movement mechanics
        setVelocity();
        if (Input.GetKey(KeyCode.A) ^ Input.GetKey(KeyCode.D))
        {
            moveRequest = true;
            moveTranslate(currentVelocity);
        }


        // debug
        print("Velocity: " + currentVelocity.x);
        print("Force: " + speedForce.x);
        print("Facing: " + facing);
        print("Facing (int): " + (int)facing);

        // prepare for next tick
        currentVelocity.x = 0;
        currentVelocity.y = 0;
        lastPos = transform.position;
    }

    // FixedUpdate is called at a fixed rate of 50 hertz
    void FixedUpdate()
    {
        // ensure that the player is not rotated
        transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        
        // jump if a jump has been requested
        if (jumpRequest)
        {
            jump();
            jumpRequest = false;
        }
        
        // move via forces if movement has been requested
        if (moveRequest) {
            moveForce(speedForce);
            moveRequest = false;
        }
    }



    // ----- ##### ----- ##### ----- ##### -----

    public enum Direction1D
    {
        LEFT = -1,
        RIGHT = 1
    }
    
}
