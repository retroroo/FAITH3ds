using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Apple : MonoBehaviour {
    // Assign this in the inspector or via another script
    public GameObject objectToActivate;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the collided object is the player and the collider is not a trigger
        if (other.CompareTag("Player") && !other.isTrigger)
        {
            // Check if the object to activate is not null
            if (objectToActivate != null)
            {
                objectToActivate.SetActive(true);
            }

            // Destroy the current game object
            Destroy(gameObject);
        }
    }
}