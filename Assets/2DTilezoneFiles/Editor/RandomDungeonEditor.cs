using UnityEngine;
using UnityEditor;
using System.Collections;

[CanEditMultipleObjects]
[CustomEditor(typeof(RandomDungeon))]
public class RandomDungeonEditor : Editor {

	SerializedProperty width;
	SerializedProperty height;
	SerializedProperty wallHeight;

	SerializedProperty scale;
	SerializedProperty floorChance;
	SerializedProperty randomAtGameStart;
	SerializedProperty updateFloorColliders;
	SerializedProperty updateWallColliders;

	SerializedProperty floorObjects;

	Texture tileTex;

	RandomDungeon randomDungeon;

	void OnEnable () {
		width = serializedObject.FindProperty( "width" );
		height = serializedObject.FindProperty( "height" );
		wallHeight = serializedObject.FindProperty( "wallHeight" );

		scale = serializedObject.FindProperty( "scale" );
		floorChance = serializedObject.FindProperty( "floorChance" );
		randomAtGameStart = serializedObject.FindProperty( "randomAtGameStart" );
		updateFloorColliders = serializedObject.FindProperty( "updateFloorColliders" );
		updateWallColliders = serializedObject.FindProperty( "updateWallColliders" );

		floorObjects = serializedObject.FindProperty( "floorObjects" );

		randomDungeon = (RandomDungeon)serializedObject.targetObject;
		randomDungeon.wallLayer = randomDungeon.GetComponent<TileInfo>();
		if( randomDungeon.transform.childCount > 0 )
			randomDungeon._floorLayer = randomDungeon.transform.GetChild( 0 ).GetComponent<TileInfo>();

		tileTex = randomDungeon.GetComponent<MeshRenderer>().sharedMaterial.mainTexture;
	}

	void OnSceneGUI () {
		Vector3 pos = randomDungeon.transform.position;
		Vector3 up = new Vector3( 0, height.intValue );
		Vector3 right = new Vector3( width.intValue, 0 );
		Vector3[] verts = new Vector3[] { pos, pos + up, pos + up + right, pos + right };
		Handles.DrawSolidRectangleWithOutline( verts, Color.clear, Color.green );
	}

	public override void OnInspectorGUI () {
		Event current = Event.current;
		serializedObject.Update();

		if( GUILayout.Button( "Snap To Grid" ) ) {
			Vector3 newPos = randomDungeon.transform.position;
			newPos.x = Mathf.Round( newPos.x );
			newPos.y = Mathf.Round( newPos.y );
			randomDungeon.transform.position = newPos;
		}

		EditorGUILayout.IntSlider( width, 8, 64 );
		EditorGUILayout.IntSlider( height, 8, 64 );
		EditorGUILayout.IntSlider( wallHeight, 1, 4 );
		EditorGUILayout.Space();
		EditorGUILayout.Slider( scale, 0.01f, 1f );
		EditorGUILayout.Slider( floorChance, 0.01f, 1f );
		EditorGUILayout.Space();
		randomDungeon.stairsUp = (GameObject)EditorGUILayout.ObjectField( "Stairs Up", randomDungeon.stairsUp, typeof(GameObject), true );
		randomDungeon.stairsDown = (GameObject)EditorGUILayout.ObjectField( "Stairs Down", randomDungeon.stairsDown, typeof(GameObject), true );
		randomAtGameStart.boolValue = EditorGUILayout.Toggle( "Random At Start", randomAtGameStart.boolValue );
		EditorGUILayout.Space();
		EditorGUILayout.HelpBox( "The following two options will run very slow on larger layers if checked.", MessageType.Info );
		updateFloorColliders.boolValue = EditorGUILayout.Toggle( "Update Floor Colliders", updateFloorColliders.boolValue );
		updateWallColliders.boolValue = EditorGUILayout.Toggle( "Update Wall Colliders", updateWallColliders.boolValue );
//		floorObjFoldout = EditorGUILayout.Foldout( floorObjFoldout, "Random Floor Objects" );
//		if( floorObjFoldout ) {
//			for( int i = 0; i < floorObjects.arraySize; i++ ) {
//				SerializedProperty chance = floorObjects.GetArrayElementAtIndex( i ).FindPropertyRelative( "chance" );
//				SerializedProperty minAmount = floorObjects.GetArrayElementAtIndex( i ).FindPropertyRelative( "minAmount" );
//				SerializedProperty maxAmount = floorObjects.GetArrayElementAtIndex( i ).FindPropertyRelative( "maxAmount" );
//
//				EditorGUILayout.Slider( chance, 0.01f, 1 );
//				EditorGUILayout.IntSlider( minAmount, 0, 100 );
//				EditorGUILayout.IntSlider( maxAmount, minAmount.intValue, 100 );
//			}
//		}
		EditorGUILayout.PropertyField( floorObjects, true );
		EditorGUILayout.Space();

		if( randomDungeon.wallLayer != null ) {

			GUILayout.Label( "Floor Tile" );
			Rect tilePos = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(64));
			tilePos.width = 64;
			DrawTile( randomDungeon.floorTile, tilePos );

			if( current.type == EventType.MouseDown && tilePos.Contains( current.mousePosition ) ) {
				//floor tile clicked
				if( TilesetEditor.autoTileSelected == -1 ) {
					randomDungeon.floorTile = new Tile( TilesetEditor.selectedTiles[0,0] );
				}
				else {
					randomDungeon.floorTile = new Tile( randomDungeon.wallLayer.autoTileData[TilesetEditor.autoTileSelected * 48 + 21] );
					randomDungeon.floorTile.autoTileIndex = TilesetEditor.autoTileSelected;
				}
			}
			if( GUI.Button( new Rect( tilePos.x + 80, tilePos.y + 16, 128, 32 ), "Remove" ) ) {
				randomDungeon.floorTile = Tile.empty;
			}
			EditorGUILayout.Space();

			GUILayout.Label( "Roof Tile" );
			tilePos = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(64));
			tilePos.width = 64;
			DrawTile( randomDungeon.roofTile, tilePos );
			
			if( current.type == EventType.MouseDown && tilePos.Contains( current.mousePosition ) ) {
				//roof tile clicked
				if( TilesetEditor.autoTileSelected == -1 ) {
					randomDungeon.roofTile = new Tile( TilesetEditor.selectedTiles[0,0] );
				}
				else {
					randomDungeon.roofTile = new Tile( randomDungeon.wallLayer.autoTileData[TilesetEditor.autoTileSelected * 48 + 21] );
					randomDungeon.roofTile.autoTileIndex = TilesetEditor.autoTileSelected;
				}
			}
			if( GUI.Button( new Rect( tilePos.x + 80, tilePos.y + 16, 128, 32 ), "Remove" ) ) {
				randomDungeon.roofTile = Tile.empty;
			}
			EditorGUILayout.Space();

			GUILayout.Label( "Wall Tile" );
			tilePos = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(64));
			tilePos.width = 64;
			DrawTile( randomDungeon.wallTile, tilePos );
			
			if( current.type == EventType.MouseDown && tilePos.Contains( current.mousePosition ) ) {
				//wall tile clicked
				if( TilesetEditor.autoTileSelected == -1 ) {
					randomDungeon.wallTile = new Tile( TilesetEditor.selectedTiles[0,0] );
				}
				else {
					randomDungeon.wallTile = new Tile( randomDungeon.wallLayer.autoTileData[TilesetEditor.autoTileSelected * 48 + 21] );
					randomDungeon.wallTile.autoTileIndex = TilesetEditor.autoTileSelected;
				}
			}
			if( GUI.Button( new Rect( tilePos.x + 80, tilePos.y + 16, 128, 32 ), "Remove" ) ) {
				randomDungeon.wallTile = Tile.empty;
			}

			if( GUILayout.Button( "Generate Dungeon" ) ) {
				randomDungeon.GenerateRandomDungeon(true);
			}
		}

		serializedObject.ApplyModifiedProperties();
	}

	void DrawTile( Tile tile, Rect position ) {
		if( randomDungeon.wallLayer == null )
			return;

		if( tile == null ) {
			GUI.DrawTexture( position, EditorGUIUtility.whiteTexture );
			return;
		}

		int tileSize = randomDungeon.wallLayer.tileSize;
		int spacing = randomDungeon.wallLayer.spacing;
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
