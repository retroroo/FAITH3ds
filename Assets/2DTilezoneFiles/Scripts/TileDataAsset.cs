using UnityEngine;
using System.Collections;

public class TileDataAsset : TileDataAssetBase {

	public int _width;
	public int _height;
	public int _numberOfLayers;
	public int[] layerHashes;
	public Tile[] tiles;

	public override int height {
		get {
			return _height;
		}
	}

	public override int width {
		get {
			return _width;
		}
	}

	public override int numberOfLayers {
		get {
			return _numberOfLayers;
		}
	}

	public override int GetLayerHash (int index)
	{
		if( index < 0 || index >= layerHashes.Length )
			return 0;
		return layerHashes[index];
	}

	public override Tile this[int x, int y, int index ] {
		get {
			if( x < 0 || x >= _width || y < 0 || y >= _height )
				return Tile.empty;
			return tiles[index*_width*_height+y*_width+x];
		}
		set {
			if( x < 0 || x >= _width || y < 0 || y >= _height )
				return;
			tiles[index*_width*_height+y*_width+x] = value;
		}
	}

	public override bool isDataReady {
		get {
			return true;
		}
	}

	public override bool IsInBounds ( int x, int y ) {
		return( x >= 0 && x < _width && y >= 0 && y < _height );
	}
}
