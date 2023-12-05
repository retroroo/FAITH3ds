using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[AddComponentMenu("2D/Random Platformer")]
public class RandomPlatformer : MonoBehaviour {
	
	[System.Serializable]
	public class FloorObject {
		[Range( 0, 1 )]
		public float chance;
		public int minAmount;
		public int maxAmount;
		public Vector3 offset;
		public int width;
		public bool mustBeReachable;
		public int minDistanceFromStart;
		public GameObject obj;
	}
	
	public FloorObject[] floorObjects;
	public List<GameObject> floorGameObjects;

	[Tooltip( "Width in chunks" )]
	public int width = 1;
	[Tooltip( "Height in chunks" )]
	public int height = 1;

	[Tooltip( "Width of each chunk in tiles" )]
	public int chunkWidth = 16;
	[Tooltip( "Height of each chunk in tiles" )]
	public int chunkHeight = 16;
	
	public int otherPlatforms = 10;
	public int pathHeight = 3;
	public int pathThickness = 1;
	public int solutionDistance = 8;

	public float backgroundZOffset = 10;
	
	public Vector2 scale = new Vector2( 0.15f, 0.15f );
	public float floorChance = 0.5f;
	public float backgroundChance = 0.7f;

	[Tooltip( "Generates ParallaxBackground if non zero" )]
	public float parallaxDistance;
	
	public GameObject entryObj;
	public GameObject exitObj;

	public Vector3 entryObjOffset;
	public Vector3 exitObjOffset;
	
	[SerializeField] RandomPlatformer _mainLayer;
	[SerializeField] int ladderLayer;

	List<int> pathChunkIndices;
	List<int> pathHotspotIndices;
	List<Vector2> oneAboveGround;
	List<Vector2> oneAboveGroundSolution;
	HashSet<Vector2> lockedTiles;
	Vector2 startPoint;
	Vector2 endPoint;

	public Tile this[ int x, int y ] {
		get {
			if( !IsInBounds( x, y ) )
				return null;
			int chunkIndex = ( y / chunkHeight ) * width + ( x / chunkWidth );
			int tileIndex = ( y % chunkHeight ) * chunkWidth + ( x % chunkWidth );
			return mainLayer.wallLayers[ chunkIndex ].tiles[tileIndex];
		}
		set {
			if( !IsInBounds( x, y ) )
				return;
			int chunkIndex = ( y / chunkHeight ) * width + ( x / chunkWidth );
			int tileIndex = ( y % chunkHeight ) * chunkWidth + ( x % chunkWidth );
			mainLayer.wallLayers[ chunkIndex ].tiles[tileIndex] = value;
		}
	}

	public RandomPlatformer mainLayer {
		get {
			if( _mainLayer == null )
				_mainLayer = this;
			return _mainLayer;
		}
		set {
			_mainLayer = value;
		}
	}

	public TileInfo wallLayer;
	public TileInfo backgroundLayer;

	public TileInfo[] _wallLayers;
	public TileInfo[] wallLayers {
		get {
			return mainLayer._wallLayers;
		}
	}

	public TileInfo[] _backgroundLayers;
	public TileInfo[] backgroundLayers {
		get {
			return mainLayer._backgroundLayers;
		}
	}
	

	public bool randomAtGameStart = true;
	public bool addBorder = true;
	public bool updateColliders = false;
	
	public Tile floorTile;
	public Tile backgroundTile;
	public Tile ladderTile;


	T CopyComponent<T>(T original, GameObject destination) where T : Component
	{
		System.Type type = original.GetType();
		Component copy = destination.AddComponent(type);
		System.Reflection.FieldInfo[] fields = type.GetFields();
		foreach (System.Reflection.FieldInfo field in fields)
		{
			field.SetValue(copy, field.GetValue(original));
		}
		return copy as T;
	}

	TileInfo CloneLayer( int newIndex, TileInfo layerToClone, bool fromEditor ) {
//		GameObject result = (GameObject)Instantiate( layerToClone.gameObject );
//		if( fromEditor )
//			DestroyImmediate( result.GetComponent<PolygonCollider2D>() );
//		else
//			Destroy( result.GetComponent<PolygonCollider2D>() );

		GameObject result = new GameObject( layerToClone.name + "_" + newIndex );
//		result.hideFlags = HideFlags.HideInHierarchy;
		result.AddComponent<MeshFilter>().sharedMesh = new Mesh();
		result.AddComponent<MeshRenderer>().material = layerToClone.GetComponent<MeshRenderer>().sharedMaterial;
		TileInfo ti = result.AddComponent<TileInfo>();
		ti.collisions = (TileInfo.CollisionType[])wallLayer.collisions.Clone();
		
		ti.tileSize = wallLayer.tileSize;
		ti.spacing = wallLayer.spacing;
		//result.GetComponent<TileInfo>().positionAtLastEdit = pos;
		ti.mapWidth = chunkWidth;
		ti.mapHeight = chunkHeight;
		ti.tiles = new Tile[chunkWidth * chunkHeight];
		ti.autoTileData = new List<Tile>( wallLayer.autoTileData );
		ti.autoTileEdgeMode = new List<TileInfo.AutoTileEdgeMode>( wallLayer.autoTileEdgeMode );
		ti.autoTileLinkMask = new List<int>( wallLayer.autoTileLinkMask );
		ti.autoTileNames = new List<string>( wallLayer.autoTileNames );
		ti.autoTileType = new List<TileInfo.AutoTileType>( wallLayer.autoTileType );
		ti.numberOfAutotiles = wallLayer.numberOfAutotiles;
		ti.showAutoTile = new List<bool>( wallLayer.showAutoTile );

		ti.pixelColliders = layerToClone.pixelColliders;
		for( int i = 0; i < ti.tiles.Length; i++ ) {
			ti.tiles[i] = Tile.empty;
		}

		if( layerToClone.GetComponent<MeshCollider>() != null )
			result.AddComponent<MeshCollider>().sharedMesh = new Mesh();
		RandomPlatformer rp = CopyComponent<RandomPlatformer>( layerToClone.GetComponent<RandomPlatformer>(), result );
		if( layerToClone.GetComponent<TilePrefabs>() != null )
			CopyComponent<TilePrefabs>( layerToClone.GetComponent<TilePrefabs>(), result );
		rp.mainLayer = mainLayer;
		rp.wallLayer = ti;
		rp._backgroundLayers = null;
		rp._wallLayers = null;
		rp.randomAtGameStart = false;
		Vector3 newPos = mainLayer.transform.position;
		if( newIndex >= 0 ) {
			int x = newIndex % width;
			int y = newIndex / width;
			newPos.x += chunkWidth * x;
			newPos.y += chunkHeight * y;
			ti.update3DWalls = layerToClone.update3DWalls;
		}
		else {
			//else is a background layer
			newPos = layerToClone.transform.position;
			newPos.z += backgroundZOffset;
			result.transform.parent = layerToClone.transform;
			ti.update3DWalls = layerToClone.update3DWalls;
			if( parallaxDistance != 0 ) {
				ParallaxBackground pb = result.AddComponent<ParallaxBackground>();
				pb.distance = parallaxDistance;
				pb.cam = Camera.main;
				Vector2 midPoint = new Vector2( ( width * chunkWidth ) / 2 - chunkWidth / 2, ( height * chunkHeight ) / 2 - chunkHeight / 2 ) + (Vector2)mainLayer.transform.position;
				float ratio = parallaxDistance / 10;
				pb.startPosition = new Vector3( newPos.x + ratio * ( newPos.x - midPoint.x ), newPos.y + ratio * ( newPos.y - midPoint.y ), parallaxDistance );
				if( FindObjectOfType<PixelPerfectCamera>() != null )
					pb.pixelSnap = true;
			}
		}
		result.transform.position = newPos;

		MeshCollider mc = result.GetComponent<MeshCollider>();
		if( mc != null )
			mc.sharedMesh = result.GetComponent<MeshFilter>().sharedMesh;

		return ti;
	}

	void DestroyLayers( bool fromEditor ) {
		if( wallLayers != null ) {
			foreach( TileInfo ti in wallLayers ) {
				if( ti == null || ti.gameObject == gameObject )
					continue;

				if( fromEditor ) {
					DestroyImmediate( ti.gameObject );
				}
				else {
					Destroy( ti.gameObject );
				}
			}
		}
		_wallLayers = null;


		if( _backgroundLayers != null ) {
			foreach( TileInfo ti in backgroundLayers ) {
				if( ti == null )
					continue;
				
				if( fromEditor ) {
					DestroyImmediate( ti.gameObject );
				}
				else {
					Destroy( ti.gameObject );
				}
			}
			_backgroundLayers = null;
			backgroundLayer = null;
		}
	}

	void ConnectLayers ( TileInfo[] layers ) {
		for( int x = 0; x < width; x++ ) {
			for( int y = 0; y < height; y++ ) {
				int index = (y) * width + (x+1);
				if( x < width-1 && index >= 0 && index < width * height )
					layers[y*width+x].rightLayer = layers[index];
				index = (y) * width + (x-1);
				if( x > 0 && index >= 0 && index < width * height )
					layers[y*width+x].leftLayer = layers[index];
				index = (y+1) * width + (x);
				if( y < height-1 && index >= 0 && index < width * height )
					layers[y*width+x].upLayer = layers[index];
				index = (y-1) * width + (x);
				if( y > 0 && index >= 0 && index < width * height )
					layers[y*width+x].downLayer = layers[index];

				index = (y+1) * width + (x+1);
				if( y < height-1 && x < width-1 && index >= 0 && index < width * height )
					layers[y*width+x].upRightLayer = layers[index];
				index = (y+1) * width + (x-1);
				if( y < height-1 && x > 0 && index >= 0 && index < width * height )
					layers[y*width+x].upLeftLayer = layers[index];
				index = (y-1) * width + (x+1);
				if( y > 0 && x < width-1 && x < width-1 && index >= 0 && index < width * height )
					layers[y*width+x].downRightLayer = layers[index];
				index = (y-1) * width + (x-1);
				if( y > 0 && x > 0 && index >= 0 && index < width * height )
					layers[y*width+x].downLeftLayer = layers[index];
			}
		}
	}

	void CreateLayers ( bool fromEditor ) {

		if( wallLayers == null || wallLayers.Length == 0 ) {
			_wallLayers = new TileInfo[width * height];
			wallLayers[0] = wallLayer;
			for( int i = 1; i < wallLayers.Length; i++ ) {
				wallLayers[i] = CloneLayer( i, wallLayer, fromEditor );
			}
			ConnectLayers( wallLayers );
		}

		if( backgroundTile == Tile.empty )
			return;

		if( backgroundLayer == null ) {
			backgroundLayer = CloneLayer( -1, wallLayer, fromEditor  );
		}

		if( backgroundLayers == null || backgroundLayers.Length == 0 ) {
			_backgroundLayers = new TileInfo[width * height];
			backgroundLayers[0] = backgroundLayer;
			for( int i = 1; i < wallLayers.Length; i++ ) {
				backgroundLayers[i] = CloneLayer( -1, wallLayers[i], fromEditor  );
				wallLayers[i].GetComponent<RandomPlatformer>().backgroundLayer = backgroundLayers[i];
			}
			ConnectLayers( backgroundLayers );
		}
	}
	

	public void GenerateRandomPlatformer ( bool fromEditor = false, float offsetX = -1, float offsetY = -1 ) {
		if( floorTile == Tile.empty )
			return;

		if( mainLayer == this ) {
			DestroyLayers( fromEditor );
			CreateLayers( fromEditor );
		}

		wallLayer.tiles = new Tile[chunkWidth * chunkHeight];
		if( backgroundLayer != null )
			backgroundLayer.tiles = new Tile[chunkWidth * chunkHeight];
		for( int i = 0; i < wallLayer.tiles.Length; i++ ) {
			wallLayer.tiles[i] = Tile.empty;
			if( backgroundLayer != null )
				backgroundLayer.tiles[i] = Tile.empty;
		}
		wallLayer.mapWidth = chunkWidth;
		wallLayer.mapHeight = chunkHeight;
//		wallLayer._numberOfTiles = 0;
//		oneAboveGroundIndices = new HashSet<int>();

		if( backgroundLayer != null ) {
			backgroundLayer.mapWidth = chunkWidth;
			backgroundLayer.mapHeight = chunkHeight;
//			backgroundLayer._numberOfTiles = 0;
		}
		
//		List<int> floorTileIndices = new List<int>();

		if( offsetX == -1 )
			offsetX = Random.Range( 0f, 1000000f );
		if( offsetY == -1 )
			offsetY = Random.Range( 0f, 1000000f );
		
		for( float x = 0; x < chunkWidth; x++ ) {
			for( float y = chunkHeight-1; y >= 0; y-- ) {
				
				float a = Mathf.Max( Mathf.PerlinNoise( x * scale.x + offsetX * scale.x, y * scale.y + offsetY * scale.y ), 0 );
				a = Mathf.Min( a, 1 );
				if( a <= floorChance ) {
					wallLayer.tiles[ (int)y * chunkWidth + (int)x ] = floorTile;

//					if( y < chunkHeight - 1 ) {
//						int indexUp = (int)(y+1) * chunkWidth + (int)x;
//						if( wallLayer.tiles[ indexUp ] == Tile.empty )
//							oneAboveGroundIndices.Add( indexUp );
//					}
				}
				if( backgroundTile != Tile.empty && a <= backgroundChance ) {
					backgroundLayer.tiles[ (int)y * chunkWidth + (int)x ] = backgroundTile;
				}
			}
		}

		if( mainLayer == this ) {
			lockedTiles = new HashSet<Vector2>();
			for( int x = 0; x < width; x++ ) {
				for( int y = 0; y < height; y++ ) {
					if( x == 0 && y == 0 )
						continue;
					float newOffsetX = offsetX + x * chunkWidth;
					float newOffsetY = offsetY + y * chunkHeight;
					wallLayers[ y * width + x ].GetComponent<RandomPlatformer>().GenerateRandomPlatformer( fromEditor, newOffsetX, newOffsetY );
				}
			}
			oneAboveGround = new List<Vector2>();
			oneAboveGroundSolution = new List<Vector2>();
			RemoveFloorObjects( fromEditor );
			DestroyLadders( fromEditor );
			GeneratePath();
			CarvePlatforms();
			AddFloorObjects( fromEditor );
			AddStartAndExit();
			AddBorder();
			UpdateMeshes( fromEditor );
		}
	}

	public void UpdateMeshes ( bool fromEditor ) {
		foreach( TileInfo ti in wallLayers ) {
			ti.mapHasChanged = true;
			ti.UpdateVisualMesh( fromEditor );
			if( updateColliders )
				ti.UpdateColliders();
		}
		if( backgroundLayers != null ) {
			foreach( TileInfo ti in backgroundLayers ) {
				ti.mapHasChanged = true;
				ti.UpdateVisualMesh( fromEditor );
			}
		}
	}

	bool isValidChunk ( int x, int y ) {
		if( x < 0 || x >= width || y < 0 || y >= height )
			return false;
		int index = y * width + x;
		if( pathChunkIndices.Contains( index ) )
			return false;
		return true;
	}

	bool FindPath( int x, int y, int minDistance ) {
		int index = y * width + x;

		pathChunkIndices.Add( index );

		if( minDistance == 0 )
			return true;



		List<Vector2> validMoves = new List<Vector2>();
		if( isValidChunk( x+1, y ) )
			validMoves.Add( new Vector2( x+1, y ) );
		if( isValidChunk( x-1, y ) )
			validMoves.Add( new Vector2( x-1, y ) );

		if( pathChunkIndices.Count < 2 ||
		   pathChunkIndices[pathChunkIndices.Count-2] / width == pathChunkIndices[pathChunkIndices.Count-1] / width ||
		   validMoves.Count == 0 ) {
			if( isValidChunk( x, y+1 ) )
				validMoves.Add( new Vector2( x, y+1 ) );
			if( isValidChunk( x, y-1 ) )
				validMoves.Add( new Vector2( x, y-1 ) );
		}


		if( validMoves.Count == 0 ) {
			return true;
		}

		int move = Random.Range( 0, validMoves.Count );

		return FindPath( (int)validMoves[move].x, (int)validMoves[move].y, minDistance-1 );
	}

	void GeneratePath () {

		pathHotspotIndices = new List<int>();
		int startChunkIndex = Random.Range( 0, width * height );
//		goalChunkIndex = Random.Range( 0, width * height );
//		while( goalChunkIndex == startChunkIndex )
//			goalChunkIndex = Random.Range( 0, width * height );
		pathChunkIndices = new List<int>();
		for( int r = 0; r < 10; r++ ) {
			if( pathChunkIndices.Count < solutionDistance ) {
				pathChunkIndices = new List<int>();

				FindPath( startChunkIndex % width, startChunkIndex / width, solutionDistance );
			}
			else
				break;
		}
		for( int i = 0; i < pathChunkIndices.Count; i++ ) {
			int topLayerYMod = 0;
			if( pathChunkIndices[i] / width == height - 1 )
				topLayerYMod = -1;
			int x = Random.Range( 1, chunkWidth-1 );
			int y = Random.Range( pathThickness, chunkHeight - pathHeight + topLayerYMod );
			for( int r = 0; r < 10; r++ ) {
				if( i > 0 && Mathf.Abs( x - pathHotspotIndices[i-1] % chunkWidth ) < 3 ) {
					x = Random.Range( 1, chunkWidth-1 );
				}
				if( i > 0 && Mathf.Abs( y - pathHotspotIndices[i-1] / chunkWidth ) < 2 ) {
					y = Random.Range( pathThickness, chunkHeight - pathHeight + topLayerYMod );
				}
			}
			pathHotspotIndices.Add( y * chunkWidth + x );
		}
	}
	void LockTile( int x, int y ) {
		if( !IsInBounds( x, y ) )
			return;
		lockedTiles.Add( new Vector2( x, y ) );
	}

//	void CarvePlatform( Vector2 point0, TileInfo map0, Vector2 point1, TileInfo map1 ) {
//
//		if( point0.x > point1.x ) {
//			Vector2 tempPoint = point0;
//			TileInfo tempInfo = map0;
//			point0 = point1;
//			map0 = map1;
//			point1 = tempPoint;
//			map1 = tempInfo;
//		}
//
//		for( int x = (int)point0.x; x <= point1.x; x++ ) {
//			int map0index;
//			int map1index;
//
//			for( int y = 1 - pathThickness; y <= 0; y++ ) {
//				map0index = map0.WorldPointToMapIndex( new Vector2( x, point0.y + y ) );
//				if( map0index != -1 && map0.tiles[ map0index ] != ladderTile ) {
//					ChangeTile( map0, map0index, floorTile );
//				}
//				map1index = map1.WorldPointToMapIndex( new Vector2( x, point0.y + y) );
//				if( map1index != -1 && map1.tiles[ map1index ] != ladderTile  )
//					ChangeTile( map1, map1index, floorTile );
//				LockTile( new Vector2( x, point0.y + y ) );
//			}
//
//			for( int y = 1; y <= pathHeight; y++ ) {
//				map0index = map0.WorldPointToMapIndex( new Vector2( x, point0.y + y ) );
//				if( map0index != -1 && map0.tiles[ map0index ] != ladderTile )
//					ChangeTile( map0, map0index, Tile.empty );
//				map1index = map1.WorldPointToMapIndex( new Vector2( x, point0.y + y ) );
//				if( map1index != -1 && map1.tiles[ map1index ] != ladderTile )
//					ChangeTile( map1, map1index, Tile.empty );
//				LockTile( new Vector2( x, point0.y + y ) );
//			}
//		}
//	}

	bool IsInBounds( int x, int y ) {
		if( x < 0 || y < 0 || x >= chunkWidth*width || y >= chunkHeight*height )
			return false;
		return true;
	}

	void CarvePlatform( Vector2 point0, Vector2 point1, bool addToOneAboveGround, bool isSolutionPath ) {

		point0 -= (Vector2)mainLayer.transform.position;
		point1 -= (Vector2)mainLayer.transform.position;
		if( point0.x > point1.x ) {
			Vector2 tempPoint = point0;
			point0 = point1;
			point1 = tempPoint;
		}

		List<Vector2> tilesToLock = new List<Vector2>();
		
		for( int x = (int)point0.x; x <= point1.x; x++ ) {
			bool fullPath = true;
			for( int y = 1 - pathThickness; y <= 0; y++ ) {

				if( IsInBounds( x, (int)point0.y + y ) && this[ x, (int)point0.y + y] != ladderTile && !lockedTiles.Contains( new Vector2( x, (int)point0.y + y ) ) ) {
					ChangeTile( x, (int)point0.y + y, floorTile );
					tilesToLock.Add( new Vector2( x, (int)point0.y + y ) );
				}
				else if( y == 0 )
					fullPath = false;
			}
			for( int y = pathHeight; y > 0; y-- ) {

				if( IsInBounds( x, (int)point0.y + y ) && this[ x, (int)point0.y + y] != ladderTile && !lockedTiles.Contains( new Vector2( x, (int)point0.y + y ) ) ) {
					ChangeTile( x, (int)point0.y + y, Tile.empty );
					tilesToLock.Add( new Vector2( x, (int)point0.y + y ) );
				}
				else
					fullPath = false;
				if( addToOneAboveGround && fullPath && y == 1 && x > (int)point0.x + 1 && x < (int)point1.x - 1 && !oneAboveGround.Contains( new Vector2( x, point0.y + y ) ) ) {
					if( !lockedTiles.Contains( new Vector2( x-1, point0.y + y ) ) && !lockedTiles.Contains( new Vector2( x+1, point0.y + y ) ) ) {
						oneAboveGround.Add( new Vector2( x, point0.y + y ) );
						if( isSolutionPath )
							oneAboveGroundSolution.Add( new Vector2( x, point0.y + y ) );
					}
				}
			}
		}

		foreach( Vector2 v2 in tilesToLock ) {
			lockedTiles.Add( v2 );
		}
	}
	
	void CarvePlatforms() {
		int hi = pathHotspotIndices[0];
		Vector2 currentPoint = (Vector2)wallLayers[pathChunkIndices[0]].transform.position + new Vector2( hi % chunkWidth, hi / chunkWidth );
		Vector2 newPoint;

		//carve start platform
		if( pathHotspotIndices[0] % chunkWidth > chunkWidth / 2 )
			startPoint = currentPoint - Vector2.right * 5;
		else
			startPoint = currentPoint + Vector2.right * 5;
		
		CarvePlatform( currentPoint, startPoint, false, false );
		if( pathHotspotIndices[0] % chunkWidth > chunkWidth / 2 )
			startPoint += new Vector2( 2, 1 );
		else
			startPoint += new Vector2( -4, 1 );

		//carve end platform
		endPoint = wallLayers[pathChunkIndices[pathChunkIndices.Count-1]].transform.position + new Vector3( pathHotspotIndices[pathHotspotIndices.Count-1] % chunkWidth, pathHotspotIndices[pathHotspotIndices.Count-1] / chunkWidth );
		Vector2 tempPoint = endPoint;
		if( pathHotspotIndices[pathHotspotIndices.Count-1] % chunkWidth > chunkWidth / 2 )
			endPoint -= Vector2.right * 5;
		else
			endPoint += Vector2.right * 5;
		
		CarvePlatform( tempPoint, endPoint, false, false );
		if( pathHotspotIndices[pathHotspotIndices.Count-1] % chunkWidth > chunkWidth / 2 )
			endPoint += new Vector2( 2, 1 );
		else
			endPoint += new Vector2( -4, 1 );

		//carve solution platforms
		for( int i = 1; i < pathChunkIndices.Count; i++ ) {
			TileInfo t0 = wallLayers[pathChunkIndices[i-1]];
			TileInfo t1 = wallLayers[pathChunkIndices[i]];
			Vector2 p0 = t0.transform.position;
			Vector2 p1 = t1.transform.position;
			hi = pathHotspotIndices[i];

			if( p1.y != p0.y ) {
				newPoint = currentPoint;
				newPoint.y = p1.y + hi / chunkWidth;
				CarveLadder( currentPoint, newPoint );
				currentPoint = newPoint;
				newPoint.x = p1.x + hi % chunkWidth;
				CarvePlatform( currentPoint, newPoint, true, true );
				currentPoint = newPoint;
			}
			else {
				newPoint = currentPoint;
				newPoint.x = p1.x + hi % chunkWidth;
				CarvePlatform( newPoint, currentPoint, true, true );
				currentPoint = newPoint;
				newPoint.y = p1.y + hi / chunkWidth;
				CarveLadder( currentPoint, newPoint );
				currentPoint = newPoint;
			}
		}

		//carve other platforms not part of the solution
		for( int i = 0; i < otherPlatforms; i++ ) {
			int x = Random.Range( 2, width*chunkWidth - 2 );
			int y = Random.Range( 2, height*chunkHeight - pathHeight - 2 );
			//only try up to 20 times
			int loopCut = 20;
			while( lockedTiles.Contains( new Vector2( x, y ) ) && loopCut > 0 ) {
				x = Random.Range( 2, width*chunkWidth - 2 );
				y = Random.Range( 2, height*chunkHeight - pathHeight - 2 );
				loopCut--;
			}
			if( loopCut <= 0 )
				break;
			int x2 = Random.Range( 2, width*chunkWidth - 2 );
			while( Mathf.Abs( x - x2 ) < 6 ) {
				x2 = Random.Range( 2, width*chunkWidth - 2 );
			}
			CarvePlatform( new Vector2( x, y ) + (Vector2)mainLayer.transform.position, new Vector2( x2, y ) + (Vector2)mainLayer.transform.position, true, false );
		}
	}

	[SerializeField] List<GameObject> ladderObjects;

	void DestroyLadders ( bool fromEditor ) {
		if( ladderObjects == null )
			return;
		for( int i = 0; i < ladderObjects.Count; i++ ) {
			if( ladderObjects[i] != null ) {
				if( fromEditor )
					DestroyImmediate( ladderObjects[i].gameObject );
				else
					Destroy( ladderObjects[i].gameObject );
			}
		}
		ladderObjects = new List<GameObject>();
	}

//	bool IsOneAboveGround( int x, int y ) {
//		int chunkIndex = ( y / chunkHeight ) * width + ( x / chunkWidth );
//		int tileIndex = ( y % chunkHeight ) * width + ( x % chunkWidth );
//		return wallLayers[ chunkIndex ].GetComponent<RandomPlatformer>().oneAboveGroundIndices.Contains( tileIndex );
//	}

//	void AddOneAboveGround ( int x, int y ) {
//		int chunkIndex = ( y / chunkHeight ) * width + ( x / chunkWidth );
//		int tileIndex = ( y % chunkHeight ) * width + ( x % chunkWidth );
//		wallLayers[ chunkIndex ].GetComponent<RandomPlatformer>().oneAboveGroundIndices.Add( tileIndex );
//	}
//
//	void RemoveOneAboveGround ( int x, int y ) {
//		int chunkIndex = ( y / chunkHeight ) * width + ( x / chunkWidth );
//		int tileIndex = ( y % chunkHeight ) * width + ( x % chunkWidth );
//		wallLayers[ chunkIndex ].GetComponent<RandomPlatformer>().oneAboveGroundIndices.Remove( tileIndex );
//	}

	void ChangeTile ( int x, int y, Tile newTile ) {

		if( this[ x, y ] == newTile )
			return;

		//map.tiles[ index ] = newTile;
		this[ x, y ] = newTile;

		if( newTile == ladderTile ) {
			if( oneAboveGround.Contains( new Vector2( x, y ) ) )
				oneAboveGround.Remove( new Vector2( x, y ) );
			if( IsInBounds( x, y+1 ) && oneAboveGround.Contains( new Vector2( x, y+1 ) ) )
				oneAboveGround.Remove( new Vector2( x, y+1 ) );
			if( oneAboveGroundSolution.Contains( new Vector2( x, y ) ) )
				oneAboveGroundSolution.Remove( new Vector2( x, y ) );
			if( IsInBounds( x, y+1 ) && oneAboveGroundSolution.Contains( new Vector2( x, y+1 ) ) )
				oneAboveGroundSolution.Remove( new Vector2( x, y+1 ) );
		}
//		if( newTile == floorTile ) {
//			if( IsOneAboveGround( x, y ) )
//				RemoveOneAboveGround( x, y );
//			if( IsInBounds( x, y+1 ) && this[ x, y+1 ] == Tile.empty ) {
//				AddOneAboveGround( x, y+1 );
//			}
//		}
//
//		if( newTile == Tile.empty ) {
//			if( IsInBounds( x, y+1 ) && IsOneAboveGround( x, y+1 ) )
//				RemoveOneAboveGround( x, y+1 );
//
//			if( IsInBounds( x, y-1 ) && this[ x, y-1 ] == floorTile )
//				AddOneAboveGround( x, y );
//		}
	}

//	void CarveLadder ( Vector2 point0, TileInfo map0, Vector2 point1, TileInfo map1 ) {
//		if( point0.y > point1.y ) {
//			Vector2 tempPoint = point0;
//			TileInfo tempInfo = map0;
//			point0 = point1;
//			map0 = map1;
//			point1 = tempPoint;
//			map1 = tempInfo;
//		}
//		
//		for( int y = (int)point0.y+1; y < point1.y+1; y++ ) {
//			int map0index = map0.WorldPointToMapIndex( new Vector2( point0.x, y ) );
//			if( map0index != -1 )
//				ChangeTile( map0, map0index, ladderTile );
//			int map1index = map1.WorldPointToMapIndex( new Vector2( point0.x, y ) );
//			if( map1index != -1 )
//				ChangeTile( map1, map1index, ladderTile );
//			LockTile( new Vector2( point0.x, y ) );
//		}
//
//
//		GameObject ladderObj = new GameObject( "Ladder" );
//		ladderObjects.Add( ladderObj );
//		ladderObj.transform.position = new Vector3( point0.x + 0.5f, point0.y + 1 + (point1.y - point0.y) / 2 );
//		BoxCollider2D boxCol = ladderObj.AddComponent<BoxCollider2D>();
//		boxCol.size = new Vector2( 0.5f, (point1.y - point0.y) );
//		boxCol.isTrigger = true;
//		ladderObj.layer = ladderLayer;
//	}

	void CarveLadder ( Vector2 point0, Vector2 point1 ) {


		point0 -= (Vector2)mainLayer.transform.position;
		point1 -= (Vector2)mainLayer.transform.position;
		if( point0.y > point1.y ) {
			Vector2 tempPoint = point0;
			point0 = point1;
			point1 = tempPoint;
		}
		
		for( int y = (int)point0.y + 1; y < point1.y + 1; y++ ) {

			if( IsInBounds( (int)point0.x, y ) ) {
				ChangeTile( (int)point0.x, y, ladderTile );
				LockTile( (int)point0.x, y );
			}
		}
		
		
		GameObject ladderObj = new GameObject( "Ladder" );
		ladderObjects.Add( ladderObj );
		ladderObj.transform.position = new Vector3( point0.x + 0.5f, point0.y + 1 + (point1.y - point0.y) / 2 ) + mainLayer.transform.position;
		if( mainLayer.GetComponent<MeshCollider>() == null ) {
			BoxCollider2D boxCol = ladderObj.AddComponent<BoxCollider2D>();
			boxCol.size = new Vector2( 0.5f, (point1.y - point0.y) );
			boxCol.isTrigger = true;
		}
		else {
			BoxCollider boxCol = ladderObj.AddComponent<BoxCollider>();
			boxCol.size = new Vector3( 0.5f, (point1.y - point0.y), 10 );
			boxCol.isTrigger = true;
		}
		ladderObj.layer = ladderLayer;
	}

	void AddBorder () {
		if( !addBorder )
			return;
		//left and right walls
		for( int cy = 0; cy < height; cy++ ) {
			for( int y = 0; y < chunkHeight; y++ ) {
				wallLayers[ cy * width].tiles[ y * chunkWidth ] = floorTile;
				wallLayers[ cy * width + width - 1 ].tiles[ y * chunkWidth + chunkWidth - 1 ] = floorTile;
			}
		}

		//top and bottom walls
		for( int cx = 0; cx < width; cx++ ) {
			for( int x = 0; x < chunkWidth; x++ ) {
				wallLayers[ cx ].tiles[ x ] = floorTile;
				wallLayers[ (height - 1) * width + cx ].tiles[ (chunkHeight - 1) * chunkWidth + x ] = floorTile;
			}
		}
	}

	bool CanFit( Vector2 spot, int objWidth ) {
		for( int i = 1; i < objWidth; i++ ) {
			if( !oneAboveGround.Contains( new Vector2( spot.x + i, spot.y ) ) )
				return false;
		}
		return true;
	}

	List<Vector2> AllValidPositions ( FloorObject floorObject ) {
		List<Vector2> result = new List<Vector2>();
		List<Vector2> tilesToCheck;
		if( floorObject.mustBeReachable )
			tilesToCheck = oneAboveGroundSolution;
		else
			tilesToCheck = oneAboveGround;
		foreach( Vector2 spot in tilesToCheck ) {
			if( CanFit( spot, floorObject.width ) && ( floorObject.minDistanceFromStart == 0 || Vector2.SqrMagnitude( spot - startPoint ) > floorObject.minDistanceFromStart * floorObject.minDistanceFromStart ) ) {
				result.Add( spot );
			}
		}
		return result;
	}

	void RemoveFromOneAboveGround( Vector2 spot, int objWidth ) {
		for( int i = 0; i < objWidth; i++ ) {
			oneAboveGround.Remove( new Vector2( spot.x + i, spot.y ) );
			oneAboveGroundSolution.Remove( new Vector2( spot.x + i, spot.y ) );
		}
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
	

	void AddFloorObjects ( bool fromEdditor ) {
		if( floorObjects == null )
			return;

		foreach( FloorObject fo in floorObjects ) {
			if( Random.value < fo.chance ) {
				int amount = Random.Range( fo.minAmount, fo.maxAmount+1 );
				for( int a = 0; a < amount; a++ ) {
					if( oneAboveGroundSolution.Count < 1 && fo.mustBeReachable )
						break;
					if( oneAboveGround.Count < 1 )
						break;
//					Vector2 spot = fo.mustBeReachable ? oneAboveGroundSolution[ Random.Range( 0, oneAboveGroundSolution.Count ) ] : oneAboveGround[ Random.Range( 0, oneAboveGround.Count ) ];
//					for( int r = 0; r < 10; r++ ) {
//						if( CanFit( spot, fo.width ) && ( fo.minDistanceFromStart == 0 || Vector2.SqrMagnitude( spot - startPoint ) > fo.minDistanceFromStart * fo.minDistanceFromStart ) )
//							break;
//						spot = fo.mustBeReachable ? oneAboveGroundSolution[ Random.Range( 0, oneAboveGroundSolution.Count ) ] : oneAboveGround[ Random.Range( 0, oneAboveGround.Count ) ];
//					}
//
//					if( CanFit( spot, fo.width ) && ( fo.minDistanceFromStart == 0 || Vector2.SqrMagnitude( spot - startPoint ) > fo.minDistanceFromStart * fo.minDistanceFromStart ) ) {
//						RemoveFromOneAboveGround( spot, fo.width );
//						floorGameObjects.Add( (GameObject)Instantiate( fo.obj, (Vector3)(spot + (Vector2)mainLayer.transform.position) + fo.offset, Quaternion.identity ) );
//					}

					List<Vector2> allValidPositions = AllValidPositions( fo );
					if( allValidPositions.Count < 1 )
						break;
					Vector2 spot = allValidPositions[Random.Range( 0, allValidPositions.Count )];
					RemoveFromOneAboveGround( spot, fo.width );
					GameObject thisObj = (GameObject)Instantiate( fo.obj, (Vector3)spot + mainLayer.transform.position + fo.offset, Quaternion.identity );
					floorGameObjects.Add( thisObj );
					TileInfo thisTileInfo = thisObj.GetComponent<TileInfo>();
					if( thisTileInfo != null ) {
						if( thisTileInfo.GetComponent<MeshFilter>().sharedMesh == null ) {
							thisTileInfo.mapHasChanged = true;
							thisTileInfo.UpdateVisualMesh( fromEdditor );
						}
					}
				}
			}
		}
	}

	void AddStartAndExit () {
		if( entryObj != null )
			entryObj.transform.position = (Vector3)startPoint + entryObjOffset;
		if( exitObj != null )
			exitObj.transform.position = (Vector3)endPoint + exitObjOffset;
	}

	void Start () {
		if( randomAtGameStart ) {
			GenerateRandomPlatformer();
		}
	}
	
}
