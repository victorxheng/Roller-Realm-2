using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviourPun
{

    [Header("Movement")]
    public float moveSpeed;

    public float groundDrag;

    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;
    bool readyToJump;

    [Header("Keybinds")]
    public KeyCode jumpKey = KeyCode.Space;

    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask whatIsGround;
    bool grounded;

    public Transform orientation;//TODO: ADD ORIENTATION SEPERATE OBJECT

    float horizontalInput;
    float verticalInput;

    Joystick moveJoystick;

    Vector3 moveDirection;

    public Rigidbody rb;

    public CameraController cameraScript;

    public GameObject explosion;

    public static GameObject LocalPlayerInstance;

    private void Awake()
    {
        // #Important
        // used in GameManager.cs: we keep track of the localPlayer instance to prevent instantiation when levels are synchronized
        if (photonView.IsMine)
        {
            Movement.LocalPlayerInstance = this.gameObject;
        }
        // #Critical
        // we flag as don't destroy on load so that instance survives level synchronization, thus giving a seamless experience when levels load.
        DontDestroyOnLoad(this.gameObject);
    }
    private void Start()
    {
        spawnPosition = new Vector3(Random.Range(-10, 10), 5, Random.Range(-10, 10));
        transform.position = spawnPosition;
        rb.velocity = new Vector3(0, 0, 0);
        //rb.freezeRotation = true;
        ResetJump();

    }
    private void Update()
    {
        if (photonView.IsMine == false && PhotonNetwork.IsConnected == true)
        {
            return;
        }

        //ground check
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f);

        MyInput();
        //SpeedControl();

        //handle drag
        if (grounded)
            rb.drag = groundDrag;
        else
            rb.drag = 0.5f;

        CheckDeath();
    }

    private void MyInput()
    {
        //TODO: REPLACE WITH JOYSTICKS
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        //when jump
        if(Input.GetKey(jumpKey) && readyToJump && grounded)
        {
            readyToJump = false;

            Jump();

            Invoke(nameof(ResetJump), jumpCooldown);
        }

    }
    private void FixedUpdate()
    {
        if (photonView.IsMine == false && PhotonNetwork.IsConnected == true)
        {
            return;
        }
        MovePlayer();
    }

    private void MovePlayer()
    {
        //calculate movement direction
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        //on ground
        if (grounded)
            rb.AddForce(moveDirection * moveSpeed * 10f, ForceMode.Force);
        //in air
        else if (!grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);
    }

    private void SpeedControl()
    {
        Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        //limit velocity if needed
        if(flatVel.magnitude > moveSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * moveSpeed;
            rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
        }
    }

    private void Jump()
    {
        //reset y velocity
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        rb.AddForce(orientation.up * jumpForce, ForceMode.Impulse);

    }

    private void ResetJump()
    {
        readyToJump = true;
    }

    private void CheckDeath()
    {
        if(transform.position.y > 40 || transform.position.y < -10)
        {
            if (cameraScript.playerAlive && explosion != null)
            {
                Instantiate(explosion, transform.position, Quaternion.identity);
                Invoke("OnDeath", 3);
            }
            cameraScript.playerAlive = false;
        }
    }

    private Vector3 spawnPosition;
    private void OnDeath()
    {
        spawnPosition = new Vector3(Random.Range(-10, 10), 5, Random.Range(-10, 10));
        transform.position = spawnPosition;
        rb.velocity = new Vector3(0, 0, 0);
        cameraScript.playerAlive = true;
    }

}
