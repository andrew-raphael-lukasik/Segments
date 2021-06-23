Shader "Segments/Line Shader v1" {
Properties
{
	_Width ( "Width" , Float ) = 0.5//_Width ("Width", Range(0.005,0.05)) = 0.01
	_Overlap ( "Overlap" , Float ) = 0.5
	
	[MainColor][HDR]
	_Color ( "Color" , Color ) = (1,1,1,1)

	[HDR]
	_Color1 ( "Border Color" , Color ) = (1,1,1,0)

	_BorderSize ( "Border Size" , Range(0,1) ) = 0.1

	_Sharpness ( "Sharpness" , Range(0,1) ) = 0

	// [MainTexture] _BaseMap("BaseMap", 2D) = "white" {}
}

SubShader
{
	LOD 200
	Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
	BlendOp Max
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
			float2 uv : UV0;
			float aspect : UV2;// aspect ratio
			float3 localcs_pos : UV3;// relative to v0
			float3 localcs_v1 : UV4;// v1, relative to v0
		};


		uniform float _ArrayVertices [1363*3];
		// uniform float4 _ArrayColors [1363];
		float4 _Color;
		float4 _Color1;
		float _Width;
		float _Overlap;
		float _Sharpness;
		float _BorderSize;


		// src: https://www.shadertoy.com/view/MlGBWD
		float remap01 ( float from , float to , float x ) { return saturate( (x-from)/(to-from) ); }

		float lengthSq ( float2 vec ) { return dot( vec , vec ); }

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


		// vertexOut vert ( vertexIn IN )
		// {
		// 	vertexOut o;
		// 	o.pos = IN.pos;
		// 	o.color = IN.color;
		// 	return o;
		// }
		vertexOut vert ( uint vId : SV_VertexID )
		{
			vertexOut o;
			int i0 = vId * 3;
			o.pos = float4( _ArrayVertices[i0] , _ArrayVertices[i0+1] , _ArrayVertices[i0+2] , 1 );
			o.color = 1;//_ArrayColors[vId];
			
			// float4 cameraLocalPos = mul( unity_WorldToObject , float4(_WorldSpaceCameraPos,1.0) );
			// o.pos.w = 1.0 - saturate( max( lengthSq(o.pos.xyz,_WorldSpaceCameraPos.xyz)*0.01 , 0 ) );

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
			float capWidth = 1/lineLen * _Overlap*0.5;
			float4 bl = UnityObjectToClipPos( IN0.pos + float4( mul( float3(-0.5,0,-capWidth) , ltw ) , 0 ) );
			float4 br = UnityObjectToClipPos( IN0.pos + float4( mul( float3( 0.5,0,-capWidth) , ltw ) , 0 ) );
			float4 tl = UnityObjectToClipPos( IN0.pos + float4( mul( float3(-0.5,0,1+capWidth) , ltw ) , 0 ) );
			float4 tr = UnityObjectToClipPos( IN0.pos + float4( mul( float3( 0.5,0,1+capWidth) , ltw ) , 0 ) );

			geomOut VERT;
			VERT.aspect = _Width / ( lineLen + _Overlap );
			// VERT.aspect = _Width / lineLen;

			float4 localcs_v0 = UnityObjectToClipPos( IN0.pos );
			VERT.localcs_v1 = UnityObjectToClipPos( IN1.pos ) - localcs_v0;
			
			// bottom right
			VERT.pos = br;
			VERT.localcs_pos = VERT.pos - localcs_v0;
			VERT.color = IN0.color;
			VERT.uv = float2(1,0);
				stream.Append(VERT);

			// bottom left
			VERT.pos = bl;
			VERT.localcs_pos = VERT.pos - localcs_v0;
			VERT.color = IN0.color;
			VERT.uv = float2(0,0);
				stream.Append(VERT);

			// top right
			VERT.pos = tr;
			VERT.localcs_pos = VERT.pos - localcs_v0;
			VERT.color = IN1.color;
			VERT.uv = float2(1,1);
				stream.Append(VERT);

			// top left
			VERT.pos = tl;
			VERT.localcs_pos = VERT.pos - localcs_v0;
			VERT.color = IN1.color;
			VERT.uv = float2(0,1);
				stream.Append(VERT);
		}


		float4 frag_1 ( geomOut i )
		{
			float t = abs( i.uv - float2(0.5,0) ) * 2.0;
			t = pow( smoothstep(0,1,t) , lerp(1,10,_Sharpness) );
			// return 1.0 - t;
			float4 col = i.color * lerp( _Color , _Color1 , t );
			// float4 col = i.color;
			// col.a *= smoothstep( 1.0 , 0 , t );
			return col;
		}
		float4 frag_draw_local_pos ( geomOut i )
		{
			return float4( i.localcs_pos , 1 );
		}
		float4 frag_uv_method ( geomOut i )
		{
			const float epsilon = 0.0000001;
			const float sqrt2 = 1.41421356237;
			float margin = _BorderSize*0.5;

			float2 ruv = abs( i.uv - 0.5 );
			float rw = 0.5 - margin;
			float rh = 0.5 - margin * i.aspect;
			float dx = ruv.x - rw;
			float dy = max( ruv.y - rh , 0 );
			float a12 = min( 1-( dx / max(0.5-rw,epsilon) ) , 1-( dy / max(0.5-rh,epsilon) ) );
			float a3 = 1 - saturate(
				length(
					abs(float2(i.uv.x,i.uv.y/i.aspect)-float2(0.5,0.5)) - (0.5-margin)
				)
				/ margin
			);
			
			// float case1 = ruv.x>rw;// left & right margins
			// float case2 = ruv.y>rh;// top & bottom margins
			float case3 = ruv.x>rw & ruv.y>rh;// corner margins

			float a = case3 ? a3 : a12;
			// return float4( i.uv.x , i.uv.y , 0 , 1 );
			
			// a = smoothstep( 0 , 1 , a );
			float t = pow( a , lerp(1,10,_Sharpness) );
			float4 col = i.color * lerp( _Color1 , _Color , t );
			return col;
		}
		float4 frag ( geomOut i ) : COLOR
		{
			// return frag_1(i);
			return frag_uv_method(i);
		}

		
		ENDCG
	}
}
	FallBack Off
}
