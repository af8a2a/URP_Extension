#ifndef KAWASE_BLUR
#define KAWASE_BLUR

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"//函数库：主要用于各种的空间变换

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"


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


half4 FragBlurH(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    float texelSize = _BlitTexture_TexelSize.x * 2.0;
    float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);

    // 9-tap gaussian blur on the downsampled source
    half3 c0 = DecodeHDR(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - float2(texelSize * 4.0, 0.0)));
    half3 c1 = DecodeHDR(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - float2(texelSize * 3.0, 0.0)));
    half3 c2 = DecodeHDR(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - float2(texelSize * 2.0, 0.0)));
    half3 c3 = DecodeHDR(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - float2(texelSize * 1.0, 0.0)));
    half3 c4 = DecodeHDR(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv));
    half3 c5 = DecodeHDR(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(texelSize * 1.0, 0.0)));
    half3 c6 = DecodeHDR(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(texelSize * 2.0, 0.0)));
    half3 c7 = DecodeHDR(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(texelSize * 3.0, 0.0)));
    half3 c8 = DecodeHDR(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(texelSize * 4.0, 0.0)));

    half3 color = c0 * 0.01621622 + c1 * 0.05405405 + c2 * 0.12162162 + c3 * 0.19459459
        + c4 * 0.22702703
        + c5 * 0.19459459 + c6 * 0.12162162 + c7 * 0.05405405 + c8 * 0.01621622;
    return EncodeHDR(color);
}

half4 FragBlurV(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    float texelSize = _BlitTexture_TexelSize.y;
    float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);

    // Optimized bilinear 5-tap gaussian on the same-sized source (9-tap equivalent)
    half3 c0 = DecodeHDR(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp,
                                            uv - float2(0.0, texelSize * 3.23076923)));
    half3 c1 = DecodeHDR(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp,
                                            uv - float2(0.0, texelSize * 1.38461538)));
    half3 c2 = DecodeHDR(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv));
    half3 c3 = DecodeHDR(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp,
                                            uv + float2(0.0, texelSize * 1.38461538
                                            )));
    half3 c4 = DecodeHDR(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp,
                                            uv + float2(0.0, texelSize * 3.23076923)));

    half3 color = c0 * 0.07027027 + c1 * 0.31621622
        + c2 * 0.22702703
        + c3 * 0.31621622 + c4 * 0.07027027;

    return EncodeHDR(color);
}

#endif
