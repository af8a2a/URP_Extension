Shader "WhiteNoise"
{

    HLSLINCLUDE
    #pragma exclude_renderers gles


    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

    float rand(float2 uv, float t)
    {
        return frac(sin(dot(uv, float2(1225.6548, 321.8942))) * 4251.4865 + t);
    }

    float _Intensity;
    half4 Frag_Flim(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);
        float scale = 50.0;
        float2 noise = (rand(uv, _Time.y) - 0.5) * 2.0 * rcp(_ScreenParams.xy) * scale;
        half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
        
        return lerp(color,color+ noise.r, _Intensity);
    }
    ENDHLSL




    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 100


        Pass
        {
            Name "Flim"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag_Flim
            ENDHLSL
        }

    }
}