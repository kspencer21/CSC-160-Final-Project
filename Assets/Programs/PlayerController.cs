using UnityEngine;
using UnityEngine.UIElements;
using System;

public class PlayerController : MonoBehaviour
{
    // object components
    private Rigidbody2D rb;
    private Collider2D collider;
    private Renderer rend;

    // layer masks
    private int groundLayer;

    // tags
    private int groundTag;
    private int climbableTag;

    // normal vector of a collision
    // private Vector2 contactNormal = new Vector2(0, 0);
    
    // constants
    private const float maxHold = 1f; // number of seconds after holding jump that currentJumpForce = pounceForce
    private const float minHold = 0.2f; // number of seconds the player must hold a jump key for currentJumpForce to begin changing to pounceForce
    private const float movingJumpBoost = 2.5f; // additional force to be applied only when the player is moving when they jump. Modified by the player's velocityVector
    private const float landBoost = 1.5f; // additional force to be applied when the player lands to account for the sudden friction
    private const float climbTime = 3f; // amount of time in seconds that a player can climb
    private const float jumpCooldown = 0.1f; // amount of time in seconds after jumping that a player can jump again
    private const float airForceFactor = 0.2f; // how much to multiply the speedForce by when in air
    private const float airTranslationFactor = 0.8f; // how much to multiply the crawlSpeed by when in air
    // private const float speedLimitWalk = 10f; // how far the player can move in a second when walking
    // private const float speedLimitCrawl = 1f;
    // private const float speedLimitSprint = 1f;

    // serialized variables (can be modified in the editor)
    [SerializeField] public Vector2 jumpForce = new Vector2(0f, 18f); // the amount of force to be applied to the player when they jump
    [SerializeField] public Vector2 pounceForce = new Vector2(20f, 10f); // the amount of force to be applied to the player after a fully charged pounce
    [SerializeField] public Vector2 speedForce = new Vector2(20f, 0f); // speedForce determines how much the translational movement is affected by acceleration (via forces)
    [SerializeField] public Vector2 walkSpeed = new Vector2(2f, 0f); // walkSpeed determines how much the translational movement is immediate (direct translation)
    [SerializeField] public Vector2 crawlSpeed = new Vector2(1f, 0f); // crawlSpeed is an alternative to walkSpeed used when the player is crouched
    // [SerializeField] public Vector2 sprintSpeed = new Vector2(3f, 0f); // sprintSpeed is an alternative to walkSpeed used when the player is sprinting
    
    // player states
    private Direction1D facing = Direction1D.RIGHT; // determines which way the player is facing
    private Direction1D facingStrict = Direction1D.RIGHT; // a parallel to the facing variable which is only altered when the player is on the ground
    private bool isOnGround = false;
    private Vector2 velocityVector; // the physics-based (i.e. from applied forces) velocity of the player

    // Update() and FixedUpdate() interactions
    private bool jumpRequest = false;
    private bool moveRequest = false;

    // miscellaneous
    private Movement mov = new Movement();
    private float pounceTicker = 0f; // amount of time in seconds that the space bar (jump key) has been held
    private float jumpTicker = 0f;
    private Vector3 lastPos;
    private Vector3 currentPos;

    // current values
    private Vector2 currentJumpForce = new Vector2(0f, 0f);
    private Vector2 currentVelocity = new Vector2(0f, 0f);
    



    
    // ----- ##### ----- ##### ----- ##### -----

    /// <summary>
    /// Listen for particular keys pressed.
    /// </summary>
    /// <remarks>
    /// Converts multiple keypresses with the same functionality to a single attribute, i.e. A and left arrow both affect mov.left.
    /// </remarks>
    void eventListen()
    {
        // listen for jump-related keys down
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.Mouse0)) 
        {
            mov.jump = true;
        } else 
        {
            mov.jump = false;
        }

        // listen for crouch-related keys down
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.LeftShift)) 
        {
            mov.crouch = true; 
        } else 
        { 
            mov.crouch = false;
        }

        // listen for sprint-related keys down
        // not currently used
        // if (Input.GetKey(KeyCode.LeftControl))
        // {
            
        // } else
        // {
            
        // }

        // listen for left-movement-related keys down
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            mov.left = true;
        } else 
        {
            mov.left = false;
        }
        
        // listen for right-movement-related keys down
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            mov.right = true;
        } else 
        {
            mov.right = false;
        }
    }

    /// <summary>
    /// Update the facing variable based on the left/right movement keys that are pressed.
    /// </summary>
    void updateFacing()
    {
        if (mov.left)
        {
            facing = Direction1D.LEFT;
        }
        if (mov.right)
        {
            facing = Direction1D.RIGHT;
        }

        if (velocityVector.x > 0)
        {
            facingStrict = Direction1D.RIGHT;
        } else if (velocityVector.x < 0)
        {
            facingStrict = Direction1D.LEFT;
        }
    }

    /// <summary>
    /// Update player velocity based on keypresses.
    /// </summary>
    /// <remarks>
    /// XOR is used so that velocity is nullified if both left and right movement keys are pressed in addition to if neither are pressed.
    /// Directionality is handled by the facing variable. currentVelocity determines the speed at which the player is moving.
    /// When either A or D is pressed and S is pressed, the player moves at a crawling speed instead.
    /// </remarks>
    void setVelocity()
    {
        if (isOnGround)
        {
            if (mov.crouch)
            {
                currentVelocity.x = crawlSpeed.x;
            } else
            {
                currentVelocity.x = walkSpeed.x;
            }
        } else
        {
            currentVelocity.x = crawlSpeed.x * airTranslationFactor;
        }
    }

    /// <summary>
    /// Move the player based on their current speed force.
    /// </summary>
    /// <param name="force">The "force" speed to be applied</param>
    void moveForce(Vector2 force)
    {
        // apply air control factor if in air
        float f = 1;
        if (!isOnGround) { f = airForceFactor; }

        // apply force
        rb.AddForce(new Vector3((int)facing * force.x * f, force.y, 0), ForceMode2D.Force);
    }

    /// <summary>
    /// Move the player based on their current speed.
    /// </summary>
    /// <param name="speed"></param>
    void moveTranslate(Vector2 speed)
    {
        // apply immediate translation
        transform.Translate(new Vector3((int)facing * speed.x * Time.deltaTime, speed.y * Time.deltaTime, 0));
    }

    /// <summary>
    /// Limit how far the player can move in a single tick.
    /// </summary>
    /// <param name="distance">distance the player traveled in the last tick</param>
    /// <param name="limit">how far the player is allowed to travel in a second</param>
    // void limitSpeedX(float distance, float limit)
    // {
    //     if (Math.Abs(distance) > limit * Time.deltaTime)
    //     {
    //         transform.Translate(new Vector3(-1 * (distance - (limit * Time.deltaTime)), 0, 0));
    //         print("Speeding ticket!");
    //         print((currentPos.x - lastPos.x) / Time.deltaTime);
    //     }
    // }

    /// <summary>
    /// Updates the currentJumpForce.
    /// </summary>
    /// <param name="t">The amount of time that the pounce has  been charging</param>
    void updateJumpForce(float t)
    {
        pounceTicker = 0;

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
    /// Default method for updateJumpForce().
    /// </summary>
    /// <remarks>
    /// Uses pounceTicker by default
    /// </remarks>
    void updateJumpForce()
    {
        updateJumpForce(pounceTicker);
    }

    /// <summary>
    /// Update the pounceTicker variable.
    /// </summary>
    void chargePounce()
    {
        if (mov.crouch && isOnGround) {
            if (mov.left || mov.right)
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
    /// Make the player jump.
    /// </summary>
    void jump()
    {
        // update currentJumpForce accordingly
        updateJumpForce(pounceTicker);

        // provide a small boost for a moving jump (running/walking into a jump)
        float a = 0;
        if (currentJumpForce.x == jumpForce.x && currentJumpForce.y == jumpForce.y && (mov.left ^ mov.right))
        {
            if (movingJumpBoost / Math.Abs(velocityVector.x) < movingJumpBoost) // reference velocityVector only if the quotient is less than movingJumpBoost due to the nature of an inversely proportional relationship
            {
                a = movingJumpBoost / Math.Abs(velocityVector.x); // this creates a greater effect at lower speeds, i.e. the player just started moving
            } else
            {
                a = movingJumpBoost;
            }
        }

        // jump
        rb.AddForce(new Vector3((int)facing * (currentJumpForce.x + a), currentJumpForce.y, 0), ForceMode2D.Impulse);

        // reset the pounceTicker
        pounceTicker = 0;
    }





    // ----- ##### ----- ##### ------ ##### -----

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        collider = GetComponent<Collider2D>();
        rend = GetComponent<Renderer>();

        // groundLayer = LayerMask.GetMask("Ground");

        lastPos = transform.position;
    }

    /// <summary>
    /// Handles frame-based events and sends physics-based requests to FixedUpdate().
    /// </summary>
    /// <remarks>
    /// Runs once per render frame, which is variable.
    /// Handles immediate translational movement.
    /// Handles pounce charging.
    /// </remarks>
    void Update()
    {        
        // prepare relative variables (e.g. currentPos (corresponding to lastPos))
        currentPos = transform.position;
        velocityVector = rb.linearVelocity; // this does not account for non-force-based movement

        // update listeners
        eventListen();

        // update jump cooldown timer
        jumpTicker += Time.deltaTime;
        
        // apply speed limits
        // limitSpeedX(currentPos.x - lastPos.x, speedLimitWalk);

        // determine which way the player is facing
        updateFacing();

        // jump and pounce mechanics
        chargePounce(); 
        if (mov.jump && isOnGround && (jumpTicker >= jumpCooldown)) 
        {
            jumpRequest = true;
        }

        // left and right movement mechanics
        setVelocity();
        if (mov.left ^ mov.right)
        {
            moveRequest = true;
            moveTranslate(currentVelocity);
        }


        // debug
        print(facingStrict);

        // prepare for next tick
        currentVelocity.x = 0;
        currentVelocity.y = 0;
        lastPos = transform.position;
    }

    /// <summary>
    /// Handles the physics-based actions and movement on the player.
    /// </summary>
    /// <remarks>
    /// Runs once per physics frame, i.e. 50 times a second.
    /// Makes the player jump if a jump has been requested by Update().
    /// Applies the force-based left/right movement if requested by Update().
    /// </remarks>
    void FixedUpdate()
    {
        // jump if a jump has been requested
        if (jumpRequest)
        {
            jump();
            jumpRequest = false;
            isOnGround = false;
            jumpTicker = 0;
        }
        
        // move via forces if movement has been requested
        if (moveRequest) {
            moveForce(speedForce);
            moveRequest = false;
        }
    }

    /// <summary>
    /// Handles events when a collision occurs
    /// </summary>
    /// <param name="collision"></param>
    void OnCollisionEnter2D(Collision2D collision) // need to account for the case when the player hits an object before the ground, i.e. slides down a wall
    {
        foreach (ContactPoint2D i in collision.contacts)
        {
            if (i.normal == new Vector2(0, 1) && collider.IsTouchingLayers(LayerMasks.ground))
            {
                // the player is on the ground
                isOnGround = true;
                
                // apply a landing force if the player is trying to move and they are facing the same direction as when they left the ground
                if ((mov.left ^ mov.right) && facing == facingStrict)
                {
                    rb.AddForce(new Vector3((int)facing * landBoost, 0, 0), ForceMode2D.Impulse);
                }
            }
        }
    }

    /// <summary>
    /// Handles events as a collision happens continuously.
    /// </summary>
    /// <param name="collision"></param>
    void OnCollisionStay2D(Collision2D collision)
    {
        if (!isOnGround) { // only run if the player is not on the ground
            foreach (ContactPoint2D i in collision.contacts) // check every object that the player is in contact with
            {
                // the player is on the ground if any of the colliding objects is a ground object that is directly beneath the player
                if (i.normal == new Vector2(0, 1) && collider.IsTouchingLayers(LayerMasks.ground))
                {
                    isOnGround = true;
                }
            }
        }
    }

    /// <summary>
    /// Handles events as a collision ends
    /// </summary>
    /// <param name="collision"></param>
    void OnCollisionExit2D(Collision2D collision)
    {
        isOnGround = false; // if the player is not in contact with any collisions (i.e. just stopped colliding with anything), they must be in the air
    }





    // ----- ##### ----- ##### ----- ##### -----

    public enum Direction1D
    {
        LEFT = -1,
        RIGHT = 1
    }





    // ----- ##### ----- ##### ----- ##### -----

    /// <summary>
    /// Movement class used for booleans pertaining to movement.
    /// </summary>
    /// <remarks>
    /// Each boolean is representative of a specific movement option (e.g. left, jump, crouch, etc) and is used to determine when and if the player should perform a certain movement option.
    /// </remarks>
    public class Movement
    {
        public bool left;
        public bool right;
        public bool jump;
        public bool crouch;
        public bool sprint;

        public Movement()
        {
            this.left = false;
            this.right = false;
            this.jump = false;
            this.crouch = false;
            this.sprint = false;
        }
    }

    /// <summary>
    /// A class used to hold all of the tags.
    /// </summary>
    public class Tag
    {
        public static string ground = "Ground";
        public static string climbable = "Climbable";
    }

    /// <summary>
    /// A class used to hold all of the layer masks in a simplified form.
    /// </summary>
    public class LayerMasks
    {
        public static int ground = LayerMask.GetMask("Ground");
        public static int wall = LayerMask.GetMask("Wall");
        public static int player = LayerMask.GetMask("Player");
        public static int enemy = LayerMask.GetMask("Enemy");
        public static int npc = LayerMask.GetMask("NPC");
        public static int collectible = LayerMask.GetMask("Collectible");
    }
    
}
