using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class SerializedTileData : ITilezoneTileDataStream {
	public short _width;
	public short _height;
	public byte _numberOfLayers;
	public int[] layerHashes;
	public int[] tileData;
	bool _ready;

	#region ITilezoneTileDataStream implementation

	public int width {
		get {
			return _width;
		}
	}

	public int height {
		get {
			return _height;
		}
	}

	public int numberOfLayers {
		get {
			return _numberOfLayers;
		}
	}

	public bool isDataReady {
		get {
			return _ready;
		}
	}

	public int GetLayerHash ( int index ) {
		if( index >= layerHashes.Length || index < 0 )
			return 0;
		return layerHashes[index];
	}

	public Tile this [int x, int y, int index] {
		get {
			if( !IsInRange( x, y, index ) )
				return Tile.empty;
			return Tile.DeSerialize( tileData[ index * width * height + y * width + x ] );
		}
		set {
			if( !IsInRange( x, y, index ) )
				return;
			tileData[ index * width * height + y * width + x ] = value.Serialize();
		}
	}

	#endregion

	bool IsInRange( int x, int y, int index ) {
		return ( x >= 0 && x < _width && y >= 0 && y < _height && index >= 0 && index < _numberOfLayers );
	}
	public bool IsInBounds( int x, int y ) {
		return ( x >= 0 && x < _width && y >= 0 && y < _height );
	}
	public SerializedTileData( int width, int height, int numberOfLayers, int[] layerHashes ) {
		this._width = (short)width;
		this._height = (short)height;
		this._numberOfLayers = (byte)numberOfLayers;
		this.tileData = new int[ width * height * numberOfLayers ];
		this._ready = false;
		this.layerHashes = layerHashes;
	}
	public SerializedTileData( ITilezoneTileDataStream layerInfo ) {
		this._width = (short)layerInfo.width;
		this._height = (short)layerInfo.height;
		this._numberOfLayers = (byte)layerInfo.numberOfLayers;
		this.tileData = new int[ width * height * numberOfLayers ];
		this._ready = false;
		this.layerHashes = new int[_numberOfLayers];
		for( int i = 0; i < this._numberOfLayers; i++ ) {
			this.layerHashes[i] = layerInfo.GetLayerHash( i );
		}
	}
	public void ApplyChanges( IEnumerable<KeyValuePair<int,int>> changes ) {
		foreach( KeyValuePair<int,int> change in changes ) {
			tileData[ change.Key ] = change.Value;
		}
	}
	public void SetReady( bool ready ) {
		_ready = ready;
	}
}

[System.Serializable]
public class ScriptableTileLayer {
	public string name;
	public float worldZ;
	[Tooltip("Creates ParallaxBackground component only if Parallax Distance is not 0 (can be negative for foreground)")]
	public float parallaxDistance;
	public bool update3DWalls;
	public bool update2DColliders;
	public TileInfo[] layers;

	[SortingLayerIndexAttribute]
	public int sortingLayer;
	public int sortingOrder;

	HashSet<int> _dirtyLayers;

	public HashSet<int> dirtyLayers {
		get {
			if( _dirtyLayers == null )
				_dirtyLayers = new HashSet<int>();
			return _dirtyLayers;
		}
		set {
			_dirtyLayers = value;
		}
	}
}

public static class ScriptableTileLayerHelper {

	public delegate Tile TileFunctionDelegate ( int x, int y );
	public delegate Tile TileFunctionDelegate<T> ( int x, int y, T arg );
	public delegate Tile TileFunctionDelegate<T1, T2> ( int x, int y, T1 arg1, T2 arg2 );
	public delegate Tile TileFunctionDelegate<T1, T2, T3> ( int x, int y, T1 arg1, T2 arg2, T3 arg3 );
	public delegate bool TileFunctionBoolDelegate( int x, int y );
	public delegate bool TileFunctionBoolDelegate<T>( int x, int y, T arg );
	public delegate bool TileFunctionBoolDelegate<T1, T2>( int x, int y, T1 arg1, T2 arg2 );
	public delegate bool TileFunctionBoolDelegate<T1, T2, T3>( int x, int y, T1 arg1, T2 arg2, T3 arg3 );

	public static bool NoiseFunction ( int x, int y, Vector2 scale, Vector2 offset, float chance ) {
		if( Mathf.PerlinNoise( ( x + offset.x ) * scale.x, ( y + offset.y ) * scale.y ) < chance )
			return true;
		return false;
	}

	public static void StampTileData ( ITilezoneTileDataStream dataFrom, ITilezoneTileDataStream dataTo, int x, int y, bool writeEmptyTiles, bool overwrite = true ) {
		if( !dataFrom.isDataReady )
			return;
		for( int xx = 0; xx < dataFrom.width; xx++ ) {
			for( int yy = 0; yy < dataFrom.height; yy++ ) {
				for( int i = 0; i < dataFrom.numberOfLayers && i < dataTo.numberOfLayers; i++ ) {
					if( !overwrite && dataTo[x+xx, y+yy, i] != Tile.empty )
						continue;
					Tile newTile = dataFrom[xx, yy,i];
					if( writeEmptyTiles || newTile != Tile.empty )
						dataTo[x+xx, y+yy, i] = newTile;
				}
			}
		}
	}
	public static void StampTileData ( ITilezoneTileDataStream dataFrom, int indexFrom, ITilezoneTileDataStream dataTo, int indexTo, int x, int y, bool writeEmptyTiles, bool overwrite = true ) {
		if( !dataFrom.isDataReady )
			return;
		for( int xx = 0; xx < dataFrom.width; xx++ ) {
			for( int yy = 0; yy < dataFrom.height; yy++ ) {
				if( !overwrite && dataTo[x+xx, y+yy, indexTo] != Tile.empty )
					continue;
				Tile newTile = dataFrom[xx, yy,indexFrom];
				if( writeEmptyTiles || newTile != Tile.empty )
					dataTo[x+xx, y+yy, indexTo] = newTile;
			}
		}
	}

	public static void BoxOutline( int x, int y, int width, int height, ITilezoneTileDataStream tiles, int index, Tile tile ) {
		for( int xx = 0; xx < width; xx++ ) {
			tiles[x+xx,y,index] = tile;
			tiles[x+xx,y+height-1,index] = tile;
		}
		for( int yy = 1; yy < height-1; yy++ ) {
			tiles[x,y+yy,index] = tile;
			tiles[x+width-1,y+yy,index] = tile;
		}
	}



	#region FillTiles and CarveTile Overloads
	public static void FillAllTiles ( ITilezoneTileDataStream tiles, int index, Tile tile ) {
		for( int x = 0; x < tiles.width; x++ ) {
			for( int y = 0; y < tiles.height; y++ ) {
				tiles[x,y,index] = tile;
			}
		}
	}
	public static void FillAllTiles ( ITilezoneTileDataStream tiles, int index, TileFunctionDelegate TileFunc, bool writeEmptyTiles = false ) {
		for( int x = 0; x < tiles.width; x++ ) {
			for( int y = 0; y < tiles.height; y++ ) {
				Tile newTile = TileFunc( x , y);
				if( writeEmptyTiles || newTile != Tile.empty )
					tiles[x,y,index] = newTile;
			}
		}
	}
	public static void FillAllTiles<T> ( ITilezoneTileDataStream tiles, int index, TileFunctionDelegate<T> TileFunc, T arg, bool writeEmptyTiles = false ) {
		for( int x = 0; x < tiles.width; x++ ) {
			for( int y = 0; y < tiles.height; y++ ) {
				Tile newTile = TileFunc( x, y, arg );
				if( writeEmptyTiles || newTile != Tile.empty )
					tiles[x,y,index] = newTile;
			}
		}
	}
	public static void FillAllTiles<T1, T2> ( ITilezoneTileDataStream tiles, int index, TileFunctionDelegate<T1, T2> TileFunc, T1 arg1, T2 arg2, bool writeEmptyTiles = false ) {
		for( int x = 0; x < tiles.width; x++ ) {
			for( int y = 0; y < tiles.height; y++ ) {
				Tile newTile = TileFunc( x, y, arg1, arg2 );
				if( writeEmptyTiles || newTile != Tile.empty )
					tiles[x,y,index] = newTile;
			}
		}
	}
	public static void FillAllTiles<T1, T2, T3> ( ITilezoneTileDataStream tiles, int index, TileFunctionDelegate<T1, T2, T3> TileFunc, T1 arg1, T2 arg2, T3 arg3, bool writeEmptyTiles = false ) {
		for( int x = 0; x < tiles.width; x++ ) {
			for( int y = 0; y < tiles.height; y++ ) {
				Tile newTile = TileFunc( x, y, arg1, arg2, arg3 );
				if( writeEmptyTiles || newTile != Tile.empty )
					tiles[x,y,index] = newTile;
			}
		}
	}
	public static void FillAllTiles ( ITilezoneTileDataStream tiles, int index, Tile tile, TileFunctionBoolDelegate TileFunc, bool writeEmptyTiles = false ) {
		for( int x = 0; x < tiles.width; x++ ) {
			for( int y = 0; y < tiles.height; y++ ) {
				Tile newTile = TileFunc( x, y ) ? tile : Tile.empty;
				if( writeEmptyTiles || newTile != Tile.empty )
					tiles[x,y,index] = newTile;
			}
		}
	}
	public static void FillAllTiles<T> ( ITilezoneTileDataStream tiles, int index, Tile tile, TileFunctionBoolDelegate<T> TileFunc, T arg, bool writeEmptyTiles = false ) {
		for( int x = 0; x < tiles.width; x++ ) {
			for( int y = 0; y < tiles.height; y++ ) {
				Tile newTile = TileFunc( x, y, arg ) ? tile : Tile.empty;
				if( writeEmptyTiles || newTile != Tile.empty )
					tiles[x,y,index] = newTile;
			}
		}
	}
	public static void FillAllTiles<T1, T2> ( ITilezoneTileDataStream tiles, int index, Tile tile, TileFunctionBoolDelegate<T1, T2> TileFunc, T1 arg1, T2 arg2, bool writeEmptyTiles = false ) {
		for( int x = 0; x < tiles.width; x++ ) {
			for( int y = 0; y < tiles.height; y++ ) {
				Tile newTile = TileFunc( x, y, arg1, arg2 ) ? tile : Tile.empty;
				if( writeEmptyTiles || newTile != Tile.empty )
					tiles[x,y,index] = newTile;
			}
		}
	}
	public static void FillAllTiles<T1, T2, T3> ( ITilezoneTileDataStream tiles, int index, Tile tile, TileFunctionBoolDelegate<T1, T2, T3> TileFunc, T1 arg1, T2 arg2, T3 arg3, bool writeEmptyTiles = false ) {
		for( int x = 0; x < tiles.width; x++ ) {
			for( int y = 0; y < tiles.height; y++ ) {
				Tile newTile = TileFunc( x, y, arg1, arg2, arg3 ) ? tile : Tile.empty;
				if( writeEmptyTiles || newTile != Tile.empty )
					tiles[x,y,index] = newTile;
			}
		}
	}

	public static void FillTiles ( int x, int y, int width, int height, ITilezoneTileDataStream tiles, int index, Tile tile ) {
		for( int xx = 0; xx < width; xx++ ) {
			for( int yy = 0; yy < height; yy++ ) {
				tiles[x+xx, y+yy, index] = tile;
			}
		}
	}
	public static void FillTiles ( int x, int y, int width, int height, ITilezoneTileDataStream tiles, int index, TileFunctionDelegate TileFunc, bool useLocalCoordinates, bool writeEmptyTiles = false ) {
		for( int xx = 0; xx < width; xx++ ) {
			for( int yy = 0; yy < height; yy++ ) {
				Tile newTile = useLocalCoordinates ? TileFunc( xx, yy ) : TileFunc( x+xx, y+yy );
				if( writeEmptyTiles || newTile != Tile.empty )
					tiles[x+xx, y+yy, index] = newTile;
			}
		}
	}
	public static void FillTiles<T> ( int x, int y, int width, int height, ITilezoneTileDataStream tiles, int index, TileFunctionDelegate<T> TileFunc, T arg, bool useLocalCoordinates, bool writeEmptyTiles = false ) {
		for( int xx = 0; xx < width; xx++ ) {
			for( int yy = 0; yy < height; yy++ ) {
				Tile newTile = useLocalCoordinates ? TileFunc( xx, yy, arg ) : TileFunc( x+xx, y+yy, arg );
				if( writeEmptyTiles || newTile != Tile.empty )
					tiles[x+xx, y+yy, index] = newTile;
			}
		}
	}
	public static void FillTiles<T1, T2> ( int x, int y, int width, int height, ITilezoneTileDataStream tiles, int index, TileFunctionDelegate<T1, T2> TileFunc, T1 arg1, T2 arg2, bool useLocalCoordinates, bool writeEmptyTiles = false ) {
		for( int xx = 0; xx < width; xx++ ) {
			for( int yy = 0; yy < height; yy++ ) {
				Tile newTile = useLocalCoordinates ? TileFunc( xx, yy, arg1, arg2 ) : TileFunc( x+xx, y+yy, arg1, arg2 );
				if( writeEmptyTiles || newTile != Tile.empty )
					tiles[x+xx, y+yy, index] = newTile;
			}
		}
	}
	public static void FillTiles<T1, T2, T3> ( int x, int y, int width, int height, ITilezoneTileDataStream tiles, int index, TileFunctionDelegate<T1, T2, T3> TileFunc, T1 arg1, T2 arg2, T3 arg3, bool useLocalCoordinates, bool writeEmptyTiles = false ) {
		for( int xx = 0; xx < width; xx++ ) {
			for( int yy = 0; yy < height; yy++ ) {
				Tile newTile = useLocalCoordinates ? TileFunc( xx, yy, arg1, arg2, arg3 ) : TileFunc( x+xx, y+yy, arg1, arg2, arg3 );
				if( writeEmptyTiles || newTile != Tile.empty )
					tiles[x+xx, y+yy, index] = newTile;
			}
		}
	}
	public static void FillTiles ( int x, int y, int width, int height, ITilezoneTileDataStream tiles, int index, Tile tile, TileFunctionBoolDelegate TileFunc, bool useLocalCoordinates, bool writeEmptyTiles = false ) {
		for( int xx = 0; xx < width; xx++ ) {
			for( int yy = 0; yy < height; yy++ ) {
				bool func = useLocalCoordinates ? TileFunc( xx, yy ) : TileFunc( x+xx, y+yy );
				if( func )
					tiles[x+xx, y+yy, index] = tile;
				else if( writeEmptyTiles )
					tiles[x+xx, y+yy, index] = Tile.empty;
			}
		}
	}
	public static void FillTiles<T> ( int x, int y, int width, int height, ITilezoneTileDataStream tiles, int index, Tile tile, TileFunctionBoolDelegate<T> TileFunc, T arg, bool useLocalCoordinates, bool writeEmptyTiles = false ) {
		for( int xx = 0; xx < width; xx++ ) {
			for( int yy = 0; yy < height; yy++ ) {
				bool func = useLocalCoordinates ? TileFunc( xx, yy, arg ) : TileFunc( x+xx, y+yy, arg );
				if( func )
					tiles[x+xx, y+yy, index] = tile;
				else if( writeEmptyTiles )
					tiles[x+xx, y+yy, index] = Tile.empty;
			}
		}
	}
	public static void FillTiles<T1, T2> ( int x, int y, int width, int height, ITilezoneTileDataStream tiles, int index, Tile tile, TileFunctionBoolDelegate<T1, T2> TileFunc, T1 arg1, T2 arg2, bool useLocalCoordinates, bool writeEmptyTiles = false ) {
		for( int xx = 0; xx < width; xx++ ) {
			for( int yy = 0; yy < height; yy++ ) {
				bool func = useLocalCoordinates ? TileFunc( xx, yy, arg1, arg2 ) : TileFunc( x+xx, y+yy, arg1, arg2 );
				if( func )
					tiles[x+xx, y+yy, index] = tile;
				else if( writeEmptyTiles )
					tiles[x+xx, y+yy, index] = Tile.empty;
			}
		}
	}
	public static void FillTiles<T1, T2, T3> ( int x, int y, int width, int height, ITilezoneTileDataStream tiles, int index, Tile tile, TileFunctionBoolDelegate<T1, T2, T3> TileFunc, T1 arg1, T2 arg2, T3 arg3, bool useLocalCoordinates, bool writeEmptyTiles = false ) {
		for( int xx = 0; xx < width; xx++ ) {
			for( int yy = 0; yy < height; yy++ ) {
				bool func = useLocalCoordinates ? TileFunc( xx, yy, arg1, arg2, arg3 ) : TileFunc( x+xx, y+yy, arg1, arg2, arg3 );
				if( func )
					tiles[x+xx, y+yy, index] = tile;
				else if( writeEmptyTiles )
					tiles[x+xx, y+yy, index] = Tile.empty;
			}
		}
	}

	public static void CarveAllTiles ( ITilezoneTileDataStream tiles, int index, TileFunctionBoolDelegate CarveFunc ) {
		for( int x = 0; x < tiles.width; x++ ) {
			for( int y = 0; y < tiles.height; y++ ) {
				if( CarveFunc( x, y ) )
					tiles[x, y, index] = Tile.empty;
			}
		}
	}
	public static void CarveAllTiles<T> ( ITilezoneTileDataStream tiles, int index, TileFunctionBoolDelegate<T> CarveFunc, T arg ) {
		for( int x = 0; x < tiles.width; x++ ) {
			for( int y = 0; y < tiles.height; y++ ) {
				if( CarveFunc( x, y, arg ) )
					tiles[x, y, index] = Tile.empty;
			}
		}
	}
	public static void CarveAllTiles<T1, T2> ( ITilezoneTileDataStream tiles, int index, TileFunctionBoolDelegate<T1, T2> CarveFunc, T1 arg1, T2 arg2 ) {
		for( int x = 0; x < tiles.width; x++ ) {
			for( int y = 0; y < tiles.height; y++ ) {
				if( CarveFunc( x, y, arg1, arg2 ) )
					tiles[x, y, index] = Tile.empty;
			}
		}
	}
	public static void CarveAllTiles<T1, T2, T3> ( ITilezoneTileDataStream tiles, int index, TileFunctionBoolDelegate<T1, T2, T3> CarveFunc, T1 arg1, T2 arg2, T3 arg3 ) {
		for( int x = 0; x < tiles.width; x++ ) {
			for( int y = 0; y < tiles.height; y++ ) {
				if( CarveFunc( x, y, arg1, arg2, arg3 ) )
					tiles[x, y, index] = Tile.empty;
			}
		}
	}

	public static void CarveTiles ( int x, int y, int width, int height, ITilezoneTileDataStream tiles, int index ) {
		for( int xx = 0; xx < width; xx++ ) {
			for( int yy = 0; yy < height; yy++ ) {
				tiles[x+xx, y+yy, index] = Tile.empty;
			}
		}
	}
	public static void CarveTiles ( int x, int y, int width, int height, ITilezoneTileDataStream tiles, int index, TileFunctionBoolDelegate CarveFunc, bool useLocalCoordinates ) {
		for( int xx = 0; xx < width; xx++ ) {
			for( int yy = 0; yy < height; yy++ ) {
				bool func = useLocalCoordinates ? CarveFunc( xx, yy ) : CarveFunc( x+xx, y+yy );
				if( func )
					tiles[x+xx, y+yy, index] = Tile.empty;
			}
		}
	}
	public static void CarveTiles<T> ( int x, int y, int width, int height, ITilezoneTileDataStream tiles, int index, TileFunctionBoolDelegate<T> CarveFunc, T arg, bool useLocalCoordinates ) {
		for( int xx = 0; xx < width; xx++ ) {
			for( int yy = 0; yy < height; yy++ ) {
				bool func = useLocalCoordinates ? CarveFunc( xx, yy, arg ) : CarveFunc( x+xx, y+yy, arg );
				if( func )
					tiles[x+xx, y+yy, index] = Tile.empty;
			}
		}
	}
	public static void CarveTiles<T1, T2> ( int x, int y, int width, int height, ITilezoneTileDataStream tiles, int index, TileFunctionBoolDelegate<T1, T2> CarveFunc, T1 arg1, T2 arg2, bool useLocalCoordinates ) {
		for( int xx = 0; xx < width; xx++ ) {
			for( int yy = 0; yy < height; yy++ ) {
				bool func = useLocalCoordinates ? CarveFunc( xx, yy, arg1, arg2 ) : CarveFunc( x+xx, y+yy, arg1, arg2 );
				if( func )
					tiles[x+xx, y+yy, index] = Tile.empty;
			}
		}
	}
	public static void CarveTiles<T1, T2, T3> ( int x, int y, int width, int height, ITilezoneTileDataStream tiles, int index, TileFunctionBoolDelegate<T1, T2, T3> CarveFunc, T1 arg1, T2 arg2, T3 arg3, bool useLocalCoordinates ) {
		for( int xx = 0; xx < width; xx++ ) {
			for( int yy = 0; yy < height; yy++ ) {
				bool func = useLocalCoordinates ? CarveFunc( xx, yy, arg1, arg2, arg3 ) : CarveFunc( x+xx, y+yy, arg1, arg2, arg3 );
				if( func )
					tiles[x+xx, y+yy, index] = Tile.empty;
			}
		}
	}
	#endregion

}

public interface ITilezoneContinousLoad {
	Transform[] players { get; }
	int viewDistInChunks { get; }
	ITilezoneTileDataStream dataToLoad { get; }
}

public interface ITilezoneTileDataStream {
	bool isDataReady { get; }
	int width { get; }
	int height { get; }
	int numberOfLayers { get; }
	Tile this[ int x, int y, int index ] { get; set; }
	bool IsInBounds( int x, int y );
	int GetLayerHash ( int index );
}

public struct GridcastHit2D {
	public int xIndex;
	public int yIndex;
	public Vector2 hitPoint;
	public float distance;
}