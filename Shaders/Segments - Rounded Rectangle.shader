Shader "Segments/Rounded Rectangle" {
Properties
{
    _Roundness ( "Shape Roundness" , Range(0,1) ) = 1.0
    _Smoothness ( "Shape Smoothness" , Range(0,1) ) = 1.0

    [Header(Width)]
    _NearWidth ( "Near Line Width" , Float ) = 0.25
    _NearWidthDistance ( "Near Width Distance" , Float ) = 200.0
    _FarWidth ( "Far Line Width" , Float ) = 7.0
    _FarWidthDistance ( "Far Width Distance" , Float ) = 1000.0

    [Header(Color)]
    [MainColor][HDR]_Color ( "Near Color" , Color ) = (0,0,0,1)
    _NearColorDistance ( "Near Color Distance" , Float ) = 1.0

    [HDR]_ColorFar ( "Far Color" , Color ) = (1,1,1,1)
    _FarColorDistance ( "Far Color Distance" , Float) = 50.0

    [Header(Texture)]
    [Toggle] _Texture ("Enabled", Float) = 0
    [MainTexture] _MainTex( "Texture" , 2D ) = "white" {}

    [Header(Alpha Test)]
    _AlphaCutoff ( "Alpha Cutoff" , Range(0,1) ) = 0.01
    _AlphaPow ( "Alpha Pow()" , Float ) = 5.5
    _NearCutoffDistane( "Near Cutoff Distance" , Float ) = 0.5
    _FarCutoffDistaneStart( "Far Cutoff Distance Start" , Float ) = 500
    _FarCutoffDistaneEnd( "Far Cutoff Distance End" , Float ) = 1000
    _DitherStrength( "Dither Strength" , Float ) = 1.0
}

SubShader
{
    Tags { "RenderType" = "TransparentCutout" "RenderPipeline" = "UniversalPipeline" }

    ZWrite On
    ZTest Less
    Cull Off

    Pass
    {
        HLSLPROGRAM
        #pragma vertex vert
        #pragma geometry geom
        #pragma fragment frag
        #pragma require geometry
        #pragma target 4.5
        // #pragma multi_compile_instancing
        // #pragma instancing_options renderinglayer
        #pragma multi_compile _TEXTURE _TEXTURE_ON
        #pragma multi_compile _ DOTS_INSTANCING_ON

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        // #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        // #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

        struct Attributes {
            float4 vertex : POSITION;
            float4 color : COLOR0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };
        struct Varyings {
            float4 vertex : SV_POSITION;
            float4 color : COLOR0;
            float4 screenPos : TEXCOORD1;
            float worldDepth : TEXCOORD2;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };
        struct geomOut {
            float4 vertex : POSITION;
            float4 color : COLOR0;
            float3 uv : TEXCOORD0;
                // uv.xy - uv
                // uv.z - aspect ratio
            float4 screenPos : TEXCOORD1;
            float worldDepth : TEXCOORD2;
        };

        #ifdef _TEXTURE_ON
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
        #endif

        CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            float4 _ColorFar;
            float _NearWidth;
            float _NearWidthDistance;
            float _FarWidth;
            float _FarWidthDistance;
            float _Roundness;
            float _Smoothness;
            float _NearColorDistance;
            float _FarColorDistance;
            float4 _MainTex_ST;
            float _AlphaCutoff;
            float _AlphaPow;
            float _NearCutoffDistane;
            float _FarCutoffDistaneStart;
            float _FarCutoffDistaneEnd;
            float _DitherStrength;
        CBUFFER_END
        
#ifdef UNITY_DOTS_INSTANCING_ENABLED
        UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
            UNITY_DOTS_INSTANCED_PROP(float4, _Color)
            UNITY_DOTS_INSTANCED_PROP(float4, _ColorFar)
        UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)
#endif

        #define epsilon 0.0000001
        #define sqrt2 1.41421356237

        float remap01 ( float from , float to , float x ) { return saturate( (x-from)/(to-from) ); }
        float2 remap01 ( float2 from , float2 to , float2 x ) { return saturate( (x-from)/(to-from) ); }
        float remap ( float from , float to , float x ) { return (x-from)/(to-from); }
        float2 remap ( float2 from , float2 to , float2 x ) { return (x-from)/(to-from); }
        float inverselerp ( float from , float to , float x ) { return remap(from,to,x); }
        float2 inverselerp ( float2 from , float2 to , float2 x ) { return remap(from,to,x); }

        float lengthSq ( float2 vec ) { return dot( vec , vec ); }

        
        float easeOutCirc ( float x ) { return sqrt( 1.0 - pow(x-1.0,2.0) ); }// src: https://easings.net/#easeOutCirc
        float easeOutQuad( float x ) { return 1 - (1 - x) * (1 - x); }// src: https://easings.net/#easeOutQuad

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

        // dithering (Bayer matrix 4x4)
        // src: https://github.com/Unity-Technologies/UnityCsSamples/blob/master/HDRPCustomPasses/Assets/Shaders/Dither.hlsl
        float GetBayerValue(float2 screenUV)
        {
            const float bayerMatrix[16] = {
                0.0 / 16.0,  8.0 / 16.0,  2.0 / 16.0, 10.0 / 16.0,
                12.0 / 16.0,  4.0 / 16.0, 14.0 / 16.0,  6.0 / 16.0,
                3.0 / 16.0, 11.0 / 16.0,  1.0 / 16.0,  9.0 / 16.0,
                15.0 / 16.0,  7.0 / 16.0, 13.0 / 16.0,  5.0 / 16.0
            };
            float2 pixelCoord = fmod(floor(screenUV * _ScaledScreenParams.xy), 4.0);
            int index = int(pixelCoord.x + pixelCoord.y * 4.0);
            return bayerMatrix[index];
        }


        Varyings vert ( Attributes IN )
        {
            Varyings OUT;
            
            UNITY_SETUP_INSTANCE_ID( IN );
            UNITY_TRANSFER_INSTANCE_ID( IN , OUT );
            OUT.vertex = float4(TransformObjectToWorld(IN.vertex.xyz),1);

            // src: https://forum.unity.com/threads/is-there-a-way-to-get-screen-pos-depth-in-shader.1009465/#post-6544999
            float4 clipPos = TransformObjectToHClip( IN.vertex.xyz );
            OUT.screenPos = ComputeScreenPos(clipPos);

            float3 viewPos = TransformWorldToView( OUT.vertex.xyz );
            OUT.worldDepth = -viewPos.z;
            
            OUT.color = IN.color;
            
            return OUT;
        }


        [maxvertexcount(4)]
        void geom ( line Varyings IN[2] , inout TriangleStream<geomOut> STREAM )
        {
            Varyings IN0 = IN[0];
            Varyings IN1 = IN[1];

            float3 lineVec = IN1.vertex.xyz - IN0.vertex.xyz;
            float3 lineDir = normalize(lineVec);
            float2 lineLen = (float2) length(lineVec);

            float2 bWidth = lerp( _NearWidth , _FarWidth , remap01(_NearWidthDistance,_FarWidthDistance,IN0.worldDepth) );
            float2 tWidth = lerp( _NearWidth , _FarWidth , remap01(_NearWidthDistance,_FarWidthDistance,IN1.worldDepth) );

            float2 bOverlap = bWidth;
            float2 tOverlap = tWidth;
            float2 bAspect = bWidth / ( lineLen + bOverlap );
            float2 tAspect = tWidth / ( lineLen + tOverlap );
            float3 bScale = float3( bWidth.x , 1 , lineLen.x );
            float3 tScale = float3( tWidth.y , 1 , lineLen.y );
            float3x3 rot = LookRotation(
                lineDir ,
                normalize(_WorldSpaceCameraPos-IN0.vertex)
            );
            float3x3 bltw = rot * S(bScale);
            float3x3 tltw = rot * S(tScale);

            // quad 1x1, pivot at bottom center
            float2 bCapWidth = float2(1,1)/lineLen * bOverlap*float2(0.5,0.5);
            float2 tCapWidth = float2(1,1)/lineLen * tOverlap*float2(0.5,0.5);
            float4 bl = TransformWorldToHClip( IN0.vertex.xyz + float3( mul( float3(-0.5,0,-bCapWidth.x) , bltw ) ) );
            float4 br = TransformWorldToHClip( IN0.vertex.xyz + float3( mul( float3( 0.5,0,-bCapWidth.x) , bltw ) ) );
            float4 tl = TransformWorldToHClip( IN0.vertex.xyz + float3( mul( float3(-0.5,0,1+tCapWidth.y) , tltw ) ) );
            float4 tr = TransformWorldToHClip( IN0.vertex.xyz + float3( mul( float3( 0.5,0,1+tCapWidth.y) , tltw ) ) );
            
            geomOut vertex;

            // bottom right
            vertex.vertex = br;
            vertex.color = IN0.color;
            vertex.uv = float3( 1 , 0 , bAspect.x );
            vertex.screenPos = ComputeScreenPos(br);
            vertex.worldDepth = IN0.worldDepth;
            STREAM.Append(vertex);

            // bottom left
            vertex.vertex = bl;
            vertex.color = IN0.color;
            vertex.uv = float3( 0 , 0 , bAspect.x );
            vertex.screenPos = ComputeScreenPos(bl);
            vertex.worldDepth = IN0.worldDepth;
            STREAM.Append(vertex);

            // top right
            vertex.vertex = tr;
            vertex.color = IN1.color;
            vertex.uv = float3( 1 , 1 , tAspect.y );
            vertex.screenPos = ComputeScreenPos(tr);
            vertex.worldDepth = IN1.worldDepth;
            STREAM.Append(vertex);

            // top left
            vertex.vertex = tl;
            vertex.color = IN1.color;
            vertex.uv = float3( 0 , 1 , tAspect.y );
            vertex.screenPos = ComputeScreenPos(tl);
            vertex.worldDepth = IN1.worldDepth;
            STREAM.Append(vertex);
        }


        float4 frag ( geomOut IN ) : COLOR
        {
            float margin = _Roundness * 0.5;
            float aspect = IN.uv.z;
            float depth = IN.worldDepth;
            // return float4(depth,depth,depth,1);

            float2 ruv = abs( IN.uv.xy - 0.5 );
            float rw = 0.5 - margin;
            float rh = 0.5 - margin * aspect;
            float dx = ruv.x - rw;
            float dy = max( ruv.y - rh , 0 );
            float a12 = min( 1-( dx / max(0.5-rw,epsilon) ) , 1-( dy / max(0.5-rh,epsilon) ) );
            float a3 = 1 - saturate( length( float2( ruv.x , ruv.y/aspect ) - float2( 0.5 - margin , 0.5*1/aspect - margin ) ) / margin );
            
            float case3 = ruv.x>rw & ruv.y>rh;// corner margins
            float alphaRaw = case3 ? a3 : a12;

            float alpha_mul = remap01( 0 , _Smoothness , easeOutCirc(alphaRaw) );
            float depth_t = remap01( _NearColorDistance , _FarColorDistance , depth );
            float4 col = saturate( IN.color * lerp(_Color, _ColorFar, depth_t) * float4(1,1,1,alpha_mul) );

            #ifdef _TEXTURE_ON
                IN.uv.xy = TRANSFORM_TEX( IN.uv.xy , _MainTex );
                float4 texCol = SAMPLE_TEXTURE2D( _MainTex , sampler_MainTex , IN.uv.xy );
                col *= texCol;
            #endif

            // if( depth > _FarCutoffDistaneStart )// attempt to make lines disappear in less noisy way when very thin
            // {
            //     float t = easeOutQuad(remap01(_FarCutoffDistaneStart,_FarCutoffDistaneEnd,depth));
            //     _AlphaPow = lerp( _AlphaPow , 1 , t );
            //     _DitherStrength = lerp( _DitherStrength , 0.1 , t );
            // }

            col.a = pow( col.a , _AlphaPow );

            if( depth < _NearCutoffDistane )
            {
                col.a *= remap01( 0 , _NearCutoffDistane , depth );
            }
            else if( depth > _FarCutoffDistaneStart )
            {
                col.a *= easeOutQuad(remap01( _FarCutoffDistaneEnd , _FarCutoffDistaneStart , depth ));
            }

            float ditherValue = GetBayerValue(IN.screenPos.xy / IN.screenPos.w);
            col.a -= (1-col.a) * ditherValue * _DitherStrength;

            clip(col.a - _AlphaCutoff);//if( alpha<=0 ) discard;

            return col;
        }

        ENDHLSL
    }
}

    FallBack Off

}
