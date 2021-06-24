Shader "Segments/Rounded Rectangle" {
Properties
{
	_Width ( "Width" , Float ) = 0.1
	
	[MainColor][HDR]
	_Color ( "Color" , Color ) = (0.4,1,0,1)

	_Roundness ( "Roundness" , Range(0,1) ) = 1.0

	_Smoothness ( "Smoothness" , Range(0,1) ) = 0.8

	// [MainTexture] _BaseMap("BaseMap", 2D) = "white" {}
}

SubShader
{
	LOD 200
	Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
	
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
		#include "UnityCG.cginc"
		

		struct vertexIn {
			float4 pos : POSITION;
			float4 color : COLOR;
		};
		struct vertexOut {
			float4 pos : SV_POSITION;
			float4 color : COLOR0;
		};
		struct geomOut {
			float4 pos : POSITION;
			float4 color : COLOR0;
			float4 uv : UV0;
				// uv.xy - uv
				// uv.z - aspect ratio
				// uv.w - depth (clip space)
		};


		uniform float _ArrayVertices [1363*3];
		// uniform float4 _ArrayColors [1363];
		float4 _Color;
		float _Width;
		float _Roundness;
		float _Smoothness;


		#define PI 3.1415926535897932384626433832795
		#define epsilon 0.0000001
		#define sqrt2 1.41421356237

		// src: https://www.shadertoy.com/view/MlGBWD
		float remap01 ( float from , float to , float x ) { return saturate( (x-from)/(to-from) ); }

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
		float3x3 Scale ( float3 scale )
		{
			return float3x3(
				float3( scale.x , scale.x , scale.x ) ,
				float3( scale.y , scale.y , scale.y ) ,
				float3( scale.z , scale.z , scale.z )
			);
		}
		

		vertexOut vert ( uint vId : SV_VertexID )
		{
			vertexOut o;
			int i0 = vId * 3;
			float4 vec = float4( _ArrayVertices[i0] , _ArrayVertices[i0+1] , _ArrayVertices[i0+2] , 1 );
			
			// src: https://forum.unity.com/threads/is-there-a-way-to-get-screen-pos-depth-in-shader.1009465/#post-6544999
			float4 clipPos = UnityObjectToClipPos( vec );
			vec.w = clipPos.z / clipPos.w;// depth
			#if !defined(UNITY_REVERSED_Z) // basically only OpenGL
			vec.w = vec.w * 0.5 + 0.5; // remap -1 to 1 range to 0.0 to 1.0
			#endif
			
			o.pos = vec;
			o.color = 1;//_ArrayColors[vId];
			
			return o;
		}


		[maxvertexcount(4)]
		void geom ( line vertexOut IN[2] , inout TriangleStream<geomOut> stream )
		{
			vertexOut IN0 = IN[0];
			vertexOut IN1 = IN[1];

			float3 cameraPosition = _WorldSpaceCameraPos;
			float3 lineVec = IN1.pos - IN0.pos;
			float3 lineDir = normalize(lineVec);
			float lineLen = length(lineVec);
			float3x3 rot = LookRotation( lineDir , normalize(cameraPosition-IN0.pos) );
			float3 scale = float3( _Width , 1 , lineLen );
			// float3x3 ltw = RS( rot , scale );
			float3x3 ltw = rot * Scale(scale);

			// quad 1x1, pivot at bottom center
			float overlap = _Width;
			float capWidth = 1/lineLen * overlap*0.5;
			float4 bl = UnityObjectToClipPos( IN0.pos + float4( mul( float3(-0.5,0,-capWidth) , ltw ) , 0 ) );
			float4 br = UnityObjectToClipPos( IN0.pos + float4( mul( float3( 0.5,0,-capWidth) , ltw ) , 0 ) );
			float4 tl = UnityObjectToClipPos( IN0.pos + float4( mul( float3(-0.5,0,1+capWidth) , ltw ) , 0 ) );
			float4 tr = UnityObjectToClipPos( IN0.pos + float4( mul( float3( 0.5,0,1+capWidth) , ltw ) , 0 ) );

			float2 depth = pow( float2( IN0.pos.w , IN1.pos.w ) , 0.3 );
			depth = float2( IN0.pos.w , IN1.pos.w );

			geomOut VERT;
			float aspect = _Width / ( lineLen + overlap );

			// bottom right
			VERT.pos = br;
			VERT.color = IN0.color;
			VERT.uv = float4( 1 , 0 , aspect , depth.x );
				stream.Append(VERT);

			// bottom left
			VERT.pos = bl;
			VERT.color = IN0.color;
			VERT.uv = float4( 0 , 0 , aspect , depth.x );
				stream.Append(VERT);

			// top right
			VERT.pos = tr;
			VERT.color = IN1.color;
			VERT.uv = float4( 1 , 1 , aspect , depth.y );
				stream.Append(VERT);

			// top left
			VERT.pos = tl;
			VERT.color = IN1.color;
			VERT.uv = float4( 0 , 1 , aspect , depth.y );
				stream.Append(VERT);
		}


		float4 frag ( geomOut i ) : COLOR
		{
			float margin = _Roundness * 0.5;
			float aspect = i.uv.z;
			float depth = i.uv.w;

			float2 ruv = abs( i.uv - 0.5 );
			float rw = 0.5 - margin;
			float rh = 0.5 - margin * aspect;
			float dx = ruv.x - rw;
			float dy = max( ruv.y - rh , 0 );
			float a12 = min( 1-( dx / max(0.5-rw,epsilon) ) , 1-( dy / max(0.5-rh,epsilon) ) );
			float a3 = 1 - saturate( length( float2( ruv.x , ruv.y/aspect ) - float2( 0.5 - margin , 0.5*1/aspect - margin ) ) / margin );
			
			float case3 = ruv.x>rw & ruv.y>rh;// corner margins
			float a = case3 ? a3 : a12;
			
			float t = easeOutCirc(a);
			t = t/_Smoothness + step(_Smoothness,t);
			
			float4 col = saturate( i.color * _Color * float4(1,1,1,t) );
			// return depth;

			if( t<=0 ) discard;
			return col;
		}

		
		ENDCG
	}
}
	FallBack Off
}
