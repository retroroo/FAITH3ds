using UnityEngine;
using System.Collections;

[AddComponentMenu("2D/Parallax Background Old")]
public class ParallaxBackground : MonoBehaviour {
	
	public Camera cam;
	public bool pixelSnap;
	public float distance;
	public Vector3 startPosition;
	// Use this for initialization
	void Start () {
		//distance = Mathf.Max( transform.position.z / 10, 0 );

		if( cam == null )
			cam = Camera.main;

		//startPos = transform.position;
	}
	
	// Update is called once per frame
	void Update () {
		//if( !Application.isPlaying )
			//transform.position = startPos + (Vector3)(Vector2)(UnityEditor.SceneView.lastActiveSceneView.camera.transform.position - startPos) * ( 1 - 1 / (distance+1));
		//else {
			transform.position = startPosition + (Vector3)(Vector2)(cam.transform.position - startPosition) * ( 1 - 1 / ((distance/10f)+1));

			if( pixelSnap ) {
				PixelPerfectCamera.SnapToPix( transform );
			}
		//}
		//UnityEditor.EditorUtility.SetDirty( gameObject );
	}
}
