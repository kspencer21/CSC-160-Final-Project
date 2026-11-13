using UnityEngine;
using UnityEngine.UIElements;
using System;

public class PlayerController : MonoBehaviour
{
    // ===========================================================================================================================================
    // ========== VARIABLES ======================================================================================================================
    // ===========================================================================================================================================

    // object components
    private Rigidbody2D rb;
    private Collider2D collider;
    private Renderer rend;

    // layer masks
    private int groundLayer;

    // tags
    private int groundTag;
    private int climbableTag;
    
    // constants
    private const float maxHold = 0.6f; // number of seconds that the player must hold crouch before pouncing for currentJumpForce = pounceForce
    private const float minHold = 0.1f; // number of seconds the player must hold a jump key for currentJumpForce to begin changing to average of jumpForce and pounceForce
    private const float climbTime = 3f; // amount of time in seconds that a player can climb
    private const float jumpCooldown = 0.4f; // amount of time in seconds after jumping that a player can jump again
    private const float limitNullifierLength = 0.5f; // amount of time in seconds that limitSpeedX() is ignored after a pounce

    private const float infinity = 1048576f; // an arbitrarily large number (equal to 2^20) representing an absence of an upper bound (used in serialized field bounds)

    // serialized variables (can be modified in the editor)
    [SerializeField] public Vector2 jumpForce = new Vector2(0f, 18f); // the amount of force to be applied to the player when they jump
    [SerializeField] public Vector2 pounceForce = new Vector2(18f, 10f); // the amount of force to be applied to the player after a fully charged pounce
    [SerializeField] public float movingJumpBoost = 5f; // additional force to be applied only when the player is moving when they jump. Modified by the player's velocityVector
    [SerializeField] public float landBoost = 1.5f; // additional force to be applied when the player lands to account for the sudden friction
    [SerializeField] public Vector2 speedForce = new Vector2(20f, 0f); // speedForce determines how much the translational movement is affected by acceleration (via forces)
    [SerializeField] public Vector2 walkSpeed = new Vector2(2f, 0f); // walkSpeed determines how much the translational movement is immediate (direct translation)
    [SerializeField] public Vector2 crawlSpeed = new Vector2(1f, 0f); // crawlSpeed is an alternative to walkSpeed used when the player is crouched
    [SerializeField] public Vector2 climbSpeed = new Vector2(0f, 5f);
    [SerializeField] public float airForceFactor = 0.2f; // how much to multiply the speedForce by when in air
    [SerializeField] public float airTranslationFactor = 0.8f; // how much to multiply the crawlSpeed by when in air
    [SerializeField] public float speedLimitWalk = 7f; // how far the player can move in a second when walking
    [SerializeField] public float speedLimitCrawl = 3f;

    // serialized field bounds
    private Bounds<Vector2> jumpForceBounds = new Bounds<Vector2>
    (
        new Vector2(0f, 0f), // upper bound of both x and y components
        new Vector2(0f, infinity) // lower bounds of both x and y components
    );
    private Bounds<Vector2> pounceForceBounds = new Bounds<Vector2>
    (
        new Vector2(0f, 0f),
        new Vector2(infinity, infinity)
    );
    private Bounds<float> jumpBoostBounds = new Bounds<float>
    (
        0f, // upper bound
        infinity // lower bound
    );
    private Bounds<float> landBoostBounds = new Bounds<float>
    (
        0f,
        infinity
    );
    private Bounds<Vector2> speedForceBounds = new Bounds<Vector2>
    (
        new Vector2(0f, 0f),
        new Vector2(infinity, 0f)
    );
    private Bounds<Vector2> walkSpeedBounds = new Bounds<Vector2>
    (
        new Vector2(0f, 0f),
        new Vector2(infinity, 0f)
    );
    private Bounds<Vector2> crawlSpeedBounds = new Bounds<Vector2>
    (
        new Vector2(0f, 0f),
        new Vector2(infinity, 0f)
    );
    private Bounds<Vector2> climbSpeedBounds = new Bounds<Vector2>
    (
        new Vector2(0f, 0f),
        new Vector2(0f, infinity);
    );
    private Bounds<float> airForceFactorBounds = new Bounds<float>
    (
        0f,
        1f
    );
    private Bounds<float> airTranslationFactorBounds = new Bounds<float>
    (
        0f,
        1f
    );
    private Bounds<float> speedLimitWalkBounds = new Bounds<float>
    (
        0f,
        infinity
    );
    private Bounds<float> speedLimitCrawlBounds = new Bounds<float>
    (
        0f,
        infinity
    );
    
    // player states
    private Direction1D facing = Direction1D.RIGHT; // determines which way the player is facing
    private Direction1D facingStrict = Direction1D.RIGHT; // a parallel to the facing variable which is only altered when the player is on the ground
    private Direction1D climbingFacing = Direction1D.NULL; // the direction the player is facing while climbing. NULL if the player is not climbing
    private Movement mov = new Movement(); // a class object containing the player's movement states
    private bool isOnGround = false;
    private bool isClimbing = false;
    private Vector2 velocityVector; // the physics-based (i.e. from applied forces) velocity of the player

    // Update() and FixedUpdate() interactions
    private bool jumpRequest = false;
    private bool moveRequest = false;

    // tickers and counters
    private float pounceTicker = 0f; // amount of time in seconds that a pounce has been charged
    private float jumpTicker = jumpCooldown; // amount of time in seconds after the last jump
    private float limitNullifier = limitNullifierLength; // amount of time after a pounce. Used to nullify the limitSpeedX() method and enable longer pounces

    // relative values
    private Vector3 lastPos;
    private Vector3 currentPos;

    // current values
    private Vector2 currentJumpForce = new Vector2(0f, 0f);
    private Vector2 currentVelocity = new Vector2(0f, 0f);
    



    
    // ===========================================================================================================================================
    // ========== METHODS ========================================================================================================================
    // ===========================================================================================================================================

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
        // update facing based on inputs
        if (mov.left && !mov.right)
        {
            facing = Direction1D.LEFT;
        } else if (mov.right && !mov.left)
        {
            facing = Direction1D.RIGHT;
        } else
        {
            // facing = Direction1D.NULL;
        }

        // update facingStrict based on player's velocity direction
        if (velocityVector.x > 0)
        {
            facingStrict = Direction1D.RIGHT;
        } else if (velocityVector.x < 0)
        {
            facingStrict = Direction1D.LEFT;
        } else
        {
            // facingStrict = Direction1D.NULL;
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
        if (!isClimbing)
        {
            // apply air control factor if in air
            float f = 1;
            if (!isOnGround) { f = airForceFactor; }

            // apply force
            rb.AddForce(new Vector3((int)facing * force.x * f, force.y, 0), ForceMode2D.Force);
        }
    }

    /// <summary>
    /// Move the player based on their current speed.
    /// </summary>
    /// <param name="speed"></param>
    void moveTranslate(Vector2 speed)
    {
        if (!isClimbing) 
        {
            // apply immediate translation
            transform.Translate(new Vector3((int)facing * speed.x * Time.deltaTime, speed.y * Time.deltaTime, 0));
        }
    }

    /// <summary>
    /// Limit how far the player can move in a single tick.
    /// </summary>
    /// <param name="velocity">physics-based velocity of the player</param>
    /// <param name="speed">translation-based velocity of the player</param>
    /// <param name="limit">how far the player is allowed to travel in a second</param>
    /// <remarks>
    /// This only affects physics-based velocity.
    /// </remarks>
    void limitSpeedX(float velocity, float speed, float limit)
    {
        if ((Math.Abs(velocity + ((int)facingStrict * speed)) > limit) && (limitNullifier > limitNullifierLength))
        {
            rb.linearVelocity = new Vector2((int)facingStrict * (limit - speed), rb.linearVelocity.y);
        }
    }

    /// <summary>
    /// Default equivalent of limitSpeedX(float, float)
    /// </summary>
    /// <remarks>
    /// Uses rb.linearVelocity.x in place of velocity
    /// Uses walkSpeed.x in place of speed
    /// Uses speedLimitWalk in place of limit
    /// </remarks>
    void limitSpeedX(float limit)
    {
        limitSpeedX(rb.linearVelocity.x, walkSpeed.x, limit);
        // if ((Math.Abs(rb.linearVelocity.x + ((int)facingStrict) * walkSpeed.x) > speedLimitWalk) && (limitNullifier > limitNullifierLength))
        // {
        //     rb.linearVelocity = new Vector2((int)facingStrict * (speedLimitWalk - walkSpeed.x), rb.linearVelocity.y);
        // }
    }

    /// <summary>
    /// Updates the currentJumpForce.
    /// </summary>
    /// <param name="t">The amount of time that the pounce has  been charging</param>
    void updateJumpForce(float t)
    {
        // reset pounceTicker
        pounceTicker = 0;

        if (t >= maxHold)
        {
            // set the jump force to pounceForce
            currentJumpForce.x = pounceForce.x;
            currentJumpForce.y = pounceForce.y;

            // reset the speed limit nullifier ticker
            limitNullifier = 0;
        } else if (t <= minHold)
        {
            // set the jump force to jumpForce
            currentJumpForce.x = jumpForce.x;
            currentJumpForce.y = jumpForce.y;
        } else
        {
            // gradually transition jump force from jumpForce to pounceForce
            // currentJumpForce.x = (float)(((pounceForce.x - jumpForce.x) / (maxHold - minHold) * (t - minHold)) + jumpForce.x);
            // currentJumpForce.y = (float)(((pounceForce.y - jumpForce.y) / (maxHold - minHold) * (t - minHold)) + jumpForce.y);

            // set the jump force to the average between jumpForce and pounceForce
            currentJumpForce.x = Math.Abs(pounceForce.x - jumpForce.x) / 2;
            currentJumpForce.y = pounceForce.y;

            // reset the speed limit nullifier ticker
            limitNullifier = 0; 
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
    /// TODO: when jumping up directly alongside a platform, the player sometimes jumps as their bottom edge passes the platform's upper edge
    void jump()
    {
        // update currentJumpForce accordingly
        updateJumpForce(pounceTicker);

        // jump
        rb.AddForce(new Vector3(((int)facingStrict * currentJumpForce.x) + ((int)facing * addMovingJumpBoost()), currentJumpForce.y, 0), ForceMode2D.Impulse);
        
        // reset the pounceTicker
        pounceTicker = 0;
    }

    /// <summary>
    /// Applies a force to the player when they land.
    /// </summary>
    /// <remarks>
    /// This is called by OnCollisionEnter()
    /// </remarks>
    void addLandForce()
    {
        rb.AddForce(new Vector3((int)facing * landBoost, 0, 0), ForceMode2D.Impulse);
    }

    /// <summary>
    /// Add a boost as the player jumps.
    /// </summary>
    /// <return>A float to be additionally applied to the x jump force</return>
    /// <remarks>
    /// This is to counteract the sudden decrease in player speed as they lose some control in the air
    /// </remarks>
    float addMovingJumpBoost()
    {
        float a = 0;
        if (currentJumpForce.x == jumpForce.x && currentJumpForce.y == jumpForce.y && (mov.left ^ mov.right))
        {
            if (movingJumpBoost / (Math.Abs(velocityVector.x) + walkSpeed.x) < movingJumpBoost) // reference velocityVector only if the quotient is less than movingJumpBoost due to the nature of an inversely proportional relationship
            {
                a = movingJumpBoost / (Math.Abs(velocityVector.x) + walkSpeed.x); // this creates a greater effect at lower speeds, i.e. the player just started moving
            } else
            {
                a = movingJumpBoost;
            }
        }
        return a;
    }
    
    /// <summary>
    /// Begin climbing.
    /// </summary>
    void startClimb()
    {
        climbingFacing = facingStrict;
        transform.rotation = Quaternion.Euler(0, 0, 90 * (int)climbingFacing);
        transform.Translate(Math.Abs(rend.bounds.size.x - rend.bounds.size.y) / 2, Math.Abs(rend.bounds.size.x - rend.bounds.size.y) / 2, 0, Space.World);
        rb.gravityScale = 0f;
        isOnGround = false; // the player should not be able to jump or move as though they were on the ground
    }

    /// <summary>
    /// Applies upper and lower bounds to a variable.
    /// </summary>
    /// <param name="value">The variable to be affected</param>
    /// <param name="upper">The upper bound of the value</param>
    /// <param name="lower">The lower bound of the value</param>
    /// <remarks>
    /// The value parameter is a reference parameter so that the variable put in can be altered from within the method
    /// </remarks>
    void bind(ref float value, float upper, float lower)
    {
        if (value > upper) { value = upper; }
        if (value < lower) { value = lower; }
    }

    /// <summary>
    /// Apply upper and lower bounds to all serialized fields.
    /// </summary>
    void bindFields()
    {
        bind(ref jumpForce.x, jumpForceBounds.max.x, jumpForceBounds.min.x);
        bind(ref jumpForce.y, jumpForceBounds.max.y, jumpForceBounds.min.y);

        bind(ref pounceForce.x, pounceForceBounds.max.x, jumpForce.x); // the lower bound is jumpForce.x because the pounceForce x component should always be greater than the jumpForce x component, i.e. the player should jump farther with a pounce
        bind(ref pounceForce.y, jumpForce.y, pounceForceBounds.min.y); // the upper bound is jumpForce.y because the pounceForce y component should never exceedd the jumpForce y component, i.e. the player should not jump higher with a pounce

        bind(ref movingJumpBoost, jumpBoostBounds.max, jumpBoostBounds.min);

        bind(ref landBoost, landBoostBounds.max, landBoostBounds.min);

        bind(ref speedForce.x, speedForceBounds.max.x, speedForceBounds.min.x);
        bind(ref speedForce.y, speedForceBounds.max.y, speedForceBounds.min.y);

        bind(ref walkSpeed.x, walkSpeedBounds.max.x, walkSpeedBounds.min.x);
        bind(ref walkSpeed.y, walkSpeedBounds.max.y, walkSpeedBounds.min.y);

        bind(ref crawlSpeed.x, walkSpeed.x, crawlSpeedBounds.min.x); // the upper bound is walkSpeed.x because the crawlSpeed x component should never exceed the walkSpeed x component, i.e. the player should be slower when crawling
        bind(ref crawlSpeed.y, crawlSpeedBounds.max.y, crawlSpeedBounds.min.y);

        bind(ref climbSpeed.x, climbSpeedBounds.max.x, climbSpeedBounds.min.x);
        bind(ref climbSpeed.y, climbSpeedBounds.max.y, climbSpeedBounds.min.y);

        bind(ref airForceFactor, airForceFactorBounds.max, airForceFactorBounds.min);

        bind(ref airTranslationFactor, airTranslationFactorBounds.max, airTranslationFactorBounds.min);
        
        bind(ref speedLimitWalk, speedLimitWalkBounds.max, speedLimitWalkBounds.min);
        bind(ref speedLimitCrawl, speedLimitWalk, speedLimitCrawlBounds.min); // the upper bound is speedLimitWalk because the speed limit for crawling should never exceed the speed limit for walking, i.e. the player should be slower when crawling
    }

    /// <summary>
    /// Checks if the player is colliding with ground.
    /// </summary>
    /// <param name="normal">The normal vector of the player's collision</param>
    /// <returns>A boolean representative of whether or not the player should be considered to be standing on ground</returns>
    /// <remarks>
    /// <format type="text/markdown">
    /// The collider does not check the layer mask on each iteration of the foreach loop (see OnCollisionEnter() and OnCollisionStay())
    /// Because of this, edge cases may arise where this method returns true when it shouldn't, if:
    /// - The player is colliding with any object with layer mask Mask.ground ("Ground"), and
    /// - The player is colliding with an object with a collider of any layer mask such that its normal vector is (0, 1)
    /// </format>
    /// </remarks>
    bool isCollidingGround(Vector2 normal)
    {
        if (normal == new Vector2(0, 1) && collider.IsTouchingLayers(Mask.ground))
        {
            return true;
        } else
        {
            return false;
        }
    }
    
    /// <summary>
    /// Checks if the player is colliding with a climbable object.
    /// </summary>
    /// <param name="normal">The normal vector of the player's collision</param>
    /// <param name="collision">The object containing all details of the collision</param>
    /// <returns>A boolean representative of whether or not the player should be considered to be standing on ground</returns>
    /// <remarks>
    /// <format type="text/markdown">
    /// The collider does not check the layer mask on each iteration of the foreach loop (see OnCollisionEnter() and OnCollisionStay())
    /// Because of this, edge cases may arise where this method returns true when it shouldn't, if:
    /// - The player is colliding with any object with layer mask Mask.ground ("Ground"),
    /// - The player is colliding with an object with a collider of any layer mask such that its normal vector is (1, 0) or (-1, 0), and
    /// - The player is colliding with an object with a collider of any layer mask that has the tag "Climbable"
    /// </format>
    /// </remarks>
    bool isCollidingClimbable(Vector2 normal, Collision2D collision)
    {
        if (
            (normal == new Vector2(1, 0) || normal == new Vector2(-1, 0)) && 
            collider.IsTouchingLayers(Mask.ground) && 
            collision.gameObject.CompareTag("Climbable"))
        {
            return true;
        } else
        {
            return false;
        }
    }





    // ===========================================================================================================================================
    // ========== UNITY METHODS ==================================================================================================================
    // ===========================================================================================================================================

    /// <summary>
    /// Sets component variables (i.e. rigidbody, renderer, etc.) as well as difference variables (i.e. lastPos).
    /// </summary>
    void Start()
    {
        // define component variables
        rb = GetComponent<Rigidbody2D>();
        collider = GetComponent<Collider2D>();
        rend = GetComponent<Renderer>();

        // set initial relative values
        lastPos = transform.position;

        // ensure serialized fields are in reasonable bounds
        bindFields();
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

        // update listeners
        eventListen();

        // update tikers
        jumpTicker += Time.deltaTime;
        limitNullifier += Time.deltaTime;

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
        // update velocityVector
        velocityVector = rb.linearVelocity; // this does not account for non-force-based movement

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
        
        // apply speed limit
        if (!mov.crouch && !mov.sprint)
        {
            limitSpeedX(speedLimitWalk);
        } else if (mov.crouch)
        {
            limitSpeedX(speedLimitCrawl);
        } else
        {
            // apply sprint speed limit, if sprint is implemented
        }


        // debug
        // print(velocityVector.x + (int)facingStrict * walkSpeed.x);
    }

    /// <summary>
    /// Handles events when a collision occurs
    /// </summary>
    /// <param name="collision"></param>
    void OnCollisionEnter2D(Collision2D collision) // need to account for the case when the player hits an object before the ground, i.e. slides down a wall
    {
        foreach (ContactPoint2D i in collision.contacts)
        {
            // test if the player is on the ground
            if (isCollidingGround(i.normal) && !isOnGround)
            {
                // the player is on the ground
                isOnGround = true;
                
                // apply a landing force if the player is trying to move and they are facing the same direction as when they left the ground
                if ((mov.left ^ mov.right) && facing == facingStrict)
                {
                    addLandForce();
                }
            }

            if (isCollidingClimbable(i.normal, collision) && !isClimbing)
            {
                // the player is climbing
                isClimbing = true;
                startClimb();

                print("Climbing!");
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
                if (isCollidingGround(i.normal))
                {
                    isOnGround = true;
                }
            }
        }

        if (!isClimbing)
        {
            foreach(ContactPoint2D i in collision.contacts)
            {
                if (isCollidingClimbable(i.normal, collision))
                {
                    isClimbing = true;
                    startClimb();
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





    // ===========================================================================================================================================
    // ========== EUNUMERATORS ===================================================================================================================
    // ===========================================================================================================================================

    /// <summary>
    /// Enumerator with two values describing left, right, or neutral
    /// </summary>
    public enum Direction1D
    {
        LEFT = -1,
        NULL = 0,
        RIGHT = 1
    }





    // ===========================================================================================================================================
    // ========== CLASSES ========================================================================================================================
    // ===========================================================================================================================================

    /// <summary>
    /// Movement class used for booleans pertaining to movement.
    /// </summary>
    /// <remarks>
    /// Each boolean is representative of a specific movement option (e.g. left, jump, crouch, etc.) and is used to determine when and if the player should perform a certain movement option.
    /// </remarks>
    public class Movement
    {
        public bool left;
        public bool right;
        public bool jump;
        public bool crouch;
        public bool sprint;

        /// <summary>
        /// Specific constructor for Movement objects
        /// </summary>
        /// <param name="left">Boolean representing whether or not a "left" key is pressed</param>
        /// <param name="right">Boolean representing whether or not a "right" key is pressed</param>
        /// <param name="jump">Boolean representing whether or not a "jump" key is pressed</param>
        /// <param name="crouch">Boolean representing whether or not a "crouch" key is pressed</param>
        /// <param name="sprint">Boolean representing whether or not the player is sprinting</param>
        public Movement(bool left, bool right, bool jump, bool crouch, bool sprint)
        {
            this.left = left;
            this.right = right;
            this.jump = jump;
            this.crouch = crouch;
            this.sprint = sprint;
        }
        
        /// <summary>
        /// Generic constructor
        /// </summary>
        /// <remarks>
        /// This assumes that all attributes are to be false
        /// </remmakrs>
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
    public class Mask
    {
        public static int ground = LayerMask.GetMask("Ground");
        public static int wall = LayerMask.GetMask("Wall");
        public static int player = LayerMask.GetMask("Player");
        public static int enemy = LayerMask.GetMask("Enemy");
        public static int npc = LayerMask.GetMask("NPC");
        public static int collectible = LayerMask.GetMask("Collectible");
    }

    /// <summary>
    /// Contains the minimum and maximum values for a particular serialized field.
    /// </summary>
    /// <typeparam name="T">The type of parameters to be inputted</typeparam>
    /// <remarks>
    /// This class is configured to use different input types so that it can contain upper and lower bounds of type float, Vector2, and others
    /// Bounds associated with Vector2 variables, such as jumpForceBounds, are a Bounds of type Vector2 so that it may contain the jumpForce bounds for both x and y components
    /// </remarks>
    public class Bounds<T>
    {
        public T min;
        public T max;

        public Bounds(T min, T max) {
            this.min = min;
            this.max = max;
        }
    }
    
}
