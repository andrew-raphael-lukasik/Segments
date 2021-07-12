Shader "Segments/Rounded Rectangle" {
Properties
{
	_Width ( "Width" , Float ) = 0.1
	_Roundness ( "Roundness" , Range(0,1) ) = 1.0
	_Smoothness ( "Smoothness" , Range(0,1) ) = 0.8

	[MainColor][Header(NEAR)]
	_Color ( "Color" , Color ) = (0.4,1,0,1)
	// _Width ( "Width" , Range(0,1) ) = 0.1
	_DepthNear ( "Depth" , Range(0,1) ) = 0.99

	[Header(FAR)]
	_ColorFar ( "Color" , Color ) = (0.4,1,0,1)
	// _WidthFar ( "Width" , Range(0,1) ) = 0.01
	_DepthFar ( "Depth" , Range(0,1) ) = 1.0

	[Header(Texture)]
	[Toggle] _Texture ("Enabled", Float) = 0
	[MainTexture] _MainTex( "Texture" , 2D ) = "white" {}
}

SubShader
{
	LOD 200
	Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
	
	BlendOp Add
	Blend SrcAlpha OneMinusSrcAlpha
	ZWrite Off
	ZTest Less

	Pass
	{
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#pragma geometry geom
		#pragma require geometry
		// #pragma target 4.0
		#pragma target 4.5
		#pragma multi_compile _TEXTURE _TEXTURE_ON
		#include "UnityCG.cginc"
		

		struct vertexIn {
			float4 vertex : POSITION;
			float4 color : COLOR;
		};
		struct vertexOut {
			float4 vertex : SV_POSITION;
			float4 color : COLOR0;
		};
		struct geomOut {
			float4 vertex : POSITION;
			float4 color : COLOR0;
			float4 uv : UV0;
				// uv.xy - uv
				// uv.z - aspect ratio
				// uv.w - depth (clip space)
		};


		// uniform float _ArrayVertices [1363*3];
		// uniform float4 _ArrayColors [1363];
		float4 _Color;
		float _Width;
		// float _WidthFar;
		float _Roundness;
		float _Smoothness;
		float _DepthNear;
		float _DepthFar;
		float4 _ColorFar;

		#ifdef _TEXTURE_ON
			sampler2D _MainTex;
			float4 _MainTex_ST;
		#endif


		#define PI 3.1415926535897932384626433832795
		#define epsilon 0.0000001
		#define sqrt2 1.41421356237

		float remap01 ( float from , float to , float x ) { return saturate( (x-from)/(to-from) ); }
		float2 remap01 ( float2 from , float2 to , float2 x ) { return saturate( (x-from)/(to-from) ); }
		float remap ( float from , float to , float x ) { return (x-from)/(to-from); }
		float2 remap ( float2 from , float2 to , float2 x ) { return (x-from)/(to-from); }
		float inverselerp ( float from , float to , float x ) { return remap(from,to,x); }
		float2 inverselerp ( float2 from , float2 to , float2 x ) { return remap(from,to,x); }

		float lengthSq ( float2 vec ) { return dot( vec , vec ); }

		// src: https://easings.net/#easeOutCirc
		float easeOutCirc ( float x ) { return sqrt( 1.0 - pow(x-1.0,2.0) ); }

		// src: https://github.com/Unity-Technologies/Unity.Mathematics/blob/7da8f190d976ab687187eaeb3d42408e7f606667/src/Unity.Mathematics/matrix.cs#L436
		float3x3 LookRotation ( float3 forward , float3 up )
		{
			float3 t = normalize( cross(up,forward) );
			return float3x3( t , cross(forward,t) , forward );
		}

		// src: https://github.com/Unity-Technologies/Unity.Mathematics/blob/7da8f190d976ab687187eaeb3d42408e7f606667/src/Unity.Mathematics/matrix.cs#L1009
		float4x4 TRS ( float3 translation , float3x3 rotation , float3 scale )
		{
			return float4x4(
				float4( rotation[0]*scale.x , 0.0 ) ,
				float4( rotation[1]*scale.y , 0.0 ) ,
				float4( rotation[2]*scale.z , 0.0 ) ,
				float4( translation , 1.0 )
			);
		}
		float3x3 RS ( float3x3 rotation , float3 scale )
		{
			return float3x3(
				float3( rotation[0]*scale.x ) ,
				float3( rotation[1]*scale.y ) ,
				float3( rotation[2]*scale.z )
			);
		}
		float3x3 S ( float3 scale )
		{
			return float3x3(
				float3( scale.x , scale.x , scale.x ) ,
				float3( scale.y , scale.y , scale.y ) ,
				float3( scale.z , scale.z , scale.z )
			);
		}


		vertexOut vert ( vertexIn i )
		{
			vertexOut o;
			float4 vec = i.vertex;
			
			// src: https://forum.unity.com/threads/is-there-a-way-to-get-screen-pos-depth-in-shader.1009465/#post-6544999
			float4 clipPos = UnityObjectToClipPos( vec );
			vec.w = clipPos.z / clipPos.w;// depth
			#if !defined(UNITY_REVERSED_Z)// if OpenGL
				vec.w = vec.w * 0.5 + 0.5;// remap -1 to 1 range to 0.0 to 1.0
			#endif
			
			o.vertex = vec;
			o.color = i.color;
			
			return o;
		}


		[maxvertexcount(4)]
		void geom ( line vertexOut IN[2] , inout TriangleStream<geomOut> STREAM )
		{
			vertexOut IN0 = IN[0];
			vertexOut IN1 = IN[1];

			float3 cameraPosition = _WorldSpaceCameraPos;
			float3 lineVec = IN1.vertex - IN0.vertex;
			float3 lineDir = normalize(lineVec);
			float2 lineLen = (float2) length(lineVec);
			float2 depth = remap(
				(float2) lerp( 1 , 0 , easeOutCirc(_DepthFar) ) ,
				(float2) lerp( 1 , 0 , easeOutCirc(_DepthNear) ) ,
				float2( max(IN0.vertex.w,0) , IN1.vertex.w )
			);
			depth = min( max( depth , 0 ) , 1 );

			float2 width = _Width;// float2 width = lerp( (float2)_WidthFar , (float2)_Width , depth );
			float2 overlap = width;
			float2 aspect = width / ( lineLen + overlap );
			float3 bscale = float3( width.x , 1 , lineLen.x );
			float3 tscale = float3( width.y , 1 , lineLen.y );
			float3x3 rot = LookRotation( lineDir , normalize(cameraPosition-IN0.vertex) );
			float3x3 bltw = rot * S(bscale);
			float3x3 tltw = rot * S(tscale);

			// quad 1x1, pivot at bottom center
			float2 capWidth = float2(1,1)/lineLen * overlap*float2(0.5,0.5);
			float4 bl = UnityObjectToClipPos( IN0.vertex + float4( mul( float3(-0.5,0,-capWidth.x) , bltw ) , 0 ) );
			float4 br = UnityObjectToClipPos( IN0.vertex + float4( mul( float3( 0.5,0,-capWidth.x) , bltw ) , 0 ) );
			float4 tl = UnityObjectToClipPos( IN0.vertex + float4( mul( float3(-0.5,0,1+capWidth.y) , tltw ) , 0 ) );
			float4 tr = UnityObjectToClipPos( IN0.vertex + float4( mul( float3( 0.5,0,1+capWidth.y) , tltw ) , 0 ) );
			
			geomOut vertex;

			// bottom right
			vertex.vertex = br;
			vertex.color = IN0.color;
			vertex.uv = float4( float2(1,0) , aspect.x , depth.x );
			STREAM.Append(vertex);

			// bottom left
			vertex.vertex = bl;
			vertex.color = IN0.color;
			vertex.uv = float4( float2(0,0) , aspect.x , depth.x );
			STREAM.Append(vertex);

			// top right
			vertex.vertex = tr;
			vertex.color = IN1.color;
			vertex.uv = float4( float2(1,1) , aspect.y , depth.y );
			STREAM.Append(vertex);

			// top left
			vertex.vertex = tl;
			vertex.color = IN1.color;
			vertex.uv = float4( float2(0,1) , aspect.y , depth.y );
			STREAM.Append(vertex);
		}


		float4 frag ( geomOut i ) : COLOR
		{
			float margin = _Roundness * 0.5;
			float aspect = i.uv.z;
			float depth = i.uv.w;
			// return float4(depth,depth,depth,1);

			float2 ruv = abs( i.uv - 0.5 );
			float rw = 0.5 - margin;
			float rh = 0.5 - margin * aspect;
			float dx = ruv.x - rw;
			float dy = max( ruv.y - rh , 0 );
			float a12 = min( 1-( dx / max(0.5-rw,epsilon) ) , 1-( dy / max(0.5-rh,epsilon) ) );
			float a3 = 1 - saturate( length( float2( ruv.x , ruv.y/aspect ) - float2( 0.5 - margin , 0.5*1/aspect - margin ) ) / margin );
			
			float case3 = ruv.x>rw & ruv.y>rh;// corner margins
			float alphaRaw = case3 ? a3 : a12;
			
			float alpha = remap01( 0 , _Smoothness , easeOutCirc(alphaRaw) );
			float4 col = saturate( i.color * lerp(_ColorFar,_Color,depth) * float4(1,1,1,alpha) );

			#ifdef _TEXTURE_ON
				i.uv.xy = TRANSFORM_TEX( i.uv.xy , _MainTex );
				float4 texCol = tex2D( _MainTex , i.uv.xy );
				col *= texCol;
				if( col.a<=0 ) discard;
			#else
				if( alpha<=0 ) discard;
			#endif

			return col;
		}

		
		ENDCG
	}
}
	FallBack Off
}
