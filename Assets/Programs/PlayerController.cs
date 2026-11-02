using UnityEngine;
using UnityEngine.UIElements;

public class PlayerController : MonoBehaviour
{
    [SerializeField] public const float maxHold = 1.5f; // number of seconds after holding space that currentJumpForce = pounceForce
    [SerializeField] public const float minHold = 0.2f;
    [SerializeField] public Vector2 jumpForce = new Vector2(0f, 15f);
    [SerializeField] public Vector2 pounceForce = new Vector2(40f, 5f);
    [SerializeField] public Vector2 speedForce = new Vector2(5f, 0f);
    [SerializeField] public Vector2 walkSpeed = new Vector2(3f, 0f);
    [SerializeField] public Vector2 sprintSpeed = new Vector2(5f, 0f);
    
    private static Rigidbody2D rb;

    private static Direction1D facing = Direction1D.RIGHT;
    private float climbTime = 3.0f; // amount of time in seconds that a player can climb

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
        } else if (t <= minHold) // default to the jumpForce values if the jump button is not held longer than minHold
        {
            currentJumpForce.x = jumpForce.x;
            currentJumpForce.y = jumpForce.y;
        } else // adjust jump force based on the amount of time the jump button is held
        {
            currentJumpForce.x = (int)facing * (float)(((pounceForce.x - jumpForce.x) / (maxHold - minHold) * (t - minHold)) + jumpForce.x);
            currentJumpForce.y = (float)(((pounceForce.y - jumpForce.y) / (maxHold - minHold) * (t - minHold)) + jumpForce.y);
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        lastPos = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        currentPos = transform.position;
        
        // jump and pounce mechanics
        if (Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.W))
        {
            pounceTicker += Time.deltaTime;
            
            // make it so that the player cannot move while charging a pounce
            currentVelocity.x = 0;
            currentVelocity.y = 0;
        }
        if (Input.GetKeyUp(KeyCode.Space) || Input.GetKeyUp(KeyCode.W))
        {
            updateJumpForce(pounceTicker);

            rb.AddForce(new Vector3(currentJumpForce.x, currentJumpForce.y, 0), ForceMode2D.Impulse);

            pounceTicker = 0;
        }

        // facing variable setter
        if (Input.GetKey(KeyCode.A)) { facing = Direction1D.LEFT; }
        if (Input.GetKey(KeyCode.D)) { facing = Direction1D.RIGHT; }

        // velocity setter
        if (Input.GetKey(KeyCode.A) ^ Input.GetKey(KeyCode.D))
        {
            currentVelocity.x = walkSpeed.x;
        } else
        {
            currentVelocity.x = 0;
        }

        // left and right movement mechanics
        if (Input.GetKey(KeyCode.A) ^ Input.GetKey(KeyCode.D)) { rb.AddForce(new Vector3(speedForce.x, speedForce.y, 0), ForceMode2D.Force); }
        transform.Translate(new Vector3((int)facing * currentVelocity.x * Time.deltaTime, currentVelocity.y * Time.deltaTime, 0));

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
