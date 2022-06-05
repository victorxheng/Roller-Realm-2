using Photon.Pun;
using UnityEngine;

public class Movement : MonoBehaviourPun
{

    [Header("Movement")]
    public float moveSpeed;

    public float groundDrag;

    public float jumpForce;
    public float jumpCooldownTime;
    public float airDecreaseFactor;
    bool isPlayerReadyToJump;

    [Header("Keybinds")]
    public KeyCode jumpKey = KeyCode.Space;

    [Header("Ground Check")]
    public float playerHeight;
    private bool grounded = false;

    public Transform orientation;//TODO: ADD ORIENTATION SEPERATE OBJECT

    float horizontalInput;
    float verticalInput;

    Joystick moveJoystick;

    Vector3 orientationForwardDirection;

    public Rigidbody rb;

    public CameraController cameraScript;

    public GameObject explosion;

    public static GameObject LocalPlayerInstance;


    public bool hitByOwnerBullet = false;

    public Joystick joystick;
    public ButtonState jumpButton;
    public LayerMask whatIsGround;

    private void Awake()
    {
        // #Important
        // used in GameManager.cs: we keep track of the localPlayer instance to prevent instantiation when levels are synchronized
        if (photonView.IsMine)
        {
            Movement.LocalPlayerInstance = this.gameObject;
            Camera.main.gameObject.GetComponent<CameraController>().FollowTransform = transform;
            Camera.main.gameObject.SetActive(true);
            //orientation = GameObject.Find("Orientation").transform;
            cameraScript = Camera.main.gameObject.GetComponent<CameraController>();
            joystick = GameObject.Find("Joystick").GetComponent<Joystick>();
            jumpButton = GameObject.Find("JumpButton").GetComponent<ButtonState>();
        }
        // #Critical
        // we flag as don't destroy on load so that instance survives level synchronization, thus giving a seamless experience when levels load.
        DontDestroyOnLoad(this.gameObject);
    }
    private void Start()
    {
        spawnPosition = new Vector3(Random.Range(-25, 25), 8, Random.Range(-25, 25));
        transform.position = spawnPosition;
        rb.velocity = new Vector3(0, 0, 0);
        //rb.freezeRotation = true;
        ResetJump();

    }
    private void Update()
    {

        if (photonView.IsMine == false && PhotonNetwork.IsConnected == true)
        {
            //ground check
            grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f);

            //handle drag
            if (grounded)
                rb.drag = groundDrag;
           else
                rb.drag = 0.1f;
            LimitSpeed();
            CheckNetworkDeath();
            return;
        }

        //ground check
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.3f, whatIsGround);
        
        ReadInput();

        LimitSpeed();
        //handle drag
        if (grounded)
            rb.drag = groundDrag;
        else
            rb.drag = 0.1f;

        CheckDeath();
    }

    private void ReadInput()
    {
        horizontalInput = Mathf.Sign(joystick.Horizontal) * Mathf.Pow(joystick.Horizontal,2);
        verticalInput = Mathf.Sign(joystick.Vertical) * Mathf.Pow(joystick.Vertical, 2);
        if(grounded)
        {
            //when jump
            if (jumpButton.pressed && isPlayerReadyToJump)
            {
                isPlayerReadyToJump = false;

                Jump();

                Invoke(nameof(ResetJump), jumpCooldownTime);
            }
        }

    }
    private void FixedUpdate()
    {
        if (photonView.IsMine == false && PhotonNetwork.IsConnected == true)
        {
            return;
        }
        MovePlayerInDirection();
    }

    private void MovePlayerInDirection()
    {
        //calculate movement direction
        orientationForwardDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        //on ground
        if (grounded)
            rb.AddForce(orientationForwardDirection * moveSpeed * 10f, ForceMode.Force);
        //in air
        else if (!grounded)
            rb.AddForce(orientationForwardDirection.normalized * moveSpeed * 10f * airDecreaseFactor, ForceMode.Force);
    }

    private void LimitSpeed()
    {
        Vector3 limitedVelocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        //limit velocity if needed
        if(limitedVelocity.magnitude > moveSpeed)
        {
            Vector3 limitedVel = limitedVelocity.normalized * moveSpeed;
            rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
        }
    }

    private void Jump()
    {
        //reset the y velocity component
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        //add jump force to rigidbody component
        rb.AddForce(orientation.up * jumpForce, ForceMode.Impulse);
    }

    private void ResetJump()
    {
        isPlayerReadyToJump = true;
    }

    private float lowerY = -10;
    private float upperY = 50;
    private bool networkAlive = true;
    private void CheckNetworkDeath()
    {
        if (transform.position.y > upperY || transform.position.y < lowerY)
        {
            if (networkAlive)
            {
                if (hitByOwnerBullet)
                {
                    ScoreManager.SessionKill();
                    GameObject.Find("Launcher").GetPhotonView().RPC("RecievedDeath", photonView.Owner, ScoreManager.rating);
                }
            }
            networkAlive = false;
        }
        else if (!networkAlive)
        {
            networkAlive = true;
        }
    }
    private void CheckDeath()
    {
        if(transform.position.y > upperY || transform.position.y < lowerY)
        {
            if (cameraScript.playerAlive && explosion != null)
            {
                if (PhotonNetwork.CurrentRoom.PlayerCount == 1) GameObject.Find("Launcher").GetComponent<ScoreboardManager>().RobotDeath();
                ScoreManager.SessionDeath();
                Instantiate(explosion, transform.position, Quaternion.identity);
                Invoke("OnDeath", 3);
            }
            cameraScript.playerAlive = false;
        }
    }

    private Vector3 spawnPosition;
    private void OnDeath()
    {
        spawnPosition = new Vector3(Random.Range(-25, 25), Random.Range(8,15), Random.Range(-25, 25));
        transform.position = spawnPosition;
        rb.velocity = new Vector3(0, 0, 0);
        cameraScript.playerAlive = true;
    }

}
