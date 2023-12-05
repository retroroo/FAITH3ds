Shader "2D/Unlit TileLayer" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Tilesize("Tile Size", Float) = 16
		[MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
		_Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Lambert vertex:vert fullforwardshadows
		#pragma multi_compile _ PIXELSNAP_ON
		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _MainTex;
		float4 _MainTex_TexelSize;

		struct Input {
			float2 uv_MainTex;
			float2 vPos;
		};

		fixed4 _Color;
		float _Tilesize;
		fixed _Cutoff;
		
		void vert (inout appdata_full v, out Input o)
		{
			#if defined(PIXELSNAP_ON)
			v.vertex = UnityPixelSnap (v.vertex);
			#endif
			UNITY_INITIALIZE_OUTPUT(Input, o);
			o.vPos = v.vertex.xy;
		}
		
		void surf (Input IN, inout SurfaceOutput o) {
			
			float2 deriv = IN.vPos * _MainTex_TexelSize.xy * _Tilesize;
			float2 uv = (floor(round(IN.uv_MainTex * _MainTex_TexelSize.zw) / (_Tilesize+1)) + float2(0.0625,0.0625) + (frac(IN.vPos)*float2(13.0/15.0,13.0/15.0))) * _MainTex_TexelSize.xy * (_Tilesize+1);
		
			// Albedo comes from a texture tinted by color
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex, ddx( deriv ), ddy( deriv )) * _Color;
			clip(c.a - _Cutoff);
			o.Albedo = c.rgb;
			o.Alpha = c.a;
		}
		ENDCG
	} 
	FallBack "Diffuse"
}
