#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine;
using UnityEditor;
using System.Collections;

[CanEditMultipleObjects]
[CustomEditor(typeof(NetworkScriptableTileLayer), true)]
public class NetworkScriptableTileLayerEditor : Editor {
	
	NetworkScriptableTileLayer layerBase;
	public static int selectedLayer = 0;
	SerializedProperty seed;
	SerializedProperty scriptableLayers;

	int newWidthInChunks;
	int newHeightInChunks;
	int chunkXOffset;
	int chunkYOffset;
	int newChunkWidth;
	int newChunkHeight;

	void ResetSizeAndOffset () {
		newWidthInChunks = layerBase.widthInChunks;
		newHeightInChunks = layerBase.heightInChunks;
		chunkXOffset = 0;
		chunkYOffset = 0;
	}
	void ResetChunkSize () {
		newChunkWidth = layerBase.chunkWidth;
		newChunkHeight = layerBase.chunkHeight;
	}
	bool isSizeInChunksChanged {
		get {
			return ( newWidthInChunks != layerBase.widthInChunks || newHeightInChunks != layerBase.heightInChunks || chunkXOffset != 0 || chunkYOffset != 0 );
		}
	}
	bool isChunkSizeChanged {
		get {
			return ( newChunkWidth != layerBase.chunkWidth || newChunkHeight != layerBase.chunkHeight );
		}
	}
	void OnEnable () {
		
		seed = serializedObject.FindProperty( "seed" );
		scriptableLayers = serializedObject.FindProperty( "scriptableLayers" );
		layerBase = (NetworkScriptableTileLayer)target;

		if( layerBase.scriptableLayers == null || layerBase.scriptableLayers.Length == 0 ) {
			layerBase.scriptableLayers = new ScriptableTileLayer[1];
			layerBase.scriptableLayers[0] = new ScriptableTileLayer();
			layerBase.scriptableLayers[0].name = "Layer 0";
			TileInfo tileInfo = layerBase.GetComponent<TileInfo>();
			layerBase.scriptableLayers[0].layers = new TileInfo[1] { tileInfo };
			layerBase.scriptableLayers[0].update3DWalls = tileInfo.update3DWalls;
			layerBase.scriptableLayers[0].update2DColliders = tileInfo.update2DColliders;
			layerBase.chunkWidth = tileInfo.mapWidth;
			layerBase.chunkHeight = tileInfo.mapHeight;
		}
		ResetSizeAndOffset();
		ResetChunkSize();
		if( selectedLayer >= layerBase.scriptableLayers.Length )
			selectedLayer = 0;
		TilesetEditor.ChangeTileLayer( layerBase.GetComponent<TileInfo>() );
	}

	public static void SceneGUI ( NetworkScriptableTileLayer layerBase ) {
		if( layerBase.scriptableLayers == null || layerBase.scriptableLayers.Length == 0 )
			return;
		if( selectedLayer >= layerBase.scriptableLayers.Length )
			selectedLayer = 0;
		Handles.BeginGUI();
		GUILayout.BeginHorizontal( "box", GUILayout.Width( 100 ) );
		if( GUILayout.Button( "<" ) ) {
			selectedLayer--;
			if( selectedLayer < 0 )
				selectedLayer = layerBase.scriptableLayers.Length-1;
			if( layerBase.scriptableLayers[ selectedLayer ].layers.Length > 0 )
				TilesetEditor.ChangeTileLayer( layerBase.scriptableLayers[ selectedLayer ].layers[0] );
		}
		GUILayout.Label( layerBase.scriptableLayers[selectedLayer].name );
		if( GUILayout.Button( ">" ) ) {
			selectedLayer++;
			if( selectedLayer >= layerBase.scriptableLayers.Length )
				selectedLayer = 0;
			if( layerBase.scriptableLayers[ selectedLayer ].layers.Length > 0 )
				TilesetEditor.ChangeTileLayer( layerBase.scriptableLayers[ selectedLayer ].layers[0] );
		}
		GUILayout.EndHorizontal();
		Handles.EndGUI();
	}
	void OnSceneGUI () {
		if( layerBase == null )
			return;
		if( layerBase.scriptableLayers == null || layerBase.scriptableLayers.Length == 0 )
			return;
		Event current = Event.current;
		Vector3 pos = layerBase.transform.position;
		Vector3 up = new Vector3( 0, layerBase.heightInChunks * layerBase.chunkHeight * layerBase.mainLayer.zoomFactor );
		Vector3 right = new Vector3( layerBase.widthInChunks * layerBase.chunkWidth * layerBase.mainLayer.zoomFactor, 0 );
		Vector3[] verts = new Vector3[] { pos, pos + up, pos + up + right, pos + right };
		Handles.DrawSolidRectangleWithOutline( verts, Color.clear, Color.green );

		if( isSizeInChunksChanged ) {
			pos = layerBase.transform.position + new Vector3( chunkXOffset * layerBase.chunkWidth, chunkYOffset * layerBase.chunkHeight ) * layerBase.mainLayer.zoomFactor;
			up = new Vector3( 0, newHeightInChunks * layerBase.chunkHeight * layerBase.mainLayer.zoomFactor );
			right = new Vector3( newWidthInChunks * layerBase.chunkWidth * layerBase.mainLayer.zoomFactor, 0 );
			verts = new Vector3[] { pos, pos + up, pos + up + right, pos + right };
			Handles.DrawSolidRectangleWithOutline( verts, Color.clear, Color.red );
		}
		if( isChunkSizeChanged ) {
			pos = layerBase.transform.position;
			up = new Vector3( 0, layerBase.heightInChunks * newChunkHeight * layerBase.mainLayer.zoomFactor );
			right = new Vector3( layerBase.widthInChunks * newChunkWidth * layerBase.mainLayer.zoomFactor, 0 );
			verts = new Vector3[] { pos, pos + up, pos + up + right, pos + right };
			Handles.DrawSolidRectangleWithOutline( verts, Color.clear, Color.red );
		}
		if( TilesetEditor.toolSelected != TilesetEditor.TileTool.None && TilesetEditor.toolSelected != TilesetEditor.TileTool.Collisions ) {
			Vector2 mousePos = SceneView.currentDrawingSceneView.camera.ScreenToWorldPoint( new Vector3( current.mousePosition.x, SceneView.currentDrawingSceneView.camera.pixelHeight - current.mousePosition.y ) );
			int xIndex = Mathf.FloorToInt( ( mousePos.x - layerBase.transform.position.x ) / ( layerBase.mainLayer.zoomFactor * layerBase.chunkWidth ) );
			int yIndex = Mathf.FloorToInt( ( mousePos.y - layerBase.transform.position.y ) / ( layerBase.mainLayer.zoomFactor * layerBase.chunkHeight ) );
			if( xIndex >= 0 && xIndex < layerBase.widthInChunks && yIndex >= 0 && yIndex < layerBase.heightInChunks ) {
				pos = layerBase.transform.position + new Vector3( xIndex * layerBase.chunkWidth, yIndex * layerBase.chunkHeight ) * layerBase.mainLayer.zoomFactor;
				up = new Vector3( 0, layerBase.chunkHeight * layerBase.mainLayer.zoomFactor );
				right = new Vector3( layerBase.chunkWidth * layerBase.mainLayer.zoomFactor, 0 );
				verts = new Vector3[] { pos, pos + up, pos + up + right, pos + right };
				Handles.DrawSolidRectangleWithOutline( verts, Color.clear, Color.blue);
			}
		}


	}

	public override void OnInspectorGUI () {
		serializedObject.GetIterator().isExpanded = EditorGUILayout.Foldout( serializedObject.GetIterator().isExpanded, "Base Inspector" );
		if( serializedObject.GetIterator().isExpanded ) {
			EditorGUI.indentLevel++;

			GUILayout.BeginVertical("box");
			EditorGUI.BeginChangeCheck();
			newWidthInChunks = EditorGUILayout.IntField( "Width in Chunks", newWidthInChunks );
			newHeightInChunks = EditorGUILayout.IntField( "Height in Chunks", newHeightInChunks );
			chunkXOffset = EditorGUILayout.IntField( "Chunk X Offset", chunkXOffset );
			chunkYOffset = EditorGUILayout.IntField( "Chunk Y Offsert", chunkYOffset );
			if( EditorGUI.EndChangeCheck() ) {
				if( isSizeInChunksChanged && isChunkSizeChanged )
					ResetChunkSize();
				SceneView.RepaintAll();
			}
			if( isSizeInChunksChanged ) {
				GUILayout.BeginHorizontal();
				if( GUILayout.Button( "Apply" ) ) {
					layerBase.Resize( chunkXOffset, chunkYOffset, newWidthInChunks, newHeightInChunks );
					ResetSizeAndOffset();
					SceneView.RepaintAll();
				}
				if( GUILayout.Button( "Cancel" ) ) {
					ResetSizeAndOffset();
					SceneView.RepaintAll();
				}
				GUILayout.EndHorizontal();
			}
			GUILayout.EndVertical();

			GUILayout.Space( 16 );
			GUILayout.BeginVertical("box");
			EditorGUI.BeginChangeCheck();
			newChunkWidth = EditorGUILayout.IntField( "Chunk Width", newChunkWidth );
			newChunkHeight = EditorGUILayout.IntField( "Chunk Height", newChunkHeight );
			if( EditorGUI.EndChangeCheck() ) {
				if( isSizeInChunksChanged && isChunkSizeChanged )
					ResetSizeAndOffset();
				SceneView.RepaintAll();
			}
			if( isChunkSizeChanged ) {
				EditorGUILayout.HelpBox( "Resizing the chunk size requires all the layers to update and might take some time", MessageType.Info );
				GUILayout.BeginHorizontal();
				if( GUILayout.Button( "Apply" ) ) {
					layerBase.Resize( newChunkWidth, newChunkHeight );
					SceneView.RepaintAll();
				}
				if( GUILayout.Button( "Cancel" ) ) {
					ResetChunkSize();
					SceneView.RepaintAll();
				}
				GUILayout.EndHorizontal();
			}
			GUILayout.EndVertical();

			serializedObject.Update();
			GUILayout.Space( 16 );
			GUILayout.BeginHorizontal();
			float labelWidth = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = 64;
			EditorGUILayout.PropertyField( seed );
			if( GUILayout.Button( "Randomize" ) ) {
				seed.intValue = Random.Range( int.MinValue, int.MaxValue );
			}
			GUILayout.EndHorizontal();

			//layers
			GUILayout.BeginVertical("box");
			GUILayout.BeginHorizontal();
			EditorGUILayout.PropertyField( scriptableLayers.GetArrayElementAtIndex( selectedLayer ).FindPropertyRelative( "name" ) );
			GUILayout.Label( "(Index " + selectedLayer + ")", GUILayout.Width( 64 ) );
			GUILayout.EndHorizontal();
			EditorGUIUtility.labelWidth = 140;
			EditorGUILayout.PropertyField( scriptableLayers.GetArrayElementAtIndex( selectedLayer ).FindPropertyRelative( "worldZ" ) );
			EditorGUILayout.PropertyField( scriptableLayers.GetArrayElementAtIndex( selectedLayer ).FindPropertyRelative( "parallaxDistance" ) );
			EditorGUILayout.PropertyField( scriptableLayers.GetArrayElementAtIndex( selectedLayer ).FindPropertyRelative( "update3DWalls" ) );
			EditorGUILayout.PropertyField( scriptableLayers.GetArrayElementAtIndex( selectedLayer ).FindPropertyRelative( "update2DColliders" ) );
			EditorGUILayout.PropertyField( scriptableLayers.GetArrayElementAtIndex( selectedLayer ).FindPropertyRelative( "sortingLayer" ) );
			EditorGUILayout.PropertyField( scriptableLayers.GetArrayElementAtIndex( selectedLayer ).FindPropertyRelative( "sortingOrder" ) );
			EditorGUIUtility.labelWidth = labelWidth;
			if( GUILayout.Button( "Update Colliders" ) ) {
				foreach( TileInfo l in layerBase.scriptableLayers[ selectedLayer ].layers ) {
					if( l == null )
						continue;
					bool u = l.update2DColliders;
					l.update2DColliders = true;
					l.mapHasChanged = true;
					l.UpdateVisualMesh( false );
					l.update2DColliders = u;
				}
			}
			GUILayout.BeginHorizontal();
			if( GUILayout.Button( "Previous" ) ) {
				selectedLayer--;
				if( selectedLayer < 0 )
					selectedLayer = layerBase.scriptableLayers.Length-1;
			}
			if( GUILayout.Button( "Add" ) ) {
				scriptableLayers.InsertArrayElementAtIndex( selectedLayer+1 );
				serializedObject.ApplyModifiedProperties();
				selectedLayer++;
				layerBase.scriptableLayers[ selectedLayer ].layers = new TileInfo[ layerBase.widthInChunks * layerBase.heightInChunks ];
			}
			if( selectedLayer > 0 && GUILayout.Button( "Remove" ) ) {
				scriptableLayers.DeleteArrayElementAtIndex( selectedLayer );
				serializedObject.ApplyModifiedProperties();
				selectedLayer--;
			}
			if( GUILayout.Button( "Next" ) ) {
				selectedLayer++;
				if( selectedLayer >= layerBase.scriptableLayers.Length )
					selectedLayer = 0;
			}
			GUILayout.EndHorizontal();
			GUILayout.EndVertical();

			serializedObject.ApplyModifiedProperties();
			if( GUILayout.Button( "Snap To Grid" ) ) {
				Vector3 newPos = layerBase.transform.position;
				newPos.x = Mathf.Round( newPos.x );
				newPos.y = Mathf.Round( newPos.y );
				layerBase.transform.position = newPos;
			}

			if( GUILayout.Button( "Clear and Generate" ) ) {
				layerBase.Generate();
				layerBase.UpdateMeshes();
			}
			if( GUILayout.Button( "Clear" ) ) {
				layerBase.DestroyLayers();
				layerBase.UpdateMeshes();
			}
			if( GUILayout.Button( "Remove Empty Chunks" ) ) {
				layerBase.RemoveEmptyChunks();
			}
			EditorGUI.indentLevel--;
		}
		DrawDefaultInspector();
	}
}

#endif