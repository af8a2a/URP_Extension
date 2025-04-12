Shader "Glass"
{
    Properties
    {
        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
        _Intensity("Intensity", Range(0.0, 1.0)) = 0.5
        _Transparency("Transparency", Range(0.0, 1.0)) = 0.5
    }
    HLSLINCLUDE
    #define SAMPLE_TEXTURE2D
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GlobalSamplers.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"

    float _Intensity;
    float _Transparency;
    // float _Radius;
    TEXTURE2D(_BlurTexture);

    struct Attributes
    {
        float4 positionOS : POSITION;
        float3 normalOS : NORMAL;
        float4 tangentOS : TANGENT;
        float2 texcoord : TEXCOORD0;
        float2 staticLightmapUV : TEXCOORD1;
        float2 dynamicLightmapUV : TEXCOORD2;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct Varyings
    {
        float2 uv : TEXCOORD0;
        float3 positionWS : TEXCOORD1;
        float3 normalWS : TEXCOORD2;
        half4 tangentWS : TEXCOORD3; // xyz: tangent, w: sign
        half3 viewDirTS : TEXCOORD7;
        float4 positionCS : SV_POSITION;
    };

    Varyings LitPassVertex(Attributes input)
    {
        Varyings output = (Varyings)0;

        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_TRANSFER_INSTANCE_ID(input, output);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

        VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);

        // normalWS and tangentWS already normalize.
        // this is required to avoid skewing the direction during interpolation
        // also required for per-vertex lighting and SH evaluation
        VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
        output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);

        // already normalized from normal transform to WS.
        output.normalWS = normalInput.normalWS;
        real sign = input.tangentOS.w * GetOddNegativeScale();
        half4 tangentWS = half4(normalInput.tangentWS.xyz, sign);
        output.tangentWS = tangentWS;

        half3 viewDirWS = GetWorldSpaceNormalizeViewDir(vertexInput.positionWS);
        half3 viewDirTS = GetViewDirectionTangentSpace(tangentWS, output.normalWS, viewDirWS);
        output.viewDirTS = viewDirTS;


        output.positionWS = vertexInput.positionWS;


        output.positionCS = vertexInput.positionCS;

        return output;
    }

    half3 DecodeHDR(half4 color)
    {
        #if UNITY_COLORSPACE_GAMMA
            color.xyz *= color.xyz; // γ to linear
        #endif

        #if _USE_RGBM
            return DecodeRGBM(color);
        #else
        return color.xyz;
        #endif
    }

    half4 EncodeHDR(half3 color)
    {
        #if _USE_RGBM
            half4 outColor = EncodeRGBM(color);
        #else
        half4 outColor = half4(color, 1.0);
        #endif

        #if UNITY_COLORSPACE_GAMMA
            return half4(sqrt(outColor.xyz), outColor.w); // linear to γ
        #else
        return outColor;
        #endif
    }


    half4 Frag(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        float4x4 thresholdMatrix =
        {
            1.0 / 17.0, 9.0 / 17.0, 3.0 / 17.0, 11.0 / 17.0,
            13.0 / 17.0, 5.0 / 17.0, 15.0 / 17.0, 7.0 / 17.0,
            4.0 / 17.0, 12.0 / 17.0, 2.0 / 17.0, 10.0 / 17.0,
            16.0 / 17.0, 8.0 / 17.0, 14.0 / 17.0, 6.0 / 17.0
        };


        float4x4 _RowAccess = {1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1};
        float2 pos = input.positionCS.xy;
        pos *= _ScreenParams.xy; // pixel position
        // pos /= 16;
        clip(_Transparency - thresholdMatrix[fmod(pos.x, 4)] * _RowAccess[fmod(pos.y, 4)]);

        float2 uv = UnityStereoTransformScreenSpaceTex(input.uv);

        float4 base_color = SAMPLE_TEXTURE2D(_BaseMap, sampler_LinearClamp, uv);
        float2 screenspace_uv = GetNormalizedScreenSpaceUV(input.positionCS);
        float4 blur_color = SAMPLE_TEXTURE2D(_BlurTexture, sampler_LinearClamp, screenspace_uv);
        half4 color = lerp(base_color, base_color * blur_color, _Intensity);
        return EncodeHDR(color);
    }
    ENDHLSL

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "LightMode" = "UniversalForward"
        }
        LOD 100

        UsePass "Universal Render Pipeline/Lit/SHADOWCASTER"

        Pass
        {
            Name "Frag"
            Tags
            {
                "RenderPipeline" = "UniversalPipeline"
                "LightMode" = "UniversalForward"
            }

            HLSLPROGRAM
            #pragma vertex LitPassVertex
            #pragma fragment Frag
            ENDHLSL
        }


    }
}