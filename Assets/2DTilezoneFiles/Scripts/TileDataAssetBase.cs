using UnityEngine;
using System.Collections;

public abstract class TileDataAssetBase : ScriptableObject, ITilezoneTileDataStream {
	public abstract bool isDataReady { get; }
	public abstract int width { get; }
	public abstract int height { get; }
	public abstract int numberOfLayers { get; }
	public abstract Tile this[ int x, int y, int index ] { get; set; }
	public abstract bool IsInBounds( int x, int y );
	public abstract int GetLayerHash( int index );
}
