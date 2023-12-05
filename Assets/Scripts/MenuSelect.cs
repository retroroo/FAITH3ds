using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuSelect : MonoBehaviour
{
    public bool use3DSControls;
    public GameObject[] objectsToShow; // Array of objects to show for each number
    public GameObject[] objectsToHide; // Array of objects to hide for each number
    public int maxNumber = 10;
    public int currentNumber = 0;

    void Update()
    {
        // Check for input and update currentNumber
        if (use3DSControls)
        {
            if (UnityEngine.N3DS.GamePad.GetButtonHold(N3dsButton.Up)) currentNumber--;
            if (UnityEngine.N3DS.GamePad.GetButtonHold(N3dsButton.Down)) currentNumber++;
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.W)) currentNumber--;
            if (Input.GetKeyDown(KeyCode.S)) currentNumber++;
        }

        // Cap the number
        currentNumber = Mathf.Clamp(currentNumber, 0, maxNumber - 1);

        // Activate/Deactivate objects based on the current number
        for (int i = 0; i < maxNumber; i++)
        {
            if (i < objectsToShow.Length && objectsToShow[i] != null) // Check if the GameObject exists
            {
                objectsToShow[i].SetActive(i == currentNumber);
            }

            if (i < objectsToHide.Length && objectsToHide[i] != null) // Check if the GameObject exists
            {
                objectsToHide[i].SetActive(i != currentNumber);
            }
        }

        // Check for Enter press
        if (Input.GetKeyDown(KeyCode.Return))
        {
            Debug.Log("Number " + currentNumber + " is selected");

            // Custom code can be added here
        }
    }
}