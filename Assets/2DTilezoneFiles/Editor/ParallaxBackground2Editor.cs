using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(ParallaxBackground2))]
public class ParallaxBackground2Editor : Editor {


	public class BackgroundPreview {
		public Vector3 startPosition;
		public ParallaxBackground2 obj;
	}
	BackgroundPreview[] previewItems;
	Camera _cam;
	Camera cam {
		get {
			if( _cam == null )
				_cam = SceneView.lastActiveSceneView.camera;
			return _cam;
		}
	}
	void EditorUpdate () {
		
		if( previewItems == null )
			return;
		if( EditorApplication.isPlayingOrWillChangePlaymode ) {
			CancelAllPreviews();
			return;
		}
		foreach( BackgroundPreview item in previewItems ) {
			if( item.obj== null )
				continue;
			item.obj.transform.position = item.startPosition + ( 1 - Mathf.Pow( 0.9f, item.obj.distance ) ) * (Vector3)( (Vector2)cam.transform.position - (Vector2)item.startPosition + item.obj._offset );
		}
	}

	void CancelAllPreviews () {
		if( previewItems == null )
			return;
		foreach( BackgroundPreview item in previewItems ) {
			item.obj.transform.position = item.startPosition;
			item.obj.editorShowMesh = false;
		}
		previewItems = null;
	}
	ParallaxBackground2 parallaxBackground;
	SerializedProperty tileX;
	SerializedProperty tileY;
	SerializedProperty tileXAmount;
	SerializedProperty tileYAmount;
	void OnEnable () {
		parallaxBackground = target as ParallaxBackground2;
		tileX = serializedObject.FindProperty( "tileX" );
		tileY = serializedObject.FindProperty( "tileY" );
		tileXAmount = serializedObject.FindProperty( "tileXAmount" );
		tileYAmount = serializedObject.FindProperty( "tileYAmount" );
		EditorApplication.update += EditorUpdate;
	}

	void OnDisable () {
		EditorApplication.update -= EditorUpdate;
		CancelAllPreviews();
	}

	public override void OnInspectorGUI () {
		DrawDefaultInspector();

		if( parallaxBackground.GetComponent<SpriteRenderer>() != null ) {
			serializedObject.Update();

			EditorGUILayout.PropertyField( tileX );
			EditorGUILayout.PropertyField( tileY );
			EditorGUILayout.PropertyField( tileXAmount );
			EditorGUILayout.PropertyField( tileYAmount );

			serializedObject.ApplyModifiedProperties();
		}

		if( GUILayout.Button( "Preview" ) ) {
			CancelAllPreviews();
			previewItems = new BackgroundPreview[] { new BackgroundPreview() };
			previewItems[0].obj = (ParallaxBackground2)target;
			previewItems[0].startPosition = previewItems[0].obj.transform.position;
			previewItems[0].obj.editorShowMesh = true;
		}
		if( GUILayout.Button( "Preview All" ) ) {
			CancelAllPreviews();
			ParallaxBackground2[] allBackgrounds = FindObjectsOfType<ParallaxBackground2>();
			previewItems = new BackgroundPreview[ allBackgrounds.Length ];
			previewItems[0] = new BackgroundPreview();
			previewItems[0].obj = (ParallaxBackground2)target;
			previewItems[0].startPosition = previewItems[0].obj.transform.position;
			previewItems[0].obj.editorShowMesh = true;
			int ii = 1;
			for( int i = 0; i < allBackgrounds.Length; i++ ) {
				if( allBackgrounds[i] != previewItems[0].obj ) {
					previewItems[ii] = new BackgroundPreview();
					previewItems[ii].obj = allBackgrounds[i];
					previewItems[ii].startPosition = allBackgrounds[i].transform.position;
					previewItems[ii].obj.editorShowMesh = true;
					ii++;
				}
			}
		}
		if( previewItems != null && GUILayout.Button( "Cancel Preview" ) ) {
			CancelAllPreviews();
		}
	}

	void OnSceneGUI () {
		if( previewItems != null ) {
			Tools.hidden = true;
			previewItems[0].startPosition = Handles.PositionHandle( previewItems[0].startPosition, Quaternion.identity );
			return;
		}
		Tools.hidden = false;
		Handles.color = Color.red;
		ParallaxBackground2 pb = (ParallaxBackground2)target;
		Handles.DrawLine( pb.transform.position, pb.transform.position +  ( 1 - Mathf.Pow( 0.9f, pb.distance ) ) * (Vector3)( (Vector2)cam.transform.position - (Vector2)pb.transform.position + pb._offset ) );
	}
}
