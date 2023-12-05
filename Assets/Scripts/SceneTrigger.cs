using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTrigger : MonoBehaviour {
	// Name of the scene you want to load
    public string sceneToLoad;


	private void OnTriggerEnter2D(Collider2D other) {
		LoadScene();
	}

    // This method can be called to load the scene
    public void LoadScene()
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
