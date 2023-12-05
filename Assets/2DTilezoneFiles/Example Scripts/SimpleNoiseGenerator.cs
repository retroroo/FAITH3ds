using UnityEngine;
using System.Collections;

[AddComponentMenu("2D/Example Scripts/Simple Noise Generator")]
public class SimpleNoiseGenerator : ScriptableTileLayerBase {

	[SerializeField] Tile groundTile;
	[SerializeField] Vector2 noiseScale = new Vector2( 0.1f, 0.1f );
	[Range(0,1)] [SerializeField] float groundAmount = 0.5f;

	public Tile NoiseFunction ( int x, int y, Vector2 offset ) {
		if( Mathf.PerlinNoise( offset.x + x * noiseScale.x, offset.y + y * noiseScale.y ) < groundAmount )
			return groundTile;
		return Tile.empty;
	}

	public override void GenerateMethod (ITilezoneTileDataStream dataStream) {
		Random.seed = seed;
		Vector2 offset = Random.insideUnitCircle * 100000;
		ScriptableTileLayerHelper.FillAllTiles( dataStream, 0, NoiseFunction, offset );
	}

}
