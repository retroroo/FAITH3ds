using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MenuSelect : MonoBehaviour
{
    public bool use3DSControls;
    public GameObject[] objectsToShow; // Array of objects to show for each number
    public GameObject[] objectsToHide; // Array of objects to hide for each number
    public int maxNumber = 10;
    public int currentNumber = 0;

	public GameObject warning;

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
        if (use3DSControls){
				if (UnityEngine.N3DS.GamePad.GetButtonHold(N3dsButton.A))
				{
					Debug.Log("Number " + currentNumber + " is selected");
					menuLogic();
				}
		}else{
			// Check for Enter press
			if (Input.GetKeyDown(KeyCode.Return))
			{
				Debug.Log("Number " + currentNumber + " is selected");
				menuLogic();
				// Custom code can be added here
			}
		}      
    }
	private void menuLogic(){
		if(currentNumber == 4){
				QuitGame();
			}else if(currentNumber == 1)
			{
				LoadScene("Tutorial");
			}
			else{
				StartCoroutine(ActivateAndDeactivate(warning, 3));
			}
	}
	 private IEnumerator ActivateAndDeactivate(GameObject obj, float seconds)
    {
        if (obj != null)
        {
            obj.SetActive(true);
            yield return new WaitForSeconds(seconds);
            obj.SetActive(false);
        }
    }
	public void QuitGame()
    {
        #if UNITY_EDITOR
        // This code will only execute in the Unity Editor
        EditorApplication.isPlaying = false;
        #else
        // This code will only execute in the build version
        Application.Quit();
        #endif
    }
	public void LoadScene(string sceneToLoad)
    {
        // Make sure the scene name is not empty
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.LogError("Scene name is empty!");
        }
    }
}