using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Movement : MonoBehaviour
{
    public float moveSpeed = 5f;
    private Vector2 inputVector;
    private Animator animator;
	private Rigidbody2D rb;

    public bool inputMove = false;
    public bool use3DSControls = false;
    public bool movementLocked = false; // New bool for movement lock

    public LayerMask collisionLayer;

    private KeyCode upKey;
    private KeyCode downKey;
    private KeyCode leftKey;
    private KeyCode rightKey;
    private KeyCode startKey;

    public UnityEvent onTriggerEvent;

    private void Awake()
    {
       animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>(); // Make sure Rigidbody2D is attached
        UpdateControlScheme();
    }

    void Start()
    {
        if (use3DSControls)
        {
            UnityEngine.N3DS.Keyboard.SetType(N3dsKeyboardType.Qwerty);
        }
    }

    private void Update()
    {
        bool upPressed, downPressed, leftPressed, rightPressed;

        if (!use3DSControls)
        {
            // Standard controls
            upPressed = Input.GetKey(upKey);
            downPressed = Input.GetKey(downKey);
            leftPressed = Input.GetKey(leftKey);
            rightPressed = Input.GetKey(rightKey);
        }
        else
        {
            // 3DS controls
            upPressed = UnityEngine.N3DS.GamePad.GetButtonHold(N3dsButton.Up);
            downPressed = UnityEngine.N3DS.GamePad.GetButtonHold(N3dsButton.Down);
            leftPressed = UnityEngine.N3DS.GamePad.GetButtonHold(N3dsButton.Left);
            rightPressed = UnityEngine.N3DS.GamePad.GetButtonHold(N3dsButton.Right);
        }

        // Calculate movement vector based on key presses
        float horizontalInput = (rightPressed ? 1f : 0f) - (leftPressed ? 1f : 0f);
        float verticalInput = (upPressed ? 1f : 0f) - (downPressed ? 1f : 0f);

        // Calculate movement vector
        inputVector = new Vector2(horizontalInput, verticalInput).normalized;

        // Update the animation direction only if there is input
        if (inputVector != Vector2.zero)
        {
            UpdateAnimationDirection();
        }

        isMoving();

        bool startPressed = use3DSControls ? 
            UnityEngine.N3DS.GamePad.GetButtonHold(N3dsButton.A) : Input.GetKey(startKey);

        if (startPressed)
        {
            animator.SetBool("Cross", true);
            movementLocked = true; // Lock movement when "Cross" is set to true
        }
        else
        {
            animator.SetBool("Cross", false);
            movementLocked = false; // Unlock movement when "Cross" is false
        }
    }

        private void FixedUpdate()
    {
        if (!movementLocked)
        {
            Vector2 movement = inputVector * moveSpeed * Time.fixedDeltaTime;
            MoveCharacter(movement);
        }
    }

    private void MoveCharacter(Vector2 movement)
    {
        Vector2 newPosition = rb.position + movement;
        if (!IsColliding(newPosition))
        {
            rb.MovePosition(newPosition);
        }
    }

    private bool IsColliding(Vector2 targetPosition)
    {
        // Assuming your character's collider is a BoxCollider2D
        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();

        Collider2D hit = Physics2D.OverlapBox(targetPosition, boxCollider.size, 0, collisionLayer);
        return hit != null;
    }

    private void UpdateControlScheme()
    {
        if (!use3DSControls)
        {
            upKey = KeyCode.W;
            downKey = KeyCode.S;
            leftKey = KeyCode.A;
            rightKey = KeyCode.D;
            startKey = KeyCode.Space;
        }
        else
        {
            // For 3DS controls, keys will be handled in the Update method
            upKey = downKey = leftKey = rightKey = startKey = KeyCode.None;
        }
    }

    private bool AreAnyKeysPressed()
    {
        if (!use3DSControls)
        {
            return Input.GetKey(upKey) || Input.GetKey(downKey) || Input.GetKey(leftKey) || Input.GetKey(rightKey);
        }
        else
        {
            // Check for 3DS button holds
            return UnityEngine.N3DS.GamePad.GetButtonHold(N3dsButton.Up) ||
                   UnityEngine.N3DS.GamePad.GetButtonHold(N3dsButton.Down) ||
                   UnityEngine.N3DS.GamePad.GetButtonHold(N3dsButton.Left) ||
                   UnityEngine.N3DS.GamePad.GetButtonHold(N3dsButton.Right);
        }
    }

    private void UpdateAnimationDirection()
    {
        // Calculate the direction for animation based on input vector
        float angle = Mathf.Atan2(inputVector.y, inputVector.x) * Mathf.Rad2Deg;
        if (angle < 0)
        {
            angle += 360;
        }

        // Determine the animation direction
        int animationDirection = Mathf.RoundToInt(angle / 45.0f);
        animator.SetInteger("Direction", animationDirection);
    }


    private void isMoving()
    {
        // Check if there is any input (either horizontal or vertical).
        if (!AreAnyKeysPressed())
        {
            // No input, set the boolean to false.
            animator.speed = 0f;
            inputMove = false;
        }
        else
        {
            // There is input, set the boolean to true.
            animator.speed = 1f;
            inputMove = true;
        }

        // Update the Animator parameter with the isMoving value.
        animator.SetBool("IsMoving", inputMove);
    }
  void OnTriggerEnter2D(Collider2D other)
    {
        bool startPressed = use3DSControls ? 
            UnityEngine.N3DS.GamePad.GetButtonHold(N3dsButton.A) : Input.GetKey(startKey);
        Possessed script = other.gameObject.GetComponent<Possessed>();
        if (script != null && startPressed)
        {
            script.Collided();
        }

        if (onTriggerEvent != null)
        {
            onTriggerEvent.Invoke();
        }
    }
}