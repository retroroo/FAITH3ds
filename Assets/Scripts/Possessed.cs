using System.Collections;
using UnityEngine;
using UnityEngine.Events; // Import the UnityEvents namespace
using UnityEngine.SceneManagement; // Import the SceneManager for loading scenes

public class Possessed : MonoBehaviour {
    public bool isDemon;
    private Animator animator;

    // Define an enum to choose the action type
    public enum ActionType {
        None,
        ActivateGameObject,
        LoadScene
    }

    // Variable to store the selected action type
    public ActionType actionType;

    // Use UnityEvent to assign actions in the Unity Editor
    public UnityEvent onCollisionEvent;

    // Optionally, define fields for GameObject activation or scene loading
    public GameObject objectToActivate; // Set this in the Unity Editor
    public string sceneToLoad; // Name of the scene to load
    public float delayBeforeAction = 2f;

    void Start() {
        // Get the Animator component attached to this GameObject
        animator = GetComponent<Animator>();
    }

    public void Collided() {
        if (isDemon) {
            animator.SetTrigger("dead");
            StartCoroutine(DestructionSequence());
        }
    }

    private IEnumerator DestructionSequence() {
        // Wait for the specified delay
        yield return new WaitForSeconds(delayBeforeAction);

        // Trigger the assigned UnityEvent
        onCollisionEvent.Invoke();

        // Optionally, handle other actions based on enum
        switch (actionType) {
            case ActionType.ActivateGameObject:
                if (objectToActivate != null) {
                    objectToActivate.SetActive(true);
                } else {
                    Debug.LogError("No object set to be activated.");
                }
                break;
            case ActionType.LoadScene:
                // Load the specified scene (make sure to handle scene loading correctly)
                SceneManager.LoadScene(sceneToLoad);
                break;
        }

        // Destroy the current GameObject
        Destroy(gameObject);
    }
}