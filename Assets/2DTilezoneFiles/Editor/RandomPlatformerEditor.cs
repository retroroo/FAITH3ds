using UnityEngine;
using UnityEditor;
using System.Collections;

[CanEditMultipleObjects]
[CustomEditor(typeof(RandomPlatformer))]
public class RandomPlatformerEditor : Editor {
	
	SerializedProperty width;
	SerializedProperty height;
	SerializedProperty chunkWidth;
	SerializedProperty chunkHeight;
	SerializedProperty otherPlatforms;
	SerializedProperty pathHeight;
	SerializedProperty pathThickness;
	SerializedProperty solutionDistance;

	SerializedProperty entryObjOffset;
	SerializedProperty exitObjOffset;
	
	SerializedProperty scale;
	SerializedProperty floorChance;
	SerializedProperty backgroundChance;
	SerializedProperty randomAtGameStart;
	SerializedProperty addBorder;
	SerializedProperty updateColliders;
	
	SerializedProperty floorObjects;
	SerializedProperty ladderLayer;
	SerializedProperty backgroundZOffset;
	SerializedProperty parallaxDistance;

	
	Texture tileTex;
	
	RandomPlatformer randomPlatform;
	
	void OnEnable () {
		width = serializedObject.FindProperty( "width" );
		height = serializedObject.FindProperty( "height" );
		chunkWidth = serializedObject.FindProperty( "chunkWidth" );
		chunkHeight = serializedObject.FindProperty( "chunkHeight" );
		otherPlatforms = serializedObject.FindProperty( "otherPlatforms" );
		pathHeight = serializedObject.FindProperty( "pathHeight" );
		pathThickness = serializedObject.FindProperty( "pathThickness" );
		solutionDistance = serializedObject.FindProperty( "solutionDistance" );

		entryObjOffset = serializedObject.FindProperty( "entryObjOffset" );
		exitObjOffset = serializedObject.FindProperty( "exitObjOffset" );
		
		scale = serializedObject.FindProperty( "scale" );
		floorChance = serializedObject.FindProperty( "floorChance" );
		backgroundChance = serializedObject.FindProperty( "backgroundChance" );
		randomAtGameStart = serializedObject.FindProperty( "randomAtGameStart" );
		addBorder = serializedObject.FindProperty( "addBorder" );
		updateColliders = serializedObject.FindProperty( "updateColliders" );
		
		floorObjects = serializedObject.FindProperty( "floorObjects" );
		ladderLayer = serializedObject.FindProperty( "ladderLayer" );
		backgroundZOffset = serializedObject.FindProperty( "backgroundZOffset" );
		parallaxDistance = serializedObject.FindProperty( "parallaxDistance" );

		
		randomPlatform = (RandomPlatformer)serializedObject.targetObject;
		randomPlatform.wallLayer = randomPlatform.GetComponent<TileInfo>();
		if( randomPlatform.transform.childCount > 0 )
			randomPlatform.backgroundLayer = randomPlatform.transform.GetChild( 0 ).GetComponent<TileInfo>();
		
		tileTex = randomPlatform.GetComponent<MeshRenderer>().sharedMaterial.mainTexture;
	}
	
	void OnSceneGUI () {
		Vector3 pos = randomPlatform.mainLayer.transform.position;
		Vector3 up = new Vector3( 0, height.intValue * chunkHeight.intValue );
		Vector3 right = new Vector3( width.intValue * chunkWidth.intValue, 0 );
		Vector3[] verts = new Vector3[] { pos, pos + up, pos + up + right, pos + right };
		Handles.DrawSolidRectangleWithOutline( verts, Color.clear, Color.green );
	}
	
	public override void OnInspectorGUI () {
		Event current = Event.current;
		serializedObject.Update();
		
		if( GUILayout.Button( "Snap To Grid" ) ) {
			Vector3 newPos = randomPlatform.transform.position;
			newPos.x = Mathf.Round( newPos.x );
			newPos.y = Mathf.Round( newPos.y );
			randomPlatform.transform.position = newPos;
		}
		
		EditorGUILayout.IntSlider( width, 1, 16 );
		EditorGUILayout.IntSlider( height, 1, 16 );
		EditorGUILayout.IntSlider( chunkWidth, 12, 64 );
		EditorGUILayout.IntSlider( chunkHeight, 12, 64 );
		EditorGUILayout.IntSlider( otherPlatforms, 0, 64 );
		EditorGUILayout.IntSlider( pathHeight, 1, 8 );
		EditorGUILayout.IntSlider( pathThickness, 1, 8 );
		EditorGUILayout.IntSlider( solutionDistance, 1, (width.intValue*height.intValue)/2 );
		EditorGUILayout.Space();
		EditorGUILayout.PropertyField( scale );
		EditorGUILayout.Slider( floorChance, 0f, 1f );
		EditorGUILayout.Slider( backgroundChance, 0f, 1f );
		backgroundZOffset.floatValue = EditorGUILayout.FloatField( "Background Z Offset", backgroundZOffset.floatValue );
		parallaxDistance.floatValue = EditorGUILayout.FloatField( "Parallax Distance", parallaxDistance.floatValue );
		EditorGUILayout.Space();
		randomPlatform.entryObj = (GameObject)EditorGUILayout.ObjectField( "Entrance", randomPlatform.entryObj, typeof(GameObject), true );
		EditorGUILayout.PropertyField( entryObjOffset );
		EditorGUILayout.Space();
		randomPlatform.exitObj = (GameObject)EditorGUILayout.ObjectField( "Exit", randomPlatform.exitObj, typeof(GameObject), true );
		EditorGUILayout.PropertyField( exitObjOffset );
		randomAtGameStart.boolValue = EditorGUILayout.Toggle( "Random At Start", randomAtGameStart.boolValue );
		addBorder.boolValue = EditorGUILayout.Toggle( "Add Border", addBorder.boolValue );
		ladderLayer.intValue = EditorGUILayout.LayerField( "Ladder Layer", ladderLayer.intValue );
		EditorGUILayout.Space();
		EditorGUILayout.HelpBox( "The following option will run very slow on larger layers if checked.", MessageType.Info );
		updateColliders.boolValue = EditorGUILayout.Toggle( "Update Colliders", updateColliders.boolValue );

		EditorGUILayout.PropertyField( floorObjects, true );
		EditorGUILayout.Space();
		
		if( randomPlatform.wallLayer != null ) {
			
			GUILayout.Label( "Floor Tile" );
			Rect tilePos = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(64));
			tilePos.width = 64;
			DrawTile( randomPlatform.floorTile, tilePos );
			
			if( current.type == EventType.MouseDown && tilePos.Contains( current.mousePosition ) ) {
				//floor tile clicked
				if( TilesetEditor.autoTileSelected == -1 ) {
					randomPlatform.floorTile = new Tile( TilesetEditor.selectedTiles[0,0] );
				}
				else {
					randomPlatform.floorTile = new Tile( randomPlatform.wallLayer.autoTileData[TilesetEditor.autoTileSelected * 48 + 21] );
					randomPlatform.floorTile.autoTileIndex = TilesetEditor.autoTileSelected;
				}
			}
			if( GUI.Button( new Rect( tilePos.x + 80, tilePos.y + 16, 128, 32 ), "Remove" ) ) {
				randomPlatform.floorTile = Tile.empty;
			}
			EditorGUILayout.Space();
			
			GUILayout.Label( "Ladder Tile" );
			tilePos = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(64));
			tilePos.width = 64;
			DrawTile( randomPlatform.ladderTile, tilePos );
			
			if( current.type == EventType.MouseDown && tilePos.Contains( current.mousePosition ) ) {
				//ladder tile clicked
				if( TilesetEditor.autoTileSelected == -1 ) {
					randomPlatform.ladderTile = new Tile( TilesetEditor.selectedTiles[0,0] );
				}
				else {
					randomPlatform.ladderTile = new Tile( randomPlatform.wallLayer.autoTileData[TilesetEditor.autoTileSelected * 48 + 21] );
					randomPlatform.ladderTile.autoTileIndex = TilesetEditor.autoTileSelected;
				}
			}
			if( GUI.Button( new Rect( tilePos.x + 80, tilePos.y + 16, 128, 32 ), "Remove" ) ) {
				randomPlatform.ladderTile = Tile.empty;
			}
			EditorGUILayout.Space();
			
			GUILayout.Label( "Background Tile" );
			tilePos = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(64));
			tilePos.width = 64;
			DrawTile( randomPlatform.backgroundTile, tilePos );
			
			if( current.type == EventType.MouseDown && tilePos.Contains( current.mousePosition ) ) {
				//wall tile clicked
				if( TilesetEditor.autoTileSelected == -1 ) {
					randomPlatform.backgroundTile = new Tile( TilesetEditor.selectedTiles[0,0] );
				}
				else {
					randomPlatform.backgroundTile = new Tile( randomPlatform.wallLayer.autoTileData[TilesetEditor.autoTileSelected * 48 + 21] );
					randomPlatform.backgroundTile.autoTileIndex = TilesetEditor.autoTileSelected;
				}
			}
			if( GUI.Button( new Rect( tilePos.x + 80, tilePos.y + 16, 128, 32 ), "Remove" ) ) {
				randomPlatform.backgroundTile = Tile.empty;
			}
			
			if( GUILayout.Button( "Generate Platformer" ) ) {
				randomPlatform.mainLayer.GenerateRandomPlatformer(true);
				randomPlatform.UpdateMeshes( true );
			}
		}
		
        if( serializedObject.targetObject != null )
		    serializedObject.ApplyModifiedProperties();
	}
	
	void DrawTile( Tile tile, Rect position ) {
		if( randomPlatform.wallLayer == null )
			return;
		
		if( tile == null ) {
			GUI.DrawTexture( position, EditorGUIUtility.whiteTexture );
			return;
		}
		
		int tileSize = randomPlatform.wallLayer.tileSize;
		int spacing = randomPlatform.wallLayer.spacing;
		Rect texCoords = new Rect( (float)tile.xIndex * (tileSize + spacing) / (float)tileTex.width,
		                          1 - (((float)tile.yIndex + 1)  * (tileSize + spacing) - spacing) / tileTex.height,
		                          (tileSize + spacing) / (float)tileTex.width,
		                          (tileSize + spacing) / (float)tileTex.height );
		Vector2 pivot = new Vector2( position.x + position.width / 2, position.y + position.height / 2 );
		if( tile.flip ) {
			texCoords.x += texCoords.width;
			texCoords.width *= -1;
		}
		EditorGUIUtility.RotateAroundPivot(tile.rotation * 90 , pivot );
		GUI.DrawTextureWithTexCoords( position, tileTex, texCoords );
		GUI.matrix = Matrix4x4.identity;
	}
}

[CustomPropertyDrawer(typeof(RandomPlatformer.FloorObject))]
public class FloorObjectDrawer : PropertyDrawer {

	int GetMapWidth( TileInfo tileInfo, int currentValue ) {
		for( int x = tileInfo.mapWidth-1; x >= 0; x-- ) {
			for( int y = 0; y < tileInfo.mapHeight; y++ ) {
				if( tileInfo.tiles[y * tileInfo.mapWidth + x] != Tile.empty )
					return x+1;
			}
		}
		return currentValue;
	}

	public override void OnGUI (Rect position, SerializedProperty property, GUIContent label) {
		position.height = 18;
		EditorGUI.BeginProperty( position, label, property );
		string name = property.FindPropertyRelative("obj").objectReferenceValue == null ? "none" : property.FindPropertyRelative("obj").objectReferenceValue.name;
		property.isExpanded = EditorGUI.Foldout( position, property.isExpanded, name );
		if( property.isExpanded ) {
			
//			position = EditorGUI.PrefixLabel( position, GUIUtility.GetControlID( label, FocusType.Passive ), GUIContent.none );
			//int indent = EditorGUI.indentLevel;
			position.height = 18;
			position.y += 18;
			EditorGUI.PropertyField( position, property.FindPropertyRelative( "chance" ) );
			position.y += 18;
			EditorGUI.PropertyField( position, property.FindPropertyRelative( "minAmount" ) );
			position.y += 18;
			EditorGUI.PropertyField( position, property.FindPropertyRelative( "maxAmount" ) );
			position.y += 18;
			EditorGUI.PropertyField( position, property.FindPropertyRelative( "offset" ) );
			position.y += 36;
			EditorGUI.PropertyField( position, property.FindPropertyRelative( "width" ) );
			position.y += 18;
			EditorGUI.PropertyField( position, property.FindPropertyRelative( "mustBeReachable" ) );
			position.y += 18;
			EditorGUI.PropertyField( position, property.FindPropertyRelative( "minDistanceFromStart" ) );
			position.y += 18;
			EditorGUI.BeginChangeCheck();
			EditorGUI.PropertyField( position, property.FindPropertyRelative( "obj" ) );
			if( EditorGUI.EndChangeCheck() ) {
				GameObject obj = (GameObject)property.FindPropertyRelative( "obj" ).objectReferenceValue;
				if( obj != null ) {
					TileInfo tileInfo = obj.GetComponent<TileInfo>();
					if( tileInfo != null ) {
						SerializedProperty width = property.FindPropertyRelative( "width" );
						width.intValue = GetMapWidth( tileInfo, width.intValue );
					}
				}
			}

		}
		EditorGUI.EndProperty();
	}
	public override float GetPropertyHeight (SerializedProperty property, GUIContent label) {
		if( !property.isExpanded )
			return 18;
		else return 180;
	}
}
