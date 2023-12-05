using UnityEngine;
using System.Collections;

[AddComponentMenu("2D/Parallax Background")]
[ExecuteInEditMode]
public class ParallaxBackground2 : MonoBehaviour {

	public float _distance = 5;
	public Vector2 _offset;
	public Camera cam;

	public bool pixelSnap;
	MaterialPropertyBlock matreialPropertyBlock;

	[HideInInspector] [SerializeField] bool tileX;
	[HideInInspector] [SerializeField] bool tileY;

	[HideInInspector]  [SerializeField] [Range(1,20)] int tileXAmount = 1;
	[HideInInspector]  [SerializeField] [Range(1,20)] int tileYAmount= 1;

	#if UNITY_EDITOR
	[HideInInspector] public bool editorShowMesh;
	#endif

	Mesh spriteMesh;
	Material spriteMat;
	float width;
	float height;
	Sprite fadeToSprite;
	Mesh fadeToMesh;
	Material fadeToMaterial;
	float fadeToWidth;
	float fadeToHeight;

	public float distance {
		get {
			return _distance;
		}
		set {
			distance = value;
			ratio = 1 - Mathf.Pow( 0.9f, value );
		}
	}
	Vector3 startPos;
	float ratio;

	bool isFading = false;
	IEnumerator FadeSpritesCoroutine( Sprite newSprite, bool tileX, bool tileY, int tileXAmount, int tileYAmount, float time ) {
		SpriteRenderer sRend = GetComponent<SpriteRenderer>();
		if( sRend != null && sRend.sprite != newSprite ) {
			Mesh newMesh;
			float newWidth;
			float newHeight;
			CreateSpriteMesh( newSprite, out newMesh, out newWidth, out newHeight );
			Material newMat = new Material( sRend.sharedMaterial );
			newMat.mainTexture = newSprite.texture;
			isFading = true;
			float timeAtFadeStart = Time.time;

			Color tempCol = spriteMat.color;
			while( Time.time < timeAtFadeStart + time ) {
				float t = ( Time.time - timeAtFadeStart ) / time;
				tempCol = spriteMat.color;
				tempCol.a = 1 - t;
				if( spriteMat != null )
					spriteMat.color = tempCol;
				sRend.color = tempCol;
				sRend.color = tempCol;
				tempCol = newMat.color;
				tempCol.a = t;
				newMat.color = tempCol;
				float xOffset = Mathf.Repeat( transform.position.x - cam.transform.position.x, newWidth );
				float yOffset = Mathf.Repeat( transform.position.y - cam.transform.position.y, newHeight );
				for( int x = -tileXAmount; x < tileXAmount; x++ ) {
					for( int y = -tileYAmount; y <= tileYAmount && ( tileY || y == -tileYAmount ); y++ ) {
						float xPos = tileX ? cam.transform.position.x + xOffset + newWidth * x : transform.position.x;
						float yPos = tileY ? cam.transform.position.y + yOffset + newHeight * y : transform.position.y;
						Graphics.DrawMesh( spriteMesh, new Vector3( xPos, yPos, transform.position.z - 0.1f ), Quaternion.identity, newMat, gameObject.layer );
					}
				}
				yield return null;
			}
			tempCol.a = 1;
			newMat.color = tempCol;
			sRend.color = tempCol;
			if( spriteMat != null )
				spriteMat = newMat;
			spriteMesh = newMesh;
			width = newWidth;
			height = newHeight;
			this.tileX = tileX;
			this.tileY = tileY;
			this.tileXAmount = tileXAmount;
			this.tileYAmount = tileYAmount;
			sRend.sprite = newSprite;
			isFading = false;
		}
		yield return null;
	}

	public class FadeData {
		public Sprite sprite;
		public bool tileX;
		public bool tileY;
		public int tileXAmount;
		public int tileYAmount;
		public float time;
		public FadeData (  Sprite sprite, bool tileX, bool tileY, int tileXAmount, int tileYAmount, float time ) {
			this.sprite = sprite;
			this.tileX = tileX;
			this.tileY = tileY;
			this.tileXAmount = tileXAmount;
			this.tileYAmount = tileYAmount;
			this.time = time;
		}
	}

	FadeData fadeQueue = null;
	public void FadeSprites ( Sprite newSprite, bool tileX, bool tileY, int tileXAmount, int tileYAmount, float time ) {
		if( isFading )
			fadeQueue = new FadeData( newSprite, tileX, tileY, tileXAmount, tileYAmount, time );
		else
			StartCoroutine( FadeSpritesCoroutine( newSprite, tileX, tileY, tileXAmount, tileYAmount, time ) );
	}

	public void FadeSprites( Sprite newSprite, float time ) {
		FadeSprites( newSprite, tileX, tileY, tileXAmount, tileYAmount, time );
	}

	void Start () {
		if( cam == null )
			cam = Camera.main;
		startPos = transform.position;
		ratio = 1 - Mathf.Pow( 0.9f, distance );
	}

	void CreateSpriteMesh( Sprite sprite, out Mesh mesh, out float width, out float height ) {
//		Sprite sprite = GetComponent<SpriteRenderer>().sprite;
		mesh = new Mesh();
		Vector3[] vertices = new Vector3[4];
		width = sprite.bounds.size.x;
		height = sprite.bounds.size.y;
		Vector3 up = height * Vector3.up;
		Vector3 right = width * Vector3.right;
		vertices[0] = sprite.bounds.min;
		vertices[1] = vertices[0] + up;
		vertices[2] = vertices[0] + up + right;
		vertices[3] = vertices[0] + right;
		int[] triangles = new int[6];
		triangles[0] = 0;
		triangles[1] = 1;
		triangles[2] = 2;
		triangles[3] = 0;
		triangles[4] = 2;
		triangles[5] = 3;
		Vector2[] uv = new Vector2[4];
		uv[0] = new Vector2( sprite.rect.x / sprite.texture.width, sprite.rect.y / sprite.texture.height );
		uv[1] = new Vector2( sprite.rect.x / sprite.texture.width, ( sprite.rect.y + sprite.rect.height ) / sprite.texture.height );
		uv[2] = new Vector2( ( sprite.rect.x + sprite.rect.width ) / sprite.texture.width, ( sprite.rect.y + sprite.rect.height ) / sprite.texture.height );
		uv[3] = new Vector2( ( sprite.rect.x + sprite.rect.width ) / sprite.texture.width, sprite.rect.y / sprite.texture.height );
		mesh.vertices = vertices;
		mesh.triangles = triangles;
		mesh.uv = uv;
		mesh.RecalculateNormals();

	}

	void DrawMeshes ( Camera c ) {
		if( spriteMesh == null ) {
			CreateSpriteMesh( GetComponent<SpriteRenderer>().sprite, out spriteMesh, out width, out height );
			spriteMat = new Material( GetComponent<SpriteRenderer>().sharedMaterial );
			spriteMat.mainTexture = GetComponent<SpriteRenderer>().sprite.texture;
		}
		float xOffset = Mathf.Repeat( transform.position.x - c.transform.position.x, width );
		float yOffset = Mathf.Repeat( transform.position.y - c.transform.position.y, height );
		for( int x = -tileXAmount; x < tileXAmount; x++ ) {
			for( int y = -tileYAmount; y <= tileYAmount && ( tileY || y == -tileYAmount ); y++ ) {
				float xPos = tileX ? c.transform.position.x + xOffset + width * x : transform.position.x;
				float yPos = tileY ? c.transform.position.y + yOffset + height * y : transform.position.y;
				if( Mathf.Approximately( xPos, transform.position.x ) && Mathf.Approximately( yPos, transform.position.y ) )
					continue;
				Graphics.DrawMesh( spriteMesh, new Vector3( xPos, yPos, transform.position.z ), Quaternion.identity, spriteMat, gameObject.layer );
			}
		}
	}

	void LateUpdate () {

		#if UNITY_EDITOR
		if( !UnityEditor.EditorApplication.isPlaying ) {
			if( UnityEditor.SceneView.lastActiveSceneView != null && editorShowMesh && ( tileX || tileY ) )
				DrawMeshes( UnityEditor.SceneView.lastActiveSceneView.camera );
			return;
		}
		#endif
		if( cam == null ) {
			cam = Camera.main;
			if( cam == null )
				return;
		}
		if( !isFading && fadeQueue != null ) {
			StartCoroutine( FadeSpritesCoroutine( fadeQueue.sprite, fadeQueue.tileX, fadeQueue.tileY, fadeQueue.tileXAmount, fadeQueue.tileYAmount, fadeQueue.time ) );
			fadeQueue = null;
		}

		transform.position = startPos + (Vector3)( (Vector2)cam.transform.position - (Vector2)startPos + _offset ) * ratio;
		if( pixelSnap ) {
			PixelPerfectCamera.SnapToPix( transform );
		}
		if( tileX || tileY )
			DrawMeshes( cam );
	}

}
