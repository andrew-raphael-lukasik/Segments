Shader "Segments/World" {
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
            float4 vertexO : POSITION;
            float4 color : COLOR0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };
        struct Varyings {
            float4 vertexW : SV_POSITION;
            float4 color : COLOR0;
            float4 screenPos : TEXCOORD1;
            float worldDepth : TEXCOORD2;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };
        struct geomOut {
            float4 vertexHC : POSITION;
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

        // dithering (Bayer matrix 4x4)
        // src: https://github.com/Unity-Technologies/UnityCsSamples/blob/master/HDRPCustomPasses/Assets/Shaders/Dither.hlsl
        float getbayervalue(float2 screenUV)
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
            OUT.vertexW = float4(TransformObjectToWorld(IN.vertexO.xyz),1);

            // src: https://forum.unity.com/threads/is-there-a-way-to-get-screen-pos-depth-in-shader.1009465/#post-6544999
            float4 clipPos = TransformObjectToHClip( IN.vertexO.xyz );
            OUT.screenPos = ComputeScreenPos(clipPos);

            float3 viewPos = TransformWorldToView( OUT.vertexW.xyz );
            OUT.worldDepth = -viewPos.z;
            
            OUT.color = IN.color;
            
            return OUT;
        }


        [maxvertexcount(4)]
        void geom ( line Varyings IN[2] , inout TriangleStream<geomOut> STREAM )
        {
            Varyings bottom = IN[0];
            Varyings top = IN[1];

            float3 lineVec = top.vertexW.xyz - bottom.vertexW.xyz;
            float3 lineDir = normalize(lineVec);
            float lineLen = length(lineVec);

            float bWidth = lerp( _NearWidth , _FarWidth , remap01(_NearWidthDistance,_FarWidthDistance,bottom.worldDepth) ) * 0.5f;
            float tWidth = lerp( _NearWidth , _FarWidth , remap01(_NearWidthDistance,_FarWidthDistance,top.worldDepth) ) * 0.5f;

            float bAspect = bWidth / ( lineLen + bWidth );
            float tAspect = tWidth / ( lineLen + tWidth );

            // float3 widthDir = cross(lineDir, normalize(_WorldSpaceCameraPos-bottom.vertexW.xyz));
            float3 widthDir = normalize(cross(normalize(_WorldSpaceCameraPos-bottom.vertexW.xyz), lineDir));
            float3 bWidthVec = widthDir * bWidth;
            float3 tWidthVec = widthDir * tWidth;
            
            // quad 1x1, pivot at bottom center
            float bCapWidth = 1.0f/lineLen * bWidth;
            float tCapWidth = 1.0f/lineLen * tWidth;

            float3 blWS = bottom.vertexW.xyz - bWidthVec + lineVec*-bCapWidth;
            float3 brWS = bottom.vertexW.xyz + bWidthVec + lineVec*-bCapWidth;
            float3 tlWS = top.vertexW.xyz - tWidthVec + lineVec*tCapWidth;
            float3 trWS = top.vertexW.xyz + tWidthVec + lineVec*tCapWidth;

            float4 blCS = TransformWorldToHClip(blWS);
            float4 brCS = TransformWorldToHClip(brWS);
            float4 tlCS = TransformWorldToHClip(tlWS);
            float4 trCS = TransformWorldToHClip(trWS);
            
            geomOut vertex;

            // bottom right
            vertex.vertexHC = brCS;
            vertex.color = bottom.color;
            vertex.uv = float3( 1 , 0 , bAspect );
            vertex.screenPos = ComputeScreenPos(brCS);
            vertex.worldDepth = bottom.worldDepth;
            STREAM.Append(vertex);

            // bottom left
            vertex.vertexHC = blCS;
            vertex.color = bottom.color;
            vertex.uv = float3( 0 , 0 , bAspect );
            vertex.screenPos = ComputeScreenPos(blCS);
            vertex.worldDepth = bottom.worldDepth;
            STREAM.Append(vertex);

            // top right
            vertex.vertexHC = trCS;
            vertex.color = top.color;
            vertex.uv = float3( 1 , 1 , tAspect );
            vertex.screenPos = ComputeScreenPos(trCS);
            vertex.worldDepth = top.worldDepth;
            STREAM.Append(vertex);

            // top left
            vertex.vertexHC = tlCS;
            vertex.color = top.color;
            vertex.uv = float3( 0 , 1 , tAspect );
            vertex.screenPos = ComputeScreenPos(tlCS);
            vertex.worldDepth = top.worldDepth;
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

            float ditherValue = getbayervalue(IN.screenPos.xy / IN.screenPos.w);
            col.a -= (1-col.a) * ditherValue * _DitherStrength;

            clip(col.a - _AlphaCutoff);//if( alpha<=0 ) discard;

            return col;
        }

        ENDHLSL
    }
}

    FallBack Off

}
