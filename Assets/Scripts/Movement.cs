using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{
    public float moveSpeed = 5f;
    private Vector2 inputVector;
    private Animator animator;

    public bool inputMove = false;
    public bool use3DSControls = false;
    public bool movementLocked = false; // New bool for movement lock

    private KeyCode upKey;
    private KeyCode downKey;
    private KeyCode leftKey;
    private KeyCode rightKey;
    private KeyCode startKey;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        UpdateControlScheme();
    }

    private void Update()
    {
        // Check for key presses based on the control scheme
        bool upPressed = Input.GetKey(upKey);
        bool downPressed = Input.GetKey(downKey);
        bool leftPressed = Input.GetKey(leftKey);
        bool rightPressed = Input.GetKey(rightKey);

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
        if (Input.GetKey(startKey))
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
        if (!movementLocked) // Check if movement is not locked
        {
            // Move the player based on the input vector
            Vector2 movement = inputVector * moveSpeed * Time.fixedDeltaTime;
            transform.Translate(movement);
        }
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
            upKey = KeyCode.UpArrow;
            downKey = KeyCode.DownArrow;
            leftKey = KeyCode.LeftArrow;
            rightKey = KeyCode.RightArrow;
            startKey = KeyCode.Return;
        }
    }

    private bool AreAnyKeysPressed()
    {
        return Input.GetKey(upKey) || Input.GetKey(downKey) || Input.GetKey(leftKey) || Input.GetKey(rightKey);
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
}