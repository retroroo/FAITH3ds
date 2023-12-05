using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(TileDataAsset))]
public class TileDataAssetEditor : Editor {

	TileDataAsset data;

	void OnEnable () {
		data = (TileDataAsset)target;
	}

	public override void OnInspectorGUI () {
		GUI.enabled = false;
		base.OnInspectorGUI ();
		GUI.enabled = true;
		if( GUILayout.Button( "Select in Tileset Editor" ) ) {
			TilesetEditor.selectedTiles = new Tile[data.width, data.height];

			for( int x = 0; x < data.width; x++ ) {
				for (int y = 0; y < data.height; y++) {
					TilesetEditor.selectedTiles[x,y] = data.tiles[ (data.height-y-1) * data.width + x ];
					TilesetEditor.autoTileSelected = -1;
				}
			}
		}
	}
}
