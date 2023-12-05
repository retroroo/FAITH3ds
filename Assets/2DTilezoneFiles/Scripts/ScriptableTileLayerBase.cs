using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;

[AddComponentMenu("2D/ScriptableTileLayer")]
public class ScriptableTileLayerBase : MonoBehaviour, ITilezoneTileDataStream {

	[HideInInspector] public int widthInChunks = 1;
	[HideInInspector] public int heightInChunks = 1;

	[HideInInspector] public int chunkWidth = 16;
	[HideInInspector] public int chunkHeight = 16;

	[HideInInspector] public int seed;


	[SerializeField][HideInInspector] List<GameObject> spawnedObjects;



	Dictionary<int, int> _changeDictionary = new Dictionary<int, int>();

	public Dictionary<int, int> changeDictionary {
		get {
			return _changeDictionary;
		}
	}

	public void AddToChangeList( IEnumerable<KeyValuePair<int, int>> changes ) {
		foreach( KeyValuePair<int, int> change in changes ) {
			_changeDictionary[change.Key] = change.Value;
		}
	}

	#region ITilezoneTileDataStream implementation

	public int width {
		get {
			return widthInChunks * chunkWidth;
		}
	}

	public int height {
		get {
			return heightInChunks * chunkHeight;
		}
	}

	public int numberOfLayers {
		get {
			return scriptableLayers.Length;
		}
	}

	public virtual bool isDataReady {
		get {
			return true;
		}
	}

	public int GetLayerHash( int index ) {
		if( index < 0 || index >= scriptableLayers.Length )
			return 0;
		if( scriptableLayers[index].layers[0] == null )
			return 0;
		return scriptableLayers[index].layers[0].GetHashCode();
	}

	public Tile this [int x, int y, int index] {
		get {
			if( !IsInBounds( x, y ) || index >= scriptableLayers.Length )
				return null;
			int chunkIndex = ( y / chunkHeight ) * widthInChunks + ( x / chunkWidth );
			int tileIndex = ( y % chunkHeight ) * chunkWidth + ( x % chunkWidth );
			if( scriptableLayers[index].layers[chunkIndex] == null )
				return Tile.empty;
			return scriptableLayers[index].layers[ chunkIndex ].tiles[tileIndex];
		}
		set {
			if( !IsInBounds( x, y ) || index >= scriptableLayers.Length )
				return;
			int chunkIndex = ( y / chunkHeight ) * widthInChunks + ( x / chunkWidth );
			int tileIndex = ( y % chunkHeight ) * chunkWidth + ( x % chunkWidth );
			if( scriptableLayers[index].layers[chunkIndex] == null ) {
				scriptableLayers[index].layers[chunkIndex] = CloneLayer( chunkIndex, scriptableLayers[index].layers[0], index );
				ConnectLayers( scriptableLayers[index].layers );
			}
			scriptableLayers[index].layers[ chunkIndex ].tiles[tileIndex] = value;
//			scriptableLayers[index].layers[ chunkIndex ].mapHasChanged = true;
		}
	}

	#endregion

	public Tile this[ Vector2 worldPos, int index ] {
		get {
			if( index >= scriptableLayers.Length )
				return Tile.empty;
			int x = Mathf.FloorToInt( ( worldPos.x - transform.position.x ) / scriptableLayers[index].layers[0].zoomFactor );
			int y = Mathf.FloorToInt( ( worldPos.y - transform.position.y ) / scriptableLayers[index].layers[0].zoomFactor );
			return this[ x, y, index ];
		}
		set {
			if( index >= scriptableLayers.Length )
				return;
			int x = Mathf.FloorToInt( ( worldPos.x - transform.position.x ) / scriptableLayers[index].layers[0].zoomFactor );
			int y = Mathf.FloorToInt( ( worldPos.y - transform.position.y ) / scriptableLayers[index].layers[0].zoomFactor );
			this[ x, y, index ] = value;
		}
	}

	public bool TrySetTile( int x, int y, int index, Tile tile ) {
		if( !IsInBounds( x, y ) || index >= scriptableLayers.Length )
			return false;
		int chunkIndex = ( y / chunkHeight ) * widthInChunks + ( x / chunkWidth );
		int tileIndex = ( y % chunkHeight ) * chunkWidth + ( x % chunkWidth );
		if( scriptableLayers[index].layers[chunkIndex] == null ) {
			scriptableLayers[index].layers[chunkIndex] = CloneLayer( chunkIndex, scriptableLayers[index].layers[0], index );
			ConnectLayers( scriptableLayers[index].layers );
		}
		scriptableLayers[index].layers[ chunkIndex ].tiles[tileIndex] = tile;
		scriptableLayers[index].layers[ chunkIndex ].mapHasChanged = true;
		return true;
	}
	public bool TrySetTile( Vector2 worldPos, int index, Tile tile ) {
		if( index >= scriptableLayers.Length )
			return false;
		int x = Mathf.FloorToInt( ( worldPos.x - transform.position.x ) / scriptableLayers[index].layers[0].zoomFactor );
		int y = Mathf.FloorToInt( ( worldPos.y - transform.position.y ) / scriptableLayers[index].layers[0].zoomFactor );
		return TrySetTile( x, y, index, tile );
	}

	public bool isLayersSetUp {
		get {
			return ( scriptableLayers[0].layers.Length == widthInChunks * heightInChunks && scriptableLayers[0].layers[0].tiles.Length == chunkWidth * chunkHeight );
		}
	}

	/// <summary>
	/// Changes the tile and sets it dirty. Once all tile modifications are done call UpdateDirtyMeshes().
	/// </summary>
	/// <param name="x">The x coordinate.</param>
	/// <param name="y">The y coordinate.</param>
	/// <param name="index">Index.</param>
	/// <param name="tile">Tile.</param>
	/// <param name="addToChangeList">If set to <c>true</c> add to change list. This can be used to save the changes to a file later</param>
	/// <param name="syncData">Optional tile data to also apply the change to.</param>
	public void ChangeTile( int x, int y, int index, Tile tile, bool addToChangeList ) {
		if( TrySetTile( x, y, index, tile ) )
			SetDirty( x, y, index );
		if( this as ITilezoneContinousLoad != null )
			( this as ITilezoneContinousLoad ).dataToLoad[x, y, index ] = tile;
		if( addToChangeList )
			_changeDictionary[ index * width * height + y * width + x ] = tile.Serialize();
	}
	public void ChangeTile( Vector2 worldPos, int index, Tile tile, bool addToChangeList ) {
		if( index >= scriptableLayers.Length )
			return;
		int x = Mathf.FloorToInt( ( worldPos.x - transform.position.x ) / scriptableLayers[index].layers[0].zoomFactor );
		int y = Mathf.FloorToInt( ( worldPos.y - transform.position.y ) / scriptableLayers[index].layers[0].zoomFactor );
		ChangeTile( x, y, index, tile, addToChangeList );
	}

	/// <summary>
	/// Changes the tile and updates the mesh.
	/// </summary>
	/// <param name="x">The x coordinate.</param>
	/// <param name="y">The y coordinate.</param>
	/// <param name="index">Index.</param>
	/// <param name="tile">Tile.</param>
	/// <param name="addToChangeList">If set to <c>true</c> add to change list. This can be used to save the changes to a file later</param>
	/// <param name="syncData">Optional tile data to also apply the change to.</param>
	public void ChangeTileAndUpdate( int x, int y, int index, Tile tile, bool addToChangeList ) {
		if( addToChangeList )
			_changeDictionary[ index * width * height + y * width + x ] = tile.Serialize();
		if( this as ITilezoneContinousLoad != null )
			( this as ITilezoneContinousLoad ).dataToLoad[ x, y, index ] = tile;
		if( TrySetTile( x, y, index, tile ) )
			UpdateMesh( x, y, index );
		

	}
	public void ChangeTileAndUpdate( Vector2 worldPos, int index, Tile tile, bool addToChangeList ) {
		if( index >= scriptableLayers.Length )
			return;
		int x = Mathf.FloorToInt( ( worldPos.x - transform.position.x ) / scriptableLayers[index].layers[0].zoomFactor );
		int y = Mathf.FloorToInt( ( worldPos.y - transform.position.y ) / scriptableLayers[index].layers[0].zoomFactor );
		ChangeTileAndUpdate( x, y, index, tile, addToChangeList );
	}




	[HideInInspector] public ScriptableTileLayer[] scriptableLayers;

	public TileInfo mainLayer {
		get {
			if( scriptableLayers.Length == 0 || scriptableLayers[0].layers == null || scriptableLayers[0].layers.Length == 0 )
				return null;
			return scriptableLayers[0].layers[0];
		}
	}

	T CopyComponent<T>(T original, GameObject destination) where T : Component {
		System.Type type = original.GetType();
		Component copy = destination.AddComponent(type);
		System.Reflection.FieldInfo[] fields = type.GetFields();
		foreach (System.Reflection.FieldInfo field in fields)
		{
			field.SetValue(copy, field.GetValue(original));
		}
		return copy as T;
	}

	TileInfo CloneLayer( int newIndex, TileInfo layerToClone, int scriptableLayer, ITilezoneTileDataStream stream = null ) {

		GameObject result = new GameObject( scriptableLayers[scriptableLayer].name + "_" + newIndex );
		result.transform.parent = mainLayer.transform;
		result.AddComponent<MeshFilter>().sharedMesh = new Mesh();
		result.layer = layerToClone.gameObject.layer;
		result.tag = layerToClone.gameObject.tag;
		MeshRenderer mr = result.AddComponent<MeshRenderer>();
		mr.material = layerToClone.GetComponent<MeshRenderer>().sharedMaterial;
		mr.sortingLayerID = scriptableLayers[scriptableLayer].sortingLayer;
		mr.sortingOrder = scriptableLayers[scriptableLayer].sortingOrder;
		#if UNITY_EDITOR
		if( !UnityEditor.EditorApplication.isPlaying )
		#if UNITY_5_5_OR_NEWER
			UnityEditor.EditorUtility.SetSelectedRenderState( mr, UnityEditor.EditorSelectedRenderState.Hidden );
		#else
			UnityEditor.EditorUtility.SetSelectedWireframeHidden(mr, true);
		#endif
		#endif
		TileInfo ti = result.AddComponent<TileInfo>();

		ti.tileSize = layerToClone.tileSize;
		ti.spacing = layerToClone.spacing;
		ti.mapWidth = chunkWidth;
		ti.mapHeight = chunkHeight;
		ti.tiles = new Tile[chunkWidth * chunkHeight];
		if( newIndex <= 0 ) {
			ti.collisions = (TileInfo.CollisionType[])layerToClone.collisions.Clone();
			ti.autoTileData = new List<Tile>( layerToClone.autoTileData );
			ti.autoTileEdgeMode = new List<TileInfo.AutoTileEdgeMode>( layerToClone.autoTileEdgeMode );
			ti.autoTileLinkMask = new List<int>( layerToClone.autoTileLinkMask );
			ti.autoTileNames = new List<string>( layerToClone.autoTileNames );
			ti.autoTileType = new List<TileInfo.AutoTileType>( layerToClone.autoTileType );
			ti.showAutoTile = new List<bool>( layerToClone.showAutoTile );
		}
		else {
			ti.collisions = layerToClone.collisions;
			ti.autoTileData = layerToClone.autoTileData;
			ti.autoTileEdgeMode = layerToClone.autoTileEdgeMode;
			ti.autoTileLinkMask = layerToClone.autoTileLinkMask;
			ti.autoTileNames = layerToClone.autoTileNames;
			ti.autoTileType = layerToClone.autoTileType;
			ti.showAutoTile = layerToClone.showAutoTile;
		}
		ti.numberOfAutotiles = layerToClone.numberOfAutotiles;
		ti.zoomFactor = layerToClone.zoomFactor;
		ti.transform.localScale = new Vector3( 1, 1, 1 );

		if( stream != null ) {
			ti.SetDataStream( stream, transform.position, scriptableLayer );
		}

		ti.pixelColliders = layerToClone.pixelColliders;
		for( int i = 0; i < ti.tiles.Length; i++ ) {
			ti.tiles[i] = Tile.empty;
		}

		if( layerToClone.GetComponent<MeshCollider>() != null )
			result.AddComponent<MeshCollider>().sharedMesh = new Mesh();
		if( layerToClone.GetComponent<TilePrefabs>() != null ) {
			TilePrefabs tpf = result.AddComponent<TilePrefabs>();
			TilePrefabs tpfc = layerToClone.GetComponent<TilePrefabs>();
			tpf.prefabs = tpfc.prefabs;
			tpf.prefabInfoIndexGrid = tpfc.prefabInfoIndexGrid;
		}

		Vector3 newPos = layerToClone.transform.position;
		if( newIndex >= 0 ) {
			int x = newIndex % widthInChunks;
			int y = newIndex / widthInChunks;
			newPos.x += layerToClone.zoomFactor * chunkWidth * x;
			newPos.y += layerToClone.zoomFactor * chunkHeight * y;
		}
		//else is not the first layer
		else {
			result.transform.parent = layerToClone.transform;
		}

		newPos.z = scriptableLayers[scriptableLayer].worldZ;

		ti.update3DWalls = scriptableLayers[scriptableLayer].update3DWalls;
		ti.update2DColliders = scriptableLayers[scriptableLayer].update2DColliders;
		if( scriptableLayers[scriptableLayer].parallaxDistance != 0 ) {
			ParallaxBackground2 pb = result.AddComponent<ParallaxBackground2>();
			pb._distance = scriptableLayers[scriptableLayer].parallaxDistance;
			pb._offset = (Vector2)newPos - ( (Vector2)transform.position + new Vector2( width, height ) * 0.5f * mainLayer.zoomFactor );
			if( FindObjectOfType<PixelPerfectCamera>() != null )
				pb.pixelSnap = true;
		}
		result.transform.position = newPos;

		MeshCollider mc = result.GetComponent<MeshCollider>();
		if( mc != null )
			mc.sharedMesh = result.GetComponent<MeshFilter>().sharedMesh;

		return ti;
	}





	void ConnectLayers ( TileInfo[] layers ) {
		for( int x = 0; x < widthInChunks; x++ ) {
			for( int y = 0; y < heightInChunks; y++ ) {
				if( layers[y*widthInChunks+x] == null )
					continue;
				int index = (y) * widthInChunks + (x+1);
				if( x < widthInChunks-1 && index >= 0 && index < widthInChunks * heightInChunks && layers[index] != null )
					layers[y*widthInChunks+x].rightLayer = layers[index];
				index = (y) * widthInChunks + (x-1);
				if( x > 0 && index >= 0 && index < widthInChunks * heightInChunks && layers[index] != null )
					layers[y*widthInChunks+x].leftLayer = layers[index];
				index = (y+1) * widthInChunks + (x);
				if( y < heightInChunks-1 && index >= 0 && index < widthInChunks * heightInChunks && layers[index] != null )
					layers[y*widthInChunks+x].upLayer = layers[index];
				index = (y-1) * widthInChunks + (x);
				if( y > 0 && index >= 0 && index < widthInChunks * heightInChunks && layers[index] != null )
					layers[y*widthInChunks+x].downLayer = layers[index];

				index = (y+1) * widthInChunks + (x+1);
				if( y < heightInChunks-1 && x < widthInChunks-1 && index >= 0 && index < widthInChunks * heightInChunks && layers[index] != null )
					layers[y*widthInChunks+x].upRightLayer = layers[index];
				index = (y+1) * widthInChunks + (x-1);
				if( y < heightInChunks-1 && x > 0 && index >= 0 && index < widthInChunks * heightInChunks && layers[index] != null )
					layers[y*widthInChunks+x].upLeftLayer = layers[index];
				index = (y-1) * widthInChunks + (x+1);
				if( y > 0 && x < widthInChunks-1 && x < widthInChunks-1 && index >= 0 && index < widthInChunks * heightInChunks && layers[index] != null )
					layers[y*widthInChunks+x].downRightLayer = layers[index];
				index = (y-1) * widthInChunks + (x-1);
				if( y > 0 && x > 0 && index >= 0 && index < widthInChunks * heightInChunks && layers[index] != null )
					layers[y*widthInChunks+x].downLeftLayer = layers[index];
			}
		}
	}




	public void Generate () {
		DestroyLayers();

		GenerateMethod( this );

//		UpdateMeshes();
	}


	public void SaveChangesAsDataFile( string path, bool writeSeed ) {
		if( !Directory.Exists( Path.GetDirectoryName( path ) ) )
			Directory.CreateDirectory(Path.GetDirectoryName( path ));
		using( BinaryWriter writer = new BinaryWriter( File.Open( path, FileMode.Create ) ) ) {
			writer.Write( writeSeed );
			if( writeSeed )
				writer.Write( seed );
			writer.Write( _changeDictionary.Count );
			foreach( KeyValuePair<int, int> change in _changeDictionary ) {
				writer.Write( change.Key );
				writer.Write( change.Value );
			}
		}
	}

	public void LoadChangesDataFile( string path ) {
		if( !File.Exists( path ) )
			return;
		_changeDictionary = new Dictionary<int, int>();
		using( BinaryReader reader = new BinaryReader( File.Open( path, FileMode.Open ) ) ) {
			if( reader.ReadBoolean() )
				seed = reader.ReadInt32();
			int count = reader.ReadInt32();
			for( int i = 0; i < count; i++ ) {
				_changeDictionary.Add( reader.ReadInt32(), reader.ReadInt32() );
			}
		}
	}

	public void GenerateAsDataFile ( string path ) {
		SerializedTileData saveData = GenerateSerializedTileData();

		FileStream file = File.Create( path );
		BinaryFormatter bf = new BinaryFormatter();
		bf.Serialize( file, saveData );
		file.Close();
	}

	public SerializedTileData GenerateSerializedTileData () {
		SerializedTileData saveData = new SerializedTileData( this );
		GenerateMethod( saveData );
		return saveData;
	}

	public SerializedTileData LoadDataFile ( string path ) {
		if( !File.Exists( path ) )
			return null;
		FileStream file = File.Open( path, FileMode.Open );
		BinaryFormatter bf = new BinaryFormatter();
		SerializedTileData result = (SerializedTileData)bf.Deserialize( file );
		file.Close();
		return result;
	}
	public virtual void Start () {
		if( this is ITilezoneContinousLoad )
			UpdateTilesContinuous();
	}


	#if UNITY_EDITOR
	/// <summary>
	/// EDITOR ONLY Resize the specified chunkXOffset, chunkYOffset, newWidthInChunks and newHeightInChunks.
	/// </summary>
	/// <param name="chunkXOffset">Chunk X offset.</param>
	/// <param name="chunkYOffset">Chunk Y offset.</param>
	/// <param name="newWidthInChunks">New width in chunks.</param>
	/// <param name="newHeightInChunks">New height in chunks.</param>
	public void Resize( int chunkXOffset, int chunkYOffset, int newWidthInChunks, int newHeightInChunks ) {
		if( UnityEditor.EditorApplication.isPlaying ) {
			Debug.LogError( "Resize is for use in editor only. Game will not compile if you reference this method outside of an editor script" );
			return;
		}
		Vector2 newBottomLeft = transform.position + new Vector3( chunkXOffset * chunkWidth, chunkYOffset * chunkHeight ) * mainLayer.zoomFactor;
		List<TileInfo>[] oldLayers = new List<TileInfo>[scriptableLayers.Length];
		bool mainLayerInBounds = false;
		TileInfo layerInStartPos = null;
		bool mainLayerInPos = false;
		for( int sl = 0; sl < scriptableLayers.Length; sl++ ) {
			oldLayers[sl] = new List<TileInfo>();
			if( sl == 0 )
				oldLayers[sl].Add( scriptableLayers[sl].layers[0] );
			for( int l = 0; l < scriptableLayers[sl].layers.Length; l++ ) {
				if( scriptableLayers[sl].layers[l] == null )
					continue;
				int newXIndex = Mathf.FloorToInt( ( scriptableLayers[sl].layers[l].transform.position.x + 1 - newBottomLeft.x ) / ( scriptableLayers[sl].layers[l].zoomFactor * chunkWidth ) );
				int newYIndex = Mathf.FloorToInt( ( scriptableLayers[sl].layers[l].transform.position.y + 1 - newBottomLeft.y ) / ( scriptableLayers[sl].layers[l].zoomFactor * chunkHeight ) );
				if( newXIndex >= 0 && newYIndex >= 0 && newXIndex < newWidthInChunks && newYIndex < newHeightInChunks ) {
					if( sl == 0 && l == 0 ) {
						mainLayerInBounds = true;
						if( newXIndex == 0 && newYIndex == 0 )
							mainLayerInPos = true;
						continue;
					}
					if( newXIndex == 0 && newYIndex == 0 && sl == 0 )
						layerInStartPos = scriptableLayers[sl].layers[l];
					oldLayers[sl].Add( scriptableLayers[sl].layers[l] );
					scriptableLayers[sl].layers[l].transform.parent = null;
				}
				else if( !( sl == 0 && l == 0 ) ) {
					UnityEditor.Undo.DestroyObjectImmediate( scriptableLayers[sl].layers[l].gameObject );
				}
			}
		}
		if( !mainLayerInPos ) {
			
			if( mainLayerInBounds ) {
				if( layerInStartPos != null ) {
					//swap positions and tile contents
					layerInStartPos.transform.position = transform.position;
					Tile[] tempTiles = mainLayer.tiles;
					mainLayer.tiles = layerInStartPos.tiles;
					layerInStartPos.tiles = tempTiles;
					layerInStartPos.mapHasChanged = true;
					layerInStartPos.UpdateVisualMesh( true );
				}
				else {
					//clone main layer and copy tile contents to clone then clear main layer
					oldLayers[0].Add( CloneLayer( 0, mainLayer, 0 ) );
					oldLayers[0][oldLayers[0].Count-1].transform.parent = null;
					oldLayers[0][oldLayers[0].Count-1].tiles = mainLayer.tiles;
					mainLayer.tiles = new Tile[chunkWidth*chunkHeight];
					for( int i = 0; i < mainLayer.tiles.Length; i++ ) {
						mainLayer.tiles[i] = Tile.empty;
					}
					oldLayers[0][oldLayers[0].Count-1].mapHasChanged = true;
					oldLayers[0][oldLayers[0].Count-1].UpdateVisualMesh( true );
				}
			}
			else {
				if( layerInStartPos != null ) {
					mainLayer.tiles = layerInStartPos.tiles;
					UnityEditor.Undo.DestroyObjectImmediate( layerInStartPos.gameObject );
				}
				else {
					for( int i = 0; i < mainLayer.tiles.Length; i++ ) {
						mainLayer.tiles[i] = Tile.empty;
					}
				}
			}

			transform.position = newBottomLeft;
			mainLayer.mapHasChanged = true;
			mainLayer.UpdateVisualMesh( true );
		}

		widthInChunks = newWidthInChunks;
		heightInChunks = newHeightInChunks;
		for( int sl = 0; sl < scriptableLayers.Length; sl++ ) {
			scriptableLayers[sl].layers = new TileInfo[newWidthInChunks * newHeightInChunks];
			foreach( TileInfo layer in oldLayers[sl] ) {
				if( layer == null )
					continue;
				int newXIndex = Mathf.FloorToInt( ( layer.transform.position.x + 1 - newBottomLeft.x ) / ( layer.zoomFactor * chunkWidth ) );
				int newYIndex = Mathf.FloorToInt( ( layer.transform.position.y + 1 - newBottomLeft.y ) / ( layer.zoomFactor * chunkHeight ) );
				layer.name = scriptableLayers[sl].name +"_" + ( newYIndex * newWidthInChunks + newXIndex );
				scriptableLayers[sl].layers[ newYIndex * newWidthInChunks + newXIndex ] = layer;
				if( !( newXIndex == 0 && newYIndex == 0 ) )
					layer.transform.SetParent( mainLayer.transform, true );
			}
			ConnectLayers( scriptableLayers[sl].layers );
		}
	}
		
	/// <summary>
	/// EDITOR ONLY Resize the specified newChunkWidth and newChunkHeight.
	/// </summary>
	/// <param name="newChunkWidth">New chunk width.</param>
	/// <param name="newChunkHeight">New chunk height.</param>
	public void Resize( int newChunkWidth, int newChunkHeight ) {
		if( UnityEditor.EditorApplication.isPlaying ) {
			Debug.LogError( "Resize is for use in editor only. Game will not compile if you reference this method outside of an editor script" );
			return;
		}
		List<int>[] xIndices = new List<int>[scriptableLayers.Length];
		List<int>[] yIndices = new List<int>[scriptableLayers.Length];
		List<Tile>[] tiles = new List<Tile>[scriptableLayers.Length];

		for( int sl = 0; sl < scriptableLayers.Length; sl++ ) {
			xIndices[sl] = new List<int>();
			yIndices[sl] = new List<int>();
			tiles[sl] = new List<Tile>();
			for( int x = 0; x < width && x < widthInChunks * newChunkWidth; x++ ) {
				for( int y = 0; y < height && y < heightInChunks * newChunkHeight; y++ ) {
					if( scriptableLayers[sl].layers[LocalPosToLayerIndex(x,y)] == null ) {
						y += chunkWidth;
						continue;
					}
					if( this[x,y,sl] == Tile.empty )
						continue;
					xIndices[sl].Add( x );
					yIndices[sl].Add( y );
					tiles[sl].Add( this[x,y,sl] );
				}
			}
		}
		chunkWidth = newChunkWidth;
		chunkHeight = newChunkHeight;
		ClearLayers();
		for( int sl = 0; sl < scriptableLayers.Length; sl++ ) {
			for( int i = 0; i < xIndices[sl].Count; i++ ) {
				this[ xIndices[sl][i], yIndices[sl][i], sl ] = tiles[sl][i];
			}
		}
		UpdateMeshes();
	}

	/// <summary>
	/// Updates the tiles continuously based on the distance from players. This method get called at game start if inherited class implements ITilezoneContinousLoad
	/// </summary>
	/// <returns>The tiles continuous.</returns>
	/// <param name="players">Players.</param>
	/// <param name="bottomLeft">Bottom left corner in world coordinates.</param>
	/// <param name="viewDist">View dist in chunks.</param>
	/// <param name="tileData">Tile data.</param>
	/// <param name="tileChanges">Optional dictionary containing tile changes.</param>
	/// <param name="deltaTime">Time in seconds between update checks.</param>
	public Coroutine UpdateTilesContinuous () {
		if( UnityEditor.EditorApplication.isPlaying && this is ITilezoneContinousLoad )
			return StartCoroutine( UpdateTilesContinuousCoroutine( this as ITilezoneContinousLoad ) );
		return null;
	}
	public void SpawnObject( GameObject obj, Vector3 position ) {
		GameObject spawnedObj = (GameObject)Instantiate( obj, position, obj.transform.rotation );
		spawnedObjects.Add( spawnedObj );
		TileInfo thisTileInfo = spawnedObj.GetComponent<TileInfo>();
		if( thisTileInfo != null ) {
			if( thisTileInfo.GetComponent<MeshFilter>().sharedMesh == null ) {
				thisTileInfo.mapHasChanged = true;
				if( UnityEditor.EditorApplication.isPlaying )
					thisTileInfo.UpdateVisualMesh( false );
				else
					thisTileInfo.UpdateVisualMesh( true );
			}
		}
	}

	public void RemoveEmptyChunks () {
		for( int sl = 0; sl < scriptableLayers.Length; sl++ ) {
			for( int i = 1; i < scriptableLayers[sl].layers.Length; i++ ) {
				if( scriptableLayers[sl].layers[i] == null )
					continue;
				bool isEmpty = true;
				for( int t = 0; t < scriptableLayers[sl].layers[i].tiles.Length; t++ ) {
					if( scriptableLayers[sl].layers[i].tiles[t] != Tile.empty ) {
						isEmpty = false;
						break;
					}
				}
				if( isEmpty ) {
					if( !UnityEditor.EditorApplication.isPlaying )
						DestroyImmediate( scriptableLayers[sl].layers[i].gameObject );
					else
						Destroy( scriptableLayers[sl].layers[i].gameObject );
				}
			}
		}
	}
	public void UpdateMeshes () {
		foreach( ScriptableTileLayer scriptableLayer in scriptableLayers ) {
			foreach( TileInfo ti in scriptableLayer.layers ) {
				if( ti == null )
					continue;
				ti.mapHasChanged = true;
				if( UnityEditor.EditorApplication.isPlaying )
					ti.UpdateVisualMesh( false );
				else
					ti.UpdateVisualMesh( true );
			}
		}
	}
	public void UpdateDirtyMeshes () {
		for( int sl = 0; sl < scriptableLayers.Length; sl++ ) {
			foreach( int i in scriptableLayers[sl].dirtyLayers ) {
				if( scriptableLayers[sl].layers.Length <= i || scriptableLayers[sl].layers[i] == null )
					continue;
				scriptableLayers[sl].layers[i].mapHasChanged = true;
				scriptableLayers[sl].layers[i].UpdateVisualMesh( !UnityEditor.EditorApplication.isPlaying );
			}
			scriptableLayers[sl].dirtyLayers.Clear();
		}
	}

	void UpdateMesh( int x, int y, int index ) {
		if( !IsInBounds(x,y) || index >= scriptableLayers.Length || index < 0 )
			return;
		scriptableLayers[index].layers[ LocalPosToLayerIndex( x, y ) ].mapHasChanged = true;
		scriptableLayers[index].layers[ LocalPosToLayerIndex( x, y ) ].UpdateVisualMesh( !UnityEditor.EditorApplication.isPlaying );
		int xMod = x % chunkWidth;
		int yMod = y % chunkHeight;
		if( xMod == 0 && IsInBounds( x-1, y ) && scriptableLayers[index].layers[ LocalPosToLayerIndex( x-1, y ) ] != null ) {
			scriptableLayers[index].layers[ LocalPosToLayerIndex( x-1, y ) ].mapHasChanged = true;
			scriptableLayers[index].layers[ LocalPosToLayerIndex( x-1, y ) ].UpdateVisualMesh( !UnityEditor.EditorApplication.isPlaying );
		}
		if( xMod == chunkWidth - 1 && IsInBounds( x+1, y ) && scriptableLayers[index].layers[ LocalPosToLayerIndex( x+1, y ) ] != null ) {
			scriptableLayers[index].layers[ LocalPosToLayerIndex( x+1, y ) ].mapHasChanged = true;
			scriptableLayers[index].layers[ LocalPosToLayerIndex( x+1, y ) ].UpdateVisualMesh( !UnityEditor.EditorApplication.isPlaying );
		}
		if( yMod == 0 && IsInBounds( x, y-1 ) && scriptableLayers[index].layers[ LocalPosToLayerIndex( x, y-1 ) ] != null ) {
			scriptableLayers[index].layers[ LocalPosToLayerIndex( x, y-1 ) ].mapHasChanged = true;
			scriptableLayers[index].layers[ LocalPosToLayerIndex( x, y-1 ) ].UpdateVisualMesh( !UnityEditor.EditorApplication.isPlaying );
		}
		if( yMod == chunkWidth - 1 && IsInBounds( x, y+1 ) && scriptableLayers[index].layers[ LocalPosToLayerIndex( x, y+1 ) ] != null ) {
			scriptableLayers[index].layers[ LocalPosToLayerIndex( x, y+1 ) ].mapHasChanged = true;
			scriptableLayers[index].layers[ LocalPosToLayerIndex( x, y+1 ) ].UpdateVisualMesh( !UnityEditor.EditorApplication.isPlaying );
		}
	}
//	void UpdateMeshes( int x, int y, int width, int height, int index ) {
//		if( !IsInBounds(x,y) || !IsInBounds(x+width-1,y+height-1) || index >= scriptableLayers.Length || index < 0 )
//			return;
//		int endXIndex = (x+width) / chunkWidth;
//		int endYIndex = (y+height) / chunkHeight;
//		int xStartMod = x % chunkWidth;
//		int xEndMod = (x+width) % chunkWidth;
//		int yStartMod = y % chunkHeight;
//		int yEndMod = (y+height) % chunkHeight;
//		for( int xx = x / chunkWidth; xx <= endXIndex; xx++ ) {
//			for( int yy = y / chunkHeight; yy <= endYIndex; yy++ ) {
//				scriptableLayers[index].layers[ yy * widthInChunks + xx ].mapHasChanged = true;
//				scriptableLayers[index].layers[ yy * widthInChunks + xx ].UpdateVisualMesh( !UnityEditor.EditorApplication.isPlaying );
//				if( scriptableLayers[index].updateColliders )
//					scriptableLayers[index].layers[ yy * widthInChunks + xx ].UpdateColliders();
//			}
//			if( yStartMod == 0 ) {
//				scriptableLayers[index].layers[ ((y / chunkHeight)-1) * widthInChunks + xx ].mapHasChanged = true;
//				scriptableLayers[index].layers[ ((y / chunkHeight)-1) * widthInChunks + xx ].UpdateVisualMesh( !UnityEditor.EditorApplication.isPlaying );
//				if( scriptableLayers[index].updateColliders )
//					scriptableLayers[index].layers[ ((y / chunkHeight)-1) * widthInChunks + xx ].UpdateColliders();
//			}
//			if( yEndMod == chunkHeight - 1 ) {
//				scriptableLayers[index].layers[ (endYIndex+1) * widthInChunks + xx ].mapHasChanged = true;
//				scriptableLayers[index].layers[ (endYIndex+1) * widthInChunks + xx ].UpdateVisualMesh( !UnityEditor.EditorApplication.isPlaying );
//				if( scriptableLayers[index].updateColliders )
//					scriptableLayers[index].layers[ (endYIndex+1) * widthInChunks + xx ].UpdateColliders();
//			}
//		}
//		if( xStartMod == 0 || xEndMod == chunkWidth-1 ) {
//			for( int yy = y / chunkHeight; yy <= endYIndex; yy++ ) {
//				if( xStartMod == 0 ) {
//					scriptableLayers[index].layers[ yy * widthInChunks + ((x / chunkWidth)-1) ].mapHasChanged = true;
//					scriptableLayers[index].layers[ yy * widthInChunks + ((x / chunkWidth)-1) ].UpdateVisualMesh( !UnityEditor.EditorApplication.isPlaying );
//					if( scriptableLayers[index].updateColliders )
//						scriptableLayers[index].layers[ yy * widthInChunks + ((x / chunkWidth)-1) ].UpdateColliders();
//				}
//				else {
//					scriptableLayers[index].layers[ yy * widthInChunks + (endXIndex+1) ].mapHasChanged = true;
//					scriptableLayers[index].layers[ yy * widthInChunks + (endXIndex+1) ].UpdateVisualMesh( !UnityEditor.EditorApplication.isPlaying );
//					if( scriptableLayers[index].updateColliders )
//						scriptableLayers[index].layers[ yy * widthInChunks + (endXIndex+1) ].UpdateColliders();
//				}
//			}
//		}
//	}

	void DestroySpawnedObjects () {
		if( spawnedObjects != null ) {
			foreach( GameObject obj in spawnedObjects ) {
				if( obj == null )
					continue;
				if( !UnityEditor.EditorApplication.isPlaying )
					DestroyImmediate( obj );
				else
					Destroy( obj );
			}
			spawnedObjects.Clear();
		}
	}
	void ClearLayers() {
		int newLength = widthInChunks * heightInChunks;
		for( int sl = 0; sl < scriptableLayers.Length; sl++ ) {
			if( scriptableLayers[sl].layers == null )
				continue;
			for( int i = 0; i < scriptableLayers[sl].layers.Length; i++ ) {
				if( scriptableLayers[sl].layers[i] == null )
					continue;
				if( i >= newLength ) {
					if( !UnityEditor.EditorApplication.isPlaying )
						DestroyImmediate( scriptableLayers[sl].layers[i].gameObject );
					else
						Destroy( scriptableLayers[sl].layers[i].gameObject );
					continue;
				}
				int x = i % widthInChunks;
				int y = i / widthInChunks;
				scriptableLayers[sl].layers[i].transform.position = transform.position + new Vector3( scriptableLayers[sl].layers[i].zoomFactor * x * chunkWidth, scriptableLayers[sl].layers[i].zoomFactor * y * chunkHeight );
				scriptableLayers[sl].layers[i].tiles = new Tile[chunkWidth*chunkHeight];
				scriptableLayers[sl].layers[i].mapWidth = chunkWidth;
				scriptableLayers[sl].layers[i].mapHeight = chunkHeight;
				for( int t = 0; t < scriptableLayers[sl].layers[i].tiles.Length; t++ ) {
					scriptableLayers[sl].layers[i].tiles[t] = Tile.empty;
				}
			}
		}
		DestroySpawnedObjects();
	}

	public void DestroyLayers () {
		for( int sl = 0; sl < scriptableLayers.Length; sl++ ) {
			if( scriptableLayers[sl].layers == null || scriptableLayers[sl].layers.Length == 0 ) {
				scriptableLayers[sl].layers = new TileInfo[widthInChunks * heightInChunks];
			}
			if( scriptableLayers[sl].layers[0] == null )
				scriptableLayers[sl].layers[0] = CloneLayer( 0, mainLayer, sl );

			scriptableLayers[sl].layers[0].tiles = new Tile[chunkWidth*chunkHeight];
			scriptableLayers[sl].layers[0].mapWidth = chunkWidth;
			scriptableLayers[sl].layers[0].mapHeight = chunkHeight;
			for( int t = 0; t < scriptableLayers[sl].layers[0].tiles.Length; t++ ) {
				scriptableLayers[sl].layers[0].tiles[t] = Tile.empty;
			}
			for( int i = 1; i < scriptableLayers[sl].layers.Length; i++ ) {
				if( scriptableLayers[sl].layers[i] == null )
					continue;
				if( !UnityEditor.EditorApplication.isPlaying )
					DestroyImmediate( scriptableLayers[sl].layers[i].gameObject );
				else
					Destroy( scriptableLayers[sl].layers[i].gameObject );
			}
			TileInfo tempLayer = scriptableLayers[sl].layers[0];
			scriptableLayers[sl].layers = new TileInfo[widthInChunks * heightInChunks];
			scriptableLayers[sl].layers[0] = tempLayer;
		}
	}
	#else
	public Coroutine UpdateTilesContinuous () {
		if( this is ITilezoneContinousLoad )
			return StartCoroutine( UpdateTilesContinuousCoroutine( this as ITilezoneContinousLoad ) );
		return null;
	}
	public void SpawnObject( GameObject obj, Vector3 position ) {
	GameObject spawnedObj = (GameObject)Instantiate( obj, position, obj.transform.rotation );
	spawnedObjects.Add( spawnedObj );
	TileInfo thisTileInfo = spawnedObj.GetComponent<TileInfo>();
	if( thisTileInfo != null ) {
	if( thisTileInfo.GetComponent<MeshFilter>().sharedMesh == null ) {
	thisTileInfo.mapHasChanged = true;
	thisTileInfo.UpdateVisualMesh( false );
	}
	}
	}
	public void RemoveEmptyChunks () {
		for( int sl = 0; sl < scriptableLayers.Length; sl++ ) {
			for( int i = 1; i < scriptableLayers[sl].layers.Length; i++ ) {
				if( scriptableLayers[sl].layers[i] == null )
					continue;
				bool isEmpty = true;
				for( int t = 0; t < scriptableLayers[sl].layers[i].tiles.Length; t++ ) {
					if( scriptableLayers[sl].layers[i].tiles[t] != Tile.empty ) {
						isEmpty = false;
						break;
					}
				}
				if( isEmpty ) {
					Destroy( scriptableLayers[sl].layers[i].gameObject );
				}
			}
		}
	}
	public void UpdateMeshes () {
		foreach( ScriptableTileLayer scriptableLayer in scriptableLayers ) {
			foreach( TileInfo ti in scriptableLayer.layers ) {
				if( ti == null )
					continue;
				ti.mapHasChanged = true;
				ti.UpdateVisualMesh( false );
			}
		}
	}

	public void UpdateDirtyMeshes () {
		for( int sl = 0; sl < scriptableLayers.Length; sl++ ) {
			foreach( int i in scriptableLayers[sl].dirtyLayers ) {
			if( scriptableLayers[sl].layers.Length <= i || scriptableLayers[sl].layers[i] == null )
					continue;
				scriptableLayers[sl].layers[i].mapHasChanged = true;
				scriptableLayers[sl].layers[i].UpdateVisualMesh( false );
			}
			scriptableLayers[sl].dirtyLayers.Clear();
		}
	}

	void UpdateMesh( int x, int y, int index ) {
		if( !IsInBounds(x,y) || index >= scriptableLayers.Length || index < 0 )
			return;
		scriptableLayers[index].layers[ LocalPosToLayerIndex( x, y ) ].mapHasChanged = true;
		scriptableLayers[index].layers[ LocalPosToLayerIndex( x, y ) ].UpdateVisualMesh( false );
		int xMod = x % chunkWidth;
		int yMod = y % chunkHeight;
		if( xMod == 0 && IsInBounds( x-1, y ) && scriptableLayers[index].layers[ LocalPosToLayerIndex( x-1, y ) ] != null ) {
			scriptableLayers[index].layers[ LocalPosToLayerIndex( x-1, y ) ].mapHasChanged = true;
			scriptableLayers[index].layers[ LocalPosToLayerIndex( x-1, y ) ].UpdateVisualMesh( false );
		}
		if( xMod == chunkWidth - 1 && IsInBounds( x+1, y ) && scriptableLayers[index].layers[ LocalPosToLayerIndex( x+1, y ) ] != null ) {
			scriptableLayers[index].layers[ LocalPosToLayerIndex( x+1, y ) ].mapHasChanged = true;
			scriptableLayers[index].layers[ LocalPosToLayerIndex( x+1, y ) ].UpdateVisualMesh( false );
		}
		if( yMod == 0 && IsInBounds( x, y-1 ) && scriptableLayers[index].layers[ LocalPosToLayerIndex( x, y-1 ) ] != null ) {
			scriptableLayers[index].layers[ LocalPosToLayerIndex( x, y-1 ) ].mapHasChanged = true;
			scriptableLayers[index].layers[ LocalPosToLayerIndex( x, y-1 ) ].UpdateVisualMesh( false );
		}
		if( yMod == chunkWidth - 1 && IsInBounds( x, y+1 ) && scriptableLayers[index].layers[ LocalPosToLayerIndex( x, y+1 ) ] != null ) {
			scriptableLayers[index].layers[ LocalPosToLayerIndex( x, y+1 ) ].mapHasChanged = true;
			scriptableLayers[index].layers[ LocalPosToLayerIndex( x, y+1 ) ].UpdateVisualMesh( false );
		}
	}
//	void UpdateMeshes( int x, int y, int width, int height, int index ) {
//		if( !IsInBounds(x,y) || !IsInBounds(x+width-1,y+height-1) || index >= scriptableLayers.Length || index < 0 )
//			return;
//		int endXIndex = (x+width) / chunkWidth;
//		int endYIndex = (y+height) / chunkHeight;
//		int xStartMod = x % chunkWidth;
//		int xEndMod = (x+width) % chunkWidth;
//		int yStartMod = y % chunkHeight;
//		int yEndMod = (y+height) % chunkHeight;
//		for( int xx = x / chunkWidth; xx <= endXIndex; xx++ ) {
//			for( int yy = y / chunkHeight; yy <= endYIndex; yy++ ) {
//				scriptableLayers[index].layers[ yy * widthInChunks + xx ].mapHasChanged = true;
//				scriptableLayers[index].layers[ yy * widthInChunks + xx ].UpdateVisualMesh( false );
//				if( scriptableLayers[index].updateColliders )
//					scriptableLayers[index].layers[ yy * widthInChunks + xx ].UpdateColliders();
//			}
//			if( yStartMod == 0 ) {
//				scriptableLayers[index].layers[ ((y / chunkHeight)-1) * widthInChunks + xx ].mapHasChanged = true;
//				scriptableLayers[index].layers[ ((y / chunkHeight)-1) * widthInChunks + xx ].UpdateVisualMesh( false );
//				if( scriptableLayers[index].updateColliders )
//					scriptableLayers[index].layers[ ((y / chunkHeight)-1) * widthInChunks + xx ].UpdateColliders();
//			}
//			if( yEndMod == chunkHeight - 1 ) {
//				scriptableLayers[index].layers[ (endYIndex+1) * widthInChunks + xx ].mapHasChanged = true;
//				scriptableLayers[index].layers[ (endYIndex+1) * widthInChunks + xx ].UpdateVisualMesh( false );
//				if( scriptableLayers[index].updateColliders )
//					scriptableLayers[index].layers[ (endYIndex+1) * widthInChunks + xx ].UpdateColliders();
//			}
//		}
//		if( xStartMod == 0 || xEndMod == chunkWidth-1 ) {
//			for( int yy = y / chunkHeight; yy <= endYIndex; yy++ ) {
//				if( xStartMod == 0 ) {
//					scriptableLayers[index].layers[ yy * widthInChunks + ((x / chunkWidth)-1) ].mapHasChanged = true;
//					scriptableLayers[index].layers[ yy * widthInChunks + ((x / chunkWidth)-1) ].UpdateVisualMesh( false );
//					if( scriptableLayers[index].updateColliders )
//						scriptableLayers[index].layers[ yy * widthInChunks + ((x / chunkWidth)-1) ].UpdateColliders();
//				}
//				else {
//					scriptableLayers[index].layers[ yy * widthInChunks + (endXIndex+1) ].mapHasChanged = true;
//					scriptableLayers[index].layers[ yy * widthInChunks + (endXIndex+1) ].UpdateVisualMesh( false );
//					if( scriptableLayers[index].updateColliders )
//						scriptableLayers[index].layers[ yy * widthInChunks + (endXIndex+1) ].UpdateColliders();
//				}
//			}
//		}
//	}

	void DestroySpawnedObjects () {
	if( spawnedObjects != null ) {
	foreach( GameObject obj in spawnedObjects ) {
	if( obj == null )
	continue;
	Destroy( obj );
	}
	spawnedObjects.Clear();
	}
	}

	void ClearLayers() {
	int newLength = widthInChunks * heightInChunks;
	for( int sl = 0; sl < scriptableLayers.Length; sl++ ) {
	if( scriptableLayers[sl].layers == null )
	continue;
	for( int i = 0; i < scriptableLayers[sl].layers.Length; i++ ) {
	if( scriptableLayers[sl].layers[i] == null )
	continue;
	if( i >= newLength ) {
	Destroy( scriptableLayers[sl].layers[i].gameObject );
	continue;
	}
	int x = i % widthInChunks;
	int y = i / widthInChunks;
	scriptableLayers[sl].layers[i].transform.position = transform.position + new Vector3( scriptableLayers[sl].layers[i].zoomFactor * x * chunkWidth, scriptableLayers[sl].layers[i].zoomFactor * y * chunkHeight );
	scriptableLayers[sl].layers[i].tiles = new Tile[chunkWidth*chunkHeight];
	scriptableLayers[sl].layers[i].mapWidth = chunkWidth;
	scriptableLayers[sl].layers[i].mapHeight = chunkHeight;
	for( int t = 0; t < scriptableLayers[sl].layers[i].tiles.Length; t++ ) {
	scriptableLayers[sl].layers[i].tiles[t] = Tile.empty;
	}
	}
	}
	DestroySpawnedObjects();
	}

	public void DestroyLayers () {
		for( int sl = 0; sl < scriptableLayers.Length; sl++ ) {
			if( scriptableLayers[sl].layers == null || scriptableLayers[sl].layers.Length == 0 ) {
				scriptableLayers[sl].layers = new TileInfo[widthInChunks * heightInChunks];
			}
			if( scriptableLayers[sl].layers[0] == null )
				scriptableLayers[sl].layers[0] = CloneLayer( 0, mainLayer, sl );

			scriptableLayers[sl].layers[0].tiles = new Tile[chunkWidth*chunkHeight];
			scriptableLayers[sl].layers[0].mapWidth = chunkWidth;
			scriptableLayers[sl].layers[0].mapHeight = chunkHeight;
			for( int t = 0; t < scriptableLayers[sl].layers[0].tiles.Length; t++ ) {
				scriptableLayers[sl].layers[0].tiles[t] = Tile.empty;
			}
			for( int i = 1; i < scriptableLayers[sl].layers.Length; i++ ) {
				if( scriptableLayers[sl].layers[i] == null )
					continue;
				Destroy( scriptableLayers[sl].layers[i].gameObject );
			}
			TileInfo tempLayer = scriptableLayers[sl].layers[0];
			scriptableLayers[sl].layers = new TileInfo[widthInChunks * heightInChunks];
			scriptableLayers[sl].layers[0] = tempLayer;
		}
	}
	#endif


	IEnumerator UpdateTilesContinuousCoroutine ( ITilezoneContinousLoad loadData ) {
		widthInChunks = loadData.dataToLoad.width / chunkWidth;
		heightInChunks = loadData.dataToLoad.height / chunkWidth;
		mainLayer.SetDataStream( loadData.dataToLoad, transform.position, 0 );

		int viewDist = loadData.viewDistInChunks;

		DestroyLayers();
		for( int sl = 0; sl < scriptableLayers.Length; sl++ ) {
//			TileInfo[] tempLayers = scriptableLayers[sl].layers;
//			scriptableLayers[sl].layers = new TileInfo[ widthInChunks * heightInChunks ];
//			if( tempLayers != null && tempLayers.Length > 0 ) {
//				for( int i = 0; i < tempLayers.Length; i++ ) {
//					scriptableLayers[sl].layers[i] = tempLayers[i];
//					tempLayers[i].UpdateVisualMesh( false );
//					if( scriptableLayers[sl].updateColliders )
//						tempLayers[i].UpdateColliders();
//				}
//			}
			if( scriptableLayers[sl].layers[0] == null ) {
				scriptableLayers[sl].layers[0] = CloneLayer( 0, mainLayer, sl, loadData.dataToLoad );
			}
			else {
				for( int x = 0; x < chunkWidth; x++ ) {
					for( int y = 0; y < chunkHeight; y++ ) {
						scriptableLayers[sl].layers[0].tiles[ y * chunkWidth + x ] = loadData.dataToLoad[ x, y, sl ];
					}
				}
				scriptableLayers[sl].layers[0].mapHasChanged = true;
				scriptableLayers[sl].layers[0].UpdateVisualMesh( false );
			}
		}
		while( true ) {
			if( loadData.players == null ) {
				yield return new WaitForSeconds( 0.2f );
				continue;
			}
				
			foreach( Transform t in loadData.players ) {
				for( int x = -viewDist; x <= viewDist; x++ ) {
					for( int y = -viewDist; y <= viewDist; y++ ) {

						int layerIndex = WorldPosToLayerIndex( (Vector2)t.position + new Vector2( x, y ) * mainLayer.zoomFactor * chunkWidth );
						if( layerIndex < 0 || layerIndex >= scriptableLayers[0].layers.Length )
							continue;
						if( scriptableLayers[0].layers[layerIndex] != null )
							continue;
						
						int xIndex = ( layerIndex % widthInChunks ) * chunkWidth;
						int yYndex = ( layerIndex / widthInChunks ) * chunkHeight;
						for( int sl = 0; sl < scriptableLayers.Length; sl++ ) {
							scriptableLayers[sl].layers[layerIndex] = CloneLayer( layerIndex, scriptableLayers[sl].layers[0], sl, loadData.dataToLoad );
							for( int xx = 0; xx < chunkWidth; xx++ ) {
								for( int yy = 0; yy < chunkHeight; yy++ ) {
//									if( loadData.dataChanges == null || !loadData.dataChanges.TryGetChangeAt( xIndex + xx, yYndex + yy, sl, out scriptableLayers[sl].layers[layerIndex].tiles[yy*chunkWidth+xx] ) )
									scriptableLayers[sl].layers[layerIndex].tiles[yy*chunkWidth+xx] = loadData.dataToLoad[ xIndex+xx, yYndex+yy, sl ];
								}
							}
							scriptableLayers[sl].layers[layerIndex].mapHasChanged = true;
							scriptableLayers[sl].layers[layerIndex].UpdateVisualMesh(false);
						}

					}
				}
			}
			yield return new WaitForSeconds( 0.2f );
		}
	}


	public int WorldPosToLayerIndex ( Vector2 worldPos ) {
		return Mathf.FloorToInt( ( worldPos.y - transform.position.y ) / ( mainLayer.zoomFactor * chunkHeight ) ) * widthInChunks + Mathf.FloorToInt( ( worldPos.x - transform.position.x ) / ( mainLayer.zoomFactor * chunkWidth ) );
	}

	public int LocalPosToLayerIndex ( int x, int y ) {
		return ( y / chunkHeight ) * widthInChunks + ( x / chunkWidth );
	}

	public void WorldPosToLocalPos ( Vector2 worldPos, out int x, out int y ) {
		x = Mathf.FloorToInt( ( worldPos.x - transform.position.x ) / mainLayer.zoomFactor );
		y = Mathf.FloorToInt( ( worldPos.y - transform.position.y ) / mainLayer.zoomFactor );
	}

	/// <summary>
	/// Converts local coordinates to world position of the tile center.
	/// </summary>
	/// <param name="x">The x coordinate.</param>
	/// <param name="y">The y coordinate.</param>
	public Vector2 LocalPosToWorldPosCenter ( int x, int y ) {
		return ( new Vector2( 0.5f + x, 0.5f + y ) * mainLayer.zoomFactor + (Vector2)transform.position );
	}

	/// <summary>
	/// Override this method to add a custom generate method
	/// </summary>
	/// <param name="dataStream">Data stream.</param>
	public virtual void GenerateMethod ( ITilezoneTileDataStream dataStream ) {}

	public void SetDirty( Vector2 worldPos, int index ) {
		SetDirty( Mathf.FloorToInt( ( worldPos.x - transform.position.x ) / mainLayer.zoomFactor ), Mathf.FloorToInt( ( worldPos.y - transform.position.y ) / mainLayer.zoomFactor ), index );
	}

	public void SetDirty( int x, int y, int index ) {
		if( !IsInBounds(x,y) || index >= scriptableLayers.Length || index < 0 )
			return;
		scriptableLayers[index].dirtyLayers.Add( LocalPosToLayerIndex( x, y ) );
		int xMod = x % chunkWidth;
		int yMod = y % chunkHeight;
		if( xMod == 0 )
			scriptableLayers[index].dirtyLayers.Add( LocalPosToLayerIndex( x-1, y ) );
		if( xMod == chunkWidth - 1 )
			scriptableLayers[index].dirtyLayers.Add( LocalPosToLayerIndex( x+1, y ) );
		if( yMod == 0 )
			scriptableLayers[index].dirtyLayers.Add( LocalPosToLayerIndex( x, y-1 ) );
		if( yMod == chunkHeight - 1 )
			scriptableLayers[index].dirtyLayers.Add( LocalPosToLayerIndex( x, y+1 ) );
		
	}
	public void SetDirty( int x, int y, int width, int height, int index ) {
		if( !IsInBounds(x,y) || !IsInBounds(x+width-1,y+height-1) || index >= scriptableLayers.Length || index < 0 )
			return;
		int endXIndex = (x+width) / chunkWidth;
		int endYIndex = (y+height) / chunkHeight;
		int xStartMod = x % chunkWidth;
		int xEndMod = (x+width) % chunkWidth;
		int yStartMod = y % chunkHeight;
		int yEndMod = (y+height) % chunkHeight;
		for( int xx = x / chunkWidth; xx <= endXIndex; xx++ ) {
			for( int yy = y / chunkHeight; yy <= endYIndex; yy++ ) {
				scriptableLayers[index].dirtyLayers.Add( yy * widthInChunks + xx );
			}
			if( yStartMod == 0 )
				scriptableLayers[index].dirtyLayers.Add( ((y / chunkHeight)-1) * widthInChunks + xx );
			if( yEndMod == chunkHeight - 1 )
				scriptableLayers[index].dirtyLayers.Add( (endYIndex+1) * widthInChunks + xx );
		}
		if( xStartMod == 0 || xEndMod == chunkWidth-1 ) {
			for( int yy = y / chunkHeight; yy <= endYIndex; yy++ ) {
				if( xStartMod == 0 )
					scriptableLayers[index].dirtyLayers.Add( yy * widthInChunks + ((x / chunkWidth)-1) );
				else
					scriptableLayers[index].dirtyLayers.Add( yy * widthInChunks + (endXIndex+1) );
			}
		}
	}


	public bool IsInBounds( int x, int y ) {
		if( x < 0 || y < 0 || x >= chunkWidth*widthInChunks || y >= chunkHeight*heightInChunks )
			return false;
		return true;
	}

	public bool IsInBounds( Vector2 worldPos ) {
		int x = Mathf.FloorToInt( ( worldPos.x - transform.position.x ) / mainLayer.zoomFactor );
		int y = Mathf.FloorToInt( ( worldPos.y - transform.position.y ) / mainLayer.zoomFactor );
		return IsInBounds( x, y );
	}

	public bool IsInBounds( Vector2 worldPos, int index ) {
		if( scriptableLayers.Length <= index )
			return false;
		int x = Mathf.FloorToInt( ( worldPos.x - transform.position.x ) / scriptableLayers[index].layers[0].zoomFactor );
		int y = Mathf.FloorToInt( ( worldPos.y - transform.position.y ) / scriptableLayers[index].layers[0].zoomFactor );
		return IsInBounds( x, y );
	}

	public void AddBorder ( Tile borderTile, int layer ) {
		//left and right walls
		for( int cy = 0; cy < heightInChunks; cy++ ) {
			for( int y = 0; y < chunkHeight; y++ ) {
				scriptableLayers[layer].layers[ cy * widthInChunks].tiles[ y * chunkWidth ] = borderTile;
				scriptableLayers[layer].layers[ cy * widthInChunks + widthInChunks - 1 ].tiles[ y * chunkWidth + chunkWidth - 1 ] = borderTile;
			}
		}

		//top and bottom walls
		for( int cx = 0; cx < widthInChunks; cx++ ) {
			for( int x = 0; x < chunkWidth; x++ ) {
				scriptableLayers[layer].layers[ cx ].tiles[ x ] = borderTile;
				scriptableLayers[layer].layers[ (heightInChunks - 1) * widthInChunks + cx ].tiles[ (chunkHeight - 1) * chunkWidth + x ] = borderTile;
			}
		}
	}

//	void Start () {
//		if( generateAtGameStart ) {
//			Generate();
//		}
//	}

	public virtual void OnValidate () {
		TileInfo tileInfo = GetComponent<TileInfo>();
		if( scriptableLayers == null || scriptableLayers.Length == 0 ) {
			scriptableLayers = new ScriptableTileLayer[1];
			scriptableLayers[0] = new ScriptableTileLayer();
			scriptableLayers[0].name = "Layer 0";
			scriptableLayers[0].update3DWalls = tileInfo.update3DWalls;
			scriptableLayers[0].update2DColliders = tileInfo.update2DColliders;
		}
		
		if( scriptableLayers[0].layers == null || scriptableLayers[0].layers.Length == 0 ) {
			scriptableLayers[0].layers = new TileInfo[1] { tileInfo };
		}

		if( scriptableLayers[0].layers[0] != tileInfo )
			scriptableLayers[0].layers[0] = tileInfo;

		for( int i = 1; i < scriptableLayers.Length; i++ ) {
			if( scriptableLayers[i].name == scriptableLayers[i-1].name )
				scriptableLayers[i].name = "Layer " + i;
			if( scriptableLayers[i].layers.Length > 0 && scriptableLayers[i].layers[0] == scriptableLayers[0].layers[0] ) {
				scriptableLayers[i].layers = scriptableLayers[i].layers = new TileInfo[ widthInChunks * heightInChunks ];
			}
		}

		for( int i = 0; i < scriptableLayers.Length; i++ ) {
			if( scriptableLayers[i].layers == null )
				scriptableLayers[i].layers = new TileInfo[ widthInChunks * heightInChunks ];
			if( i > 0 && scriptableLayers[i].layers[0] == null ) {
				scriptableLayers[i].layers[0] = CloneLayer( 0, mainLayer, i );
			}
			for( int l = 0; l < scriptableLayers[i].layers.Length; l++ ) {
				if( scriptableLayers[i].layers[l] == null )
					continue;

				if( i != 0 || l != 0 )
					scriptableLayers[i].layers[l].name = scriptableLayers[i].name + "_" + l;
				
				scriptableLayers[i].layers[l].GetComponent<MeshRenderer>().sortingLayerID = scriptableLayers[i].sortingLayer;
				scriptableLayers[i].layers[l].GetComponent<MeshRenderer>().sortingOrder = scriptableLayers[i].sortingOrder;
				scriptableLayers[i].layers[l].transform.position = (Vector3)(Vector2)scriptableLayers[i].layers[l].transform.position + new Vector3( 0, 0, scriptableLayers[i].worldZ );
				scriptableLayers[i].layers[l].update3DWalls = scriptableLayers[i].update3DWalls;
				scriptableLayers[i].layers[l].update2DColliders = scriptableLayers[i].update2DColliders;

				if( scriptableLayers[i].parallaxDistance != 0 ) {
					if( scriptableLayers[i].layers[l].GetComponent<ParallaxBackground2>() == null ) {
						scriptableLayers[i].layers[l].gameObject.AddComponent<ParallaxBackground2>();
						if( FindObjectOfType<PixelPerfectCamera>() != null )
							scriptableLayers[i].layers[l].GetComponent<ParallaxBackground2>().pixelSnap = true;
					}
					scriptableLayers[i].layers[l].GetComponent<ParallaxBackground2>()._distance = scriptableLayers[i].parallaxDistance;
					scriptableLayers[i].layers[l].GetComponent<ParallaxBackground2>()._offset = (Vector2)scriptableLayers[i].layers[l].transform.position - ( (Vector2)transform.position + new Vector2( width, height ) * 0.5f * scriptableLayers[i].layers[l].zoomFactor );
				}
			}
		}
	}

	public virtual bool IsCollision ( int x, int y ) {
		return ( this[ x, y, 0 ].GetCollisionType( mainLayer ) == TileInfo.CollisionType.Full );
	}

	public virtual bool IsCollision ( int x, int y, int index ) {
		return ( this[ x, y, index ].GetCollisionType( scriptableLayers[index].layers[0] ) == TileInfo.CollisionType.Full );
	}

	public bool Linecast ( Vector2 start, Vector2 end, out GridcastHit2D hit ) {
		
		Vector2 u = ( start - (Vector2)transform.position ) / mainLayer.zoomFactor;
		Vector2 v = ( end - (Vector2)transform.position ) / mainLayer.zoomFactor - u;
		float length = v.magnitude;
		v /= length;
		int xSign = (int)Mathf.Sign( v.x );
		int ySign =	(int)Mathf.Sign( v.y );
		float tMaxX = ( xSign > 0 ) ? (Mathf.Ceil(u.x)-u.x) / v.x : (Mathf.Floor(u.x)-u.x) / v.x;
		float tMaxY = ( ySign > 0 ) ? (Mathf.Ceil(u.y)-u.y) / v.y : (Mathf.Floor(u.y)-u.y) / v.y;
		float tDeltaX = Mathf.Abs( 1 / v.x );
		float tDeltaY = Mathf.Abs( 1 / v.y );
		int x = Mathf.FloorToInt( u.x );
		int y = Mathf.FloorToInt( u.y );
		while( tMaxX < length || tMaxY < length ) {
			if( tMaxX < tMaxY ) {
				x += xSign;
				if( IsCollision( x, y ) ) {
					hit.xIndex = x;
					hit.yIndex = y;
					hit.distance = tMaxX;
					hit.hitPoint = start + v * tMaxX * mainLayer.zoomFactor;
					return true;
				}
				tMaxX += tDeltaX;
			}
			else {
				y += ySign;
				if( IsCollision( x, y ) ) {
					hit.xIndex = x;
					hit.yIndex = y;
					hit.distance = tMaxY;
					hit.hitPoint = start + v * tMaxY * mainLayer.zoomFactor;
					return true;
				}
				tMaxY += tDeltaY;
			}

		}
		hit.xIndex=x;
		hit.yIndex=y;
		hit.hitPoint=end;
		hit.distance=length;
		return false;
	}

	public GridcastHit2D[] LinecastAll ( Vector2 start, Vector2 end ) {
		List<GridcastHit2D> result = new List<GridcastHit2D>();
		Vector2 u = ( start - (Vector2)transform.position ) / mainLayer.zoomFactor;
		Vector2 v = ( end - (Vector2)transform.position ) / mainLayer.zoomFactor - u;
		float length = v.magnitude;
		v /= length;
		int xSign = (int)Mathf.Sign( v.x );
		int ySign =	(int)Mathf.Sign( v.y );
		float tMaxX = ( xSign > 0 ) ? (Mathf.Ceil(u.x)-u.x) / v.x : (Mathf.Floor(u.x)-u.x) / v.x;
		float tMaxY = ( ySign > 0 ) ? (Mathf.Ceil(u.y)-u.y) / v.y : (Mathf.Floor(u.y)-u.y) / v.y;
		float tDeltaX = Mathf.Abs( 1 / v.x );
		float tDeltaY = Mathf.Abs( 1 / v.y );
		int x = Mathf.FloorToInt( u.x );
		int y = Mathf.FloorToInt( u.y );
		if( u.x % 1 == 0 )
			x--;
		if( u.y % 1 == 0 )
			y--;
		while( tMaxX < length || tMaxY < length ) {
			if( tMaxX < tMaxY ) {
				x += xSign;
				if( IsCollision( x, y ) ) {
					GridcastHit2D hit = new GridcastHit2D();
					hit.xIndex = x;
					hit.yIndex = y;
					hit.distance = tMaxX;
					hit.hitPoint = start + v * tMaxX * mainLayer.zoomFactor;
					result.Add( hit );
				}
				tMaxX += tDeltaX;
			}
			else {
				y += ySign;
				if( IsCollision( x, y ) ) {
					GridcastHit2D hit = new GridcastHit2D();
					hit.xIndex = x;
					hit.yIndex = y;
					hit.distance = tMaxY;
					hit.hitPoint = start + v * tMaxY * mainLayer.zoomFactor;
					result.Add( hit );
				}
				tMaxY += tDeltaY;
			}

		}
		return result.ToArray();
	}

}

