using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(TilePrefabs))]
public class TilePrefabsEditor : Editor {


	TilePrefabs tilePrefabs;
	TileInfo tileInfo;
	SerializedProperty prefabs;
	SerializedProperty prefabInfoIndexGrid;

	void UpdateIndexGrid () {
		prefabInfoIndexGrid.arraySize = tileInfo.width * tileInfo.height;
		for( int i = 0; i < prefabInfoIndexGrid.arraySize; i++ ) {
			prefabInfoIndexGrid.GetArrayElementAtIndex(i).intValue = -1;
		}
		for( int i = 0; i < prefabs.arraySize; i++ ) {
			for( int t = 0; t < prefabs.GetArrayElementAtIndex(i).FindPropertyRelative( "tiles" ).arraySize; t++ ) {
				SerializedProperty thisTile = prefabs.GetArrayElementAtIndex(i).FindPropertyRelative( "tiles" ).GetArrayElementAtIndex(t);
				if( thisTile.FindPropertyRelative( "xIndex" ).intValue == -1 )
					continue;
				int index = thisTile.FindPropertyRelative( "yIndex" ).intValue * tileInfo.width + thisTile.FindPropertyRelative( "xIndex" ).intValue;
				prefabInfoIndexGrid.GetArrayElementAtIndex(index).intValue = i;
			}
		}
		serializedObject.ApplyModifiedProperties();
	}

	void OnEnable () {
		tilePrefabs = (TilePrefabs)target;
		prefabs = serializedObject.FindProperty( "prefabs" );
		prefabInfoIndexGrid = serializedObject.FindProperty( "prefabInfoIndexGrid" );
		tileInfo = tilePrefabs.GetComponent<TileInfo>();
		if( tileInfo == null )
			return;
		if( prefabInfoIndexGrid.arraySize != tileInfo.width * tileInfo.height ) {
			UpdateIndexGrid();
		}
	}

	string searchString = "";
	public override void OnInspectorGUI () {
		serializedObject.Update();

		EditorGUILayout.BeginHorizontal();
		searchString = EditorGUILayout.TextField( searchString, (GUIStyle)"SearchTextField" );
		if( searchString == "" )
			GUILayout.Button( GUIContent.none, "SearchCancelButtonEmpty" );
		else {
			if( GUILayout.Button( GUIContent.none, "SearchCancelButton" ) ) {
				searchString = "";
				EditorGUIUtility.editingTextField = false;
			}
		}
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
		GUILayout.Label( "Tile Prefabs", EditorStyles.boldLabel );
		if( GUILayout.Button( "+" ) ) {
			int index = prefabs.arraySize;
			prefabs.InsertArrayElementAtIndex( index );
			prefabs.GetArrayElementAtIndex( index ).FindPropertyRelative( "tiles" ).arraySize = 0;
			prefabs.GetArrayElementAtIndex( index ).FindPropertyRelative( "name" ).stringValue = "Tile Prefab " + index;
		}
		EditorGUILayout.EndHorizontal();


		for( int i = 0; i < prefabs.arraySize; i++ ) {
			if( prefabs.GetArrayElementAtIndex(i).FindPropertyRelative( "name" ).stringValue.Contains( searchString ) ) {
				EditorGUILayout.BeginVertical("box");
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.PropertyField( prefabs.GetArrayElementAtIndex(i).FindPropertyRelative( "name" ), GUIContent.none );
				if( GUILayout.Button( "-" ) ) {
					prefabs.DeleteArrayElementAtIndex( i );
					UpdateIndexGrid();
					EditorGUILayout.EndHorizontal();
					EditorGUILayout.EndVertical();
					return;
				}
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
				GUILayout.Label( "Tiles", EditorStyles.boldLabel );
				if( GUILayout.Button( "+" ) ) {
					SerializedProperty tiles = prefabs.GetArrayElementAtIndex(i).FindPropertyRelative( "tiles" );
					int index =  tiles.arraySize;
					tiles.InsertArrayElementAtIndex( index );
					tiles.GetArrayElementAtIndex( index ).FindPropertyRelative( "xIndex" ).intValue = 0;
					tiles.GetArrayElementAtIndex( index ).FindPropertyRelative( "yIndex" ).intValue = 0;
					tiles.GetArrayElementAtIndex( index ).FindPropertyRelative( "flip" ).boolValue = false;
					tiles.GetArrayElementAtIndex( index ).FindPropertyRelative( "rotation" ).intValue = 0;
					tiles.GetArrayElementAtIndex( index ).FindPropertyRelative( "autoTileIndex" ).intValue = -1;
					UpdateIndexGrid();
				}
				EditorGUILayout.EndHorizontal();
				for( int t = 0; t < prefabs.GetArrayElementAtIndex(i).FindPropertyRelative( "tiles" ).arraySize; t++ ) {
					SerializedProperty thisTile = prefabs.GetArrayElementAtIndex(i).FindPropertyRelative( "tiles" ).GetArrayElementAtIndex(t);
					EditorGUI.BeginChangeCheck();
					EditorGUILayout.PropertyField( thisTile );
					if( EditorGUI.EndChangeCheck() ) {
						if( thisTile.FindPropertyRelative( "xIndex" ).intValue == -1 ) {
							prefabs.GetArrayElementAtIndex(i).FindPropertyRelative( "tiles" ).DeleteArrayElementAtIndex(t);
						}
						UpdateIndexGrid();
						EditorGUILayout.EndVertical();
						return;
					}
				}

				EditorGUILayout.PropertyField( prefabs.GetArrayElementAtIndex(i).FindPropertyRelative( "prefab" ) );
				EditorGUILayout.PropertyField( prefabs.GetArrayElementAtIndex(i).FindPropertyRelative( "offset" ) );
				EditorGUILayout.PropertyField( prefabs.GetArrayElementAtIndex(i).FindPropertyRelative( "dontDrawTile" ) );
				EditorGUILayout.EndVertical();
			}
		}

//		EditorGUILayout.PropertyField( serializedObject.FindProperty( "prefabInfoIndexGrid" ), true );
//		EditorGUILayout.PropertyField( serializedObject.FindProperty( "prefabGameObjectReferences" ), true );

		serializedObject.ApplyModifiedProperties();
	}

}
