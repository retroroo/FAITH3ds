using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[AddComponentMenu("2D/Tile Prefabs")]
public class TilePrefabs : MonoBehaviour {
	
	[System.Serializable]
	public class TilePrefabInfo {
		public string name;
		public Tile[] tiles;
		public GameObject prefab;
		public Vector3 offset;
		public bool dontDrawTile;
	}

	TileInfo _tileInfo;
	public TileInfo tileInfo {
		get {
			if( _tileInfo == null )
				_tileInfo = GetComponent<TileInfo>();
			return _tileInfo;
		}
	}

	public TilePrefabInfo[] prefabs;
	[HideInInspector] public int[] prefabInfoIndexGrid;
	[HideInInspector] public List<GameObject> prefabGameObjectReferences = new List<GameObject>();

	public int[] PrefabInfoIndexGrid {
		get {
			return prefabInfoIndexGrid;
		}
	}

	public TilePrefabInfo GetPrefabInfo( Tile tile ) {
		
		if( tile == Tile.empty || tileInfo == null )
			return null;
		int index = tile.yIndex * tileInfo.width + tile.xIndex;

		if( prefabInfoIndexGrid == null || prefabInfoIndexGrid.Length <= index )
			return null;
		if( prefabInfoIndexGrid[index] >= 0 ) {
			
			return prefabs[prefabInfoIndexGrid[index]];
		}
		return null;
	}

	public bool HasPrefabInfo( Tile tile ) {
		if( tile == Tile.empty || tileInfo == null )
			return false;
		int index = tile.yIndex * tileInfo.width + tile.xIndex;
		if( prefabInfoIndexGrid[index] >= 0 )
			return true;
		return false;
	}

	public void AddPrefabObject( int xIndex, int yIndex, TilePrefabInfo prefabInfo ) {
		if( prefabInfo == null || prefabInfo.prefab == null || xIndex< 0 || yIndex < 0 )
			return;
		Vector3 prefabPos = new Vector3( xIndex, yIndex ) * tileInfo.zoomFactor + transform.position + prefabInfo.offset;
		GameObject prefabGO = (GameObject)Instantiate( prefabInfo.prefab, prefabPos, prefabInfo.prefab.transform.rotation );
		prefabGO.transform.parent = transform;
		prefabGameObjectReferences.Add( prefabGO );
	}

	public void RemoveAllPrefabs ( bool fromEditor ) {
		foreach( GameObject go in prefabGameObjectReferences ) {
			
			if( fromEditor )
				DestroyImmediate( go );
			else
				Destroy( go );
		}
		prefabGameObjectReferences = new List<GameObject>();
	}
}
