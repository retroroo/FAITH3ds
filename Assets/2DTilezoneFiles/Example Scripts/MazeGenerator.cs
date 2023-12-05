using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[AddComponentMenu("2D/Example Scripts/Maze Generator")]
public class MazeGenerator : ScriptableTileLayerBase {

	public Tile wallTile;
	public Tile floorTile;

	public class MazeCell {
		public int neighbourMask;
		public int x, y;
		public MazeCell ( int neighbourMask, int x, int y ) {
			this.neighbourMask = neighbourMask;
			this.x = x;
			this.y = y;
		}
	}
	int NumberOfSetBits ( int i ) {
		i = i - ( ( i >> 1 ) & 0x55555555 );
		i = ( i & 0x33333333 ) + ( ( i >> 2 ) & 0x33333333 );
		return ( ( ( i + ( i >> 4 ) ) & 0x0f0f0f0f ) * 0x01010101 ) >> 24;
	}
	int BitmaskRandom ( int mask ) {
		if( mask <= 0 )
			return 0;
		int setBits = NumberOfSetBits( mask );
		int rand = Random.Range( 0, setBits )+1;
		int n = 0;
		int pos = 0;
		while( n < rand ) {
			if( ( ( 1 << pos ) & mask ) > 0 )
				n++;
			pos++;
		}
		return pos-1;
	}

	MazeCell[,] SetUpGrid (ITilezoneTileDataStream dataStream, int mazeWidth, int mazeHeight) {
		MazeCell[,] result = new MazeCell[mazeWidth,mazeHeight];

		for( int x = 0; x < mazeWidth; x++ ) {
			for( int y = 0; y < mazeWidth; y++ ) {
				int neighbourMask = 15;					
				int xIndex = x*2+1;
				int yIndex = y*2+1;
				dataStream[xIndex,yIndex+1,0] = wallTile;
				dataStream[xIndex+1,yIndex+1,0] = wallTile;
				dataStream[xIndex+1,yIndex,0] = wallTile;
				dataStream[xIndex,yIndex,0] = floorTile;
				if( x == mazeWidth-1 )
					neighbourMask &= 13;
				if( y == mazeHeight-1 )
					neighbourMask &= 11;
				if( y == 0 ) {
					neighbourMask &= 14;
					dataStream[xIndex,yIndex-1,0] = wallTile;
					dataStream[xIndex+1,yIndex-1,0] = wallTile;
					if( x == 0 )
						dataStream[xIndex-1,yIndex-1,0] = wallTile;
				}
				if( x == 0 ) {
					neighbourMask &= 7;
					dataStream[xIndex-1,yIndex+1,0] = wallTile;
					dataStream[xIndex-1,yIndex,0] = wallTile;
				}
				result[x,y] = new MazeCell( neighbourMask, x, y );
			}
		}
		return result;
	}

	void BlockCell( MazeCell[,] cells, int mazeWidth, int mazeHeight, MazeCell cell ) {
		if( cell.x > 0 )
			cells[cell.x-1,cell.y].neighbourMask &= 13;
		if( cell.x < mazeWidth-1 )
			cells[cell.x+1,cell.y].neighbourMask &= 7;
		if( cell.y > 0 )
			cells[cell.x,cell.y-1].neighbourMask &= 11;
		if( cell.y < mazeHeight-1 )
			cells[cell.x,cell.y+1].neighbourMask &= 14;
	}

	public override void GenerateMethod (ITilezoneTileDataStream dataStream) {
		int mazeWidth = (width-1)/2;
		int mazeHeight = (height-1)/2;
		MazeCell[,] cells =  SetUpGrid(dataStream, mazeWidth, mazeHeight);
		Stack<MazeCell> path = new Stack<MazeCell>();
		path.Push( cells[0,0] );
		BlockCell( cells, mazeWidth, mazeHeight, cells[0,0] );

		while( path.Count > 0 ) {
			MazeCell thisCell = path.Peek();
			if( thisCell.neighbourMask == 0 ) {
				path.Pop();
				if( path.Count == 1 )
					break;
				continue;
			}
			int randPath = BitmaskRandom( thisCell.neighbourMask );
			MazeCell nextCell;
			switch ( randPath ) {
			case 0: default:
				nextCell = cells[thisCell.x,thisCell.y-1];
				dataStream[thisCell.x*2+1,thisCell.y*2,0] = floorTile;
				break;
			case 1:
				nextCell = cells[thisCell.x+1,thisCell.y];
				dataStream[thisCell.x*2+2,thisCell.y*2+1,0] = floorTile;
				break;
			case 2:
				nextCell = cells[thisCell.x,thisCell.y+1];
				dataStream[thisCell.x*2+1,thisCell.y*2+2,0] = floorTile;
				break;
			case 3:
				nextCell = cells[thisCell.x-1,thisCell.y];
				dataStream[thisCell.x*2,thisCell.y*2+1,0] = floorTile;
				break;
			}
			BlockCell( cells, mazeWidth, mazeHeight, nextCell );
			path.Push( nextCell );
		}
	}
}
