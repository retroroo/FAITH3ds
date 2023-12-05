using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[AddComponentMenu("2D/Random Dungeon")]
public class RandomDungeon : MonoBehaviour {

	[System.Serializable]
	public class FloorObject {
		public float chance;
		public int minAmount;
		public int maxAmount;
		public Vector2 offset;
		public bool mustBeReachable;
		public GameObject obj;
	}

	public FloorObject[] floorObjects;
	public List<GameObject> floorGameObjects;

	public int width = 32;
	public int height = 32;
	public int wallHeight = 1;

	public float scale = 0.15f;
	public float floorChance = 0.5f;

	public GameObject stairsUp;
	public GameObject stairsDown;

	public TileInfo wallLayer;
	public TileInfo _floorLayer;
	public TileInfo floorLayer {
		get {
			if( _floorLayer == null ) {
				if( wallTile != Tile.empty )
					_floorLayer = CreateFloorLayer();
				else
					_floorLayer = wallLayer;
			}
			return _floorLayer;
		}
	}

	public bool randomAtGameStart = true;
	public bool updateFloorColliders = false;
	public bool updateWallColliders = false;

	public Tile floorTile;
	public Tile wallTile;
	public Tile roofTile;

	int DistanceToEdge( int x, int y ) {
		int xDist = Mathf.Min( x, width - 1 - x );
		int yDist = Mathf.Min( y, height - 1 - y - wallHeight );
		return Mathf.Min( xDist, yDist );
	}

	int Distance( int fromIndex, int toIndex ) {
		int fromX = fromIndex % width;
		int fromY = fromIndex / width;
		int toX = toIndex % width;
		int toY = toIndex / width;
		return Mathf.Abs(fromX - toX) + Mathf.Abs( fromY - toY );
	}

	TileInfo CreateFloorLayer () {
		GameObject result;
		
		result = new GameObject( name + "_floor" );
		result.transform.position = new Vector3( transform.position.x, transform.position.y, transform.position.z + 0.1f );
		result.transform.parent = transform;
		result.AddComponent<MeshFilter>().sharedMesh = new Mesh();
		result.AddComponent<MeshRenderer>().material = wallLayer.GetComponent<MeshRenderer>().sharedMaterial;
		TileInfo ti = result.AddComponent<TileInfo>();
		ti.collisions = (TileInfo.CollisionType[])wallLayer.collisions.Clone();

		ti.tileSize = wallLayer.tileSize;
		ti.spacing = wallLayer.spacing;
		//result.GetComponent<TileInfo>().positionAtLastEdit = pos;
		ti.mapWidth = width;
		ti.mapHeight = height;
		ti.tiles = new Tile[width * height];
		ti.autoTileData = new List<Tile>( wallLayer.autoTileData );
		ti.autoTileEdgeMode = new List<TileInfo.AutoTileEdgeMode>( wallLayer.autoTileEdgeMode );
		ti.autoTileLinkMask = new List<int>( wallLayer.autoTileLinkMask );
		ti.autoTileNames = new List<string>( wallLayer.autoTileNames );
		ti.autoTileType = new List<TileInfo.AutoTileType>( wallLayer.autoTileType );
		ti.numberOfAutotiles = wallLayer.numberOfAutotiles;
		ti.showAutoTile = new List<bool>( wallLayer.showAutoTile );
		for( int i = 0; i < ti.tiles.Length; i++ ) {
			ti.tiles[i] = Tile.empty;
		}
		return ti;
	}

	public void GenerateRandomDungeon ( bool fromEditor = false ) {
		if( floorTile == Tile.empty )
			return;

		wallLayer.tiles = new Tile[width * height];
		floorLayer.tiles = new Tile[width * height];
		for( int i = 0; i < wallLayer.tiles.Length; i++ ) {
			wallLayer.tiles[i] = Tile.empty;
			floorLayer.tiles[i] = Tile.empty;
		}
		wallLayer.mapWidth = width;
		wallLayer.mapHeight = height;
//		wallLayer._numberOfTiles = 0;

		floorLayer.mapWidth = width;
		floorLayer.mapHeight = height;
//		floorLayer._numberOfTiles = 0;

		List<int> floorTileIndices = new List<int>();

		float offsetX = Random.Range( 0f, 1000000f );
		float offsetY = Random.Range( 0f, 1000000f );

		for( float x = 0; x < width; x++ ) {
			for( float y = 0; y < height; y++ ) {

				float a = Mathf.Max( Mathf.PerlinNoise( x * scale + offsetX, y * scale + offsetY ), 0 );
				int dist = DistanceToEdge( (int)x, (int)y );
				float chance = floorChance;
				if( dist <= 0 )
					chance = 0;
				else if( dist < 4 ) {
					chance -= (1f / dist) * a;
				}
				if( a < chance ) {
					floorLayer.tiles[ (int)y * width + (int)x ] = new Tile( (Vector2)floorTile, floorTile.rotation, floorTile.autoTileIndex, floorTile.flip );
					floorTileIndices.Add( (int)y * width + (int)x );
//					floorLayer._numberOfTiles++;
				}
			}
		}

		if( wallTile != Tile.empty )
			AddWalls( floorTileIndices );

		if( floorTileIndices.Count > 1 ) {

			List<int> validFloorTileIndices = new List<int>();
			AddStairs( floorTileIndices, ref validFloorTileIndices );
			AddFloorObjects( floorTileIndices, validFloorTileIndices, fromEditor );

		}
		else {
			RemoveFloorObjects( fromEditor );
		}
		wallLayer.mapHasChanged = true;
		wallLayer.UpdateVisualMesh( fromEditor );
		floorLayer.mapHasChanged = true;
		floorLayer.UpdateVisualMesh( fromEditor );
		if( updateFloorColliders )
			floorLayer.UpdateColliders();
		if( updateWallColliders )
			wallLayer.UpdateColliders();
	}

	void AddWallAtIndex ( int index, List<int> wallTileIndices, bool includeFloor ) {
		if( index == -1 )
			return;
		if( floorLayer.tiles[index] == Tile.empty ) {
			wallLayer.tiles[index] = new Tile( (Vector2)wallTile, wallTile.rotation, wallTile.autoTileIndex, wallTile.flip );
//			wallLayer._numberOfTiles++;
			wallTileIndices.Add( index );
			if( includeFloor ) {
				floorLayer.tiles[index] = new Tile( (Vector2)floorTile, floorTile.rotation, floorTile.autoTileIndex, floorTile.flip );
//				floorLayer._numberOfTiles++;
			}
		}
	}

	void AddWalls ( List<int> floorTileIndices ) {
		List<int> wallTileIndices = new List<int>();
		foreach( int i in floorTileIndices ) {
			Vector2 tilePos = new Vector2( i % width, (int)i / width );

			AddWallAtIndex ( wallLayer.LocalPointToMapIndex( tilePos + Vector2.up ), wallTileIndices, true );
			AddWallAtIndex ( wallLayer.LocalPointToMapIndex( tilePos - Vector2.up ), wallTileIndices, false );
			AddWallAtIndex ( wallLayer.LocalPointToMapIndex( tilePos + Vector2.right ), wallTileIndices, true );
			AddWallAtIndex ( wallLayer.LocalPointToMapIndex( tilePos - Vector2.right ), wallTileIndices, true );

			AddWallAtIndex ( wallLayer.LocalPointToMapIndex( tilePos + Vector2.up + Vector2.right ), wallTileIndices, true );
			AddWallAtIndex ( wallLayer.LocalPointToMapIndex( tilePos - Vector2.up + Vector2.right ), wallTileIndices, false );
			AddWallAtIndex ( wallLayer.LocalPointToMapIndex( tilePos + Vector2.up - Vector2.right ), wallTileIndices, true );
			AddWallAtIndex ( wallLayer.LocalPointToMapIndex( tilePos - Vector2.up - Vector2.right ), wallTileIndices, false );
		}

		foreach( int i in wallTileIndices ) {
			Vector2 tilePos = new Vector2( i % width, (int)i / width );
			for( int index = 1; index < wallHeight; index++ ) {
				int upWallIndex = wallLayer.LocalPointToMapIndex( tilePos + Vector2.up * index );
				if( upWallIndex != -1 ) {
					if( wallLayer.tiles[upWallIndex] == Tile.empty ) {
						wallLayer.tiles[upWallIndex] = new Tile( (Vector2)wallTile, wallTile.rotation, wallTile.autoTileIndex, wallTile.flip );
//						wallLayer._numberOfTiles++;
					}
					if( floorLayer.tiles[upWallIndex] == floorTile ) {
						floorTileIndices.Remove( upWallIndex );
					}
				}
			}
			int roofIndex = wallLayer.LocalPointToMapIndex( tilePos + Vector2.up * wallHeight );
			if( roofIndex != -1 && roofTile != Tile.empty ) {
				if( wallLayer.tiles[roofIndex] == Tile.empty ) {
					wallLayer.tiles[roofIndex] = new Tile( (Vector2)roofTile, roofTile.rotation, roofTile.autoTileIndex, roofTile.flip );
//					wallLayer._numberOfTiles++;
				}
				if( floorLayer.tiles[roofIndex] == floorTile ) {
					floorTileIndices.Remove( roofIndex );
				}
				if( wallLayer.tiles[roofIndex] == wallTile ) {
					wallLayer.tiles[roofIndex] = new Tile( (Vector2)roofTile, roofTile.rotation, roofTile.autoTileIndex, roofTile.flip );
				}
			}
		}
	}

	void AddAllValidFloorTiles ( List<int> validTileIndices, int index ) {
		if( validTileIndices.Contains( index ) )
			return;
		if( index == -1 || ( wallLayer.tiles[index] != Tile.empty && wallTile != Tile.empty ) )
			return;

		validTileIndices.Add( index );
		int x = index % width;
		int y = index / width;
		AddAllValidFloorTiles( validTileIndices, wallLayer.LocalPointToMapIndex( new Vector2( x + 1, y ) ) );
		AddAllValidFloorTiles( validTileIndices, wallLayer.LocalPointToMapIndex( new Vector2( x - 1, y ) ) );
		AddAllValidFloorTiles( validTileIndices, wallLayer.LocalPointToMapIndex( new Vector2( x, y + 1 ) ) );
		AddAllValidFloorTiles( validTileIndices, wallLayer.LocalPointToMapIndex( new Vector2( x, y - 1 ) ) );
	}

	void AddStairs ( List<int> floorTileIndices, ref List<int> validTileIndices ) {
		if( floorTileIndices.Count == 0 )
			return;
		int upIndex = 0;
		for( int n = 0; n < 10; n++ ) {
			int tempUpIndex = floorTileIndices[Random.Range( 0, floorTileIndices.Count )];
			List<int> tempValidTileIndices = new List<int>();
			AddAllValidFloorTiles( tempValidTileIndices, tempUpIndex );
			tempValidTileIndices.Remove( tempUpIndex );
			if( tempValidTileIndices.Count > validTileIndices.Count ) {
				validTileIndices = tempValidTileIndices;
				upIndex = tempUpIndex;
			}
			if( validTileIndices.Count > width * height * floorChance * 0.4f ) {
				break;
			}
		}

		int downIndex = validTileIndices[Random.Range( 0, validTileIndices.Count )];
		for( int n = 0; n < 10; n++ ) {
			int i = validTileIndices[Random.Range( 0, validTileIndices.Count )];
			if( Distance( i, upIndex ) > Distance( downIndex, upIndex ) )
				downIndex = i;
		}

		if( stairsUp != null )
			stairsUp.transform.position = new Vector3( transform.position.x + (upIndex % width), transform.position.y + (upIndex / width), stairsUp.transform.position.z );

		if( stairsDown != null )
			stairsDown.transform.position = new Vector3( transform.position.x + (downIndex % width), transform.position.y + (downIndex / width), stairsDown.transform.position.z );
	}

	void RemoveFloorObjects ( bool fromEditor ) {
		if( floorGameObjects != null ) {
			foreach( GameObject go in floorGameObjects ) {
				if( fromEditor )
					DestroyImmediate( go );
				else
					Destroy( go );
			}
		}
		floorGameObjects = new List<GameObject>();
	}

	void AddFloorObjects ( List<int> floorTileIndices, List<int> validTileIndices, bool fromEditor ) {
		RemoveFloorObjects( fromEditor );

		if( floorObjects == null || floorObjects.Length == 0 )
			return;
		foreach( FloorObject floorObj in floorObjects ) {
			if( Random.value > floorObj.chance )
				continue;
			if( floorTileIndices.Count == 0 )
				break;
			int amount = Random.Range( floorObj.minAmount, floorObj.maxAmount + 1 );
			for( int n = 0; n < amount; n++ ) {
				int index;
				if( floorObj.mustBeReachable )
					index = validTileIndices[Random.Range( 0, validTileIndices.Count )];
				else
					index = floorTileIndices[Random.Range( 0, floorTileIndices.Count )];
				Vector3 pos = new Vector3( (index % width) + transform.position.x, (index / width) + transform.position.y, transform.position.z );

				GameObject go = (GameObject)Instantiate( floorObj.obj, pos, floorObj.obj.transform.rotation );
				go.name = floorObj.obj.name;
				go.transform.parent = transform;
				floorGameObjects.Add( go );
				floorTileIndices.Remove( index );
				validTileIndices.Remove( index );
			}
		}
	}

	void Start () {
		if( randomAtGameStart ) {
			GenerateRandomDungeon();
		}
	}

}
