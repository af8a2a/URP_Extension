#ifndef URP_TEMPORAL_ACCUMULATION_HLSL
#define URP_TEMPORAL_ACCUMULATION_HLSL
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


// Camera or Per Object motion vectors.
TEXTURE2D(_MotionVectorTexture);
float4 _MotionVectorTexture_TexelSize;
float4 _BlitTexture_TexelSize;
float _AccumulationFactor;
// Previous frame reflection color
TEXTURE2D(_HistoryColorTexture);
//R16F

inline void FetchColorAndDepth(float2 uv, inout float4 color, inout float depth)
{
    color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv);
    depth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_PointClamp, uv);
}

inline float2 FetchMotionVectors(float2 uv)
{
    return SAMPLE_TEXTURE2D_X(_MotionVectorTexture, sampler_PointClamp, uv).rg;
}

inline float4 FetchColorHistory(float2 uv)
{
    return SAMPLE_TEXTURE2D_X(_HistoryColorTexture, sampler_LinearClamp, uv);
}

// inline float4 FetchDepthHistory(float2 uv)
// {
//     return SAMPLE_TEXTURE2D_X(_HistoryDepthTexture, sampler_LinearClamp, uv);
// }


inline half FetchNeighbor(float2 uv, float2 offset)
{
    return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv + _BlitTexture_TexelSize.xy * offset).a;
}

inline half DisocclusionTest(float2 uvm1, float depth, float depthm1)
{
    // disocclusion test
    // https://developer.nvidia.com/sites/default/files/akamai/gamedev/files/gdc12/GDC12_Bavoil_Stable_SSAO_In_BF3_With_STF.pdf (Page 19)
    float z = depth;
    float zm1 = depthm1;
    // for fetching zi-1, use clamp-to-border to discard out-of-frame data, with borderZ = 0.f
    // https://developer.nvidia.com/sites/default/files/akamai/gamedev/files/gdc12/GDC12_Bavoil_Stable_SSAO_In_BF3_With_STF.pdf (Page 39)
    // if (uvm1.x < 0 || uvm1.y < 0 || uvm1.x > 1 || uvm1.y > 1) zm1 = 0;
    // if (uvm1.x < 0 || uvm1.y < 0 || uvm1.x > 1 || uvm1.y > 1) => dot(step(half4(uvm1, 1, 1), half4(0, 0, uvm1)), 1) is 1 if out-of-frame, so
    zm1 *= 1.0 - dot(step(float4(uvm1, 1, 1), float4(0, 0, uvm1)), 1);
    // relaxed disocclusion test: abs(1.0 - (z / zm1)) > 0.1 => 10% 
    // float disocclusion = max(sign(abs(1.0 - (z / zm1)) - 0.1), 0.0);
    float disocclusion = abs(1.0 - (z / zm1)) > 0.1;

    return disocclusion;
}

inline half4 VarianceClipping(float2 uv, half4 color, half aom1, float velocityWeight)
{
    // neighborhood clamping
    // http://twvideo01.ubm-us.net/o1/vault/gdc2016/Presentations/Pedersen_LasseJonFuglsang_TemporalReprojectionAntiAliasing.pdf // (pages 26-28)
    // superseded by variance clipping
    // http://developer.download.nvidia.com/gameworks/events/GDC2016/msalvi_temporal_supersampling.pdf (page 23-29)
    #if VARIANCE_CLIPPING_4TAP
    half cT = FetchNeighbor(uv, float2(0, 1));
    half cR = FetchNeighbor(uv, float2(1, 0));
    half cB = FetchNeighbor(uv, float2(0, -1));
    half cL = FetchNeighbor(uv, float2(-1, 0));
    // compute 1st and 2nd color moments
    half4 m1 = color + cT + cR + cB + cL;
    half4 m2 = color * color + cT * cT + cR * cR + cB * cB + cL * cL;
    // aabb from mean u and variance sigma2
    half4 mu = m1 / 5.0;
    half sigma = sqrt(m2 / 5.0 - mu * mu);

    #elif VARIANCE_CLIPPING_8TAP
    half cTL = FetchNeighbor(uv, float2(-1, 1));
    half cT = FetchNeighbor(uv, float2(0, 1));
    half cTR = FetchNeighbor(uv, float2(1, 1));
    half cR = FetchNeighbor(uv, float2(1, 0));
    half cBR = FetchNeighbor(uv, float2(1, -1));
    half cB = FetchNeighbor(uv, float2(0, -1));
    half cBL = FetchNeighbor(uv, float2(-1, -1));
    half cL = FetchNeighbor(uv, float2(-1, 0));
    // compute 1st and 2nd color moments
    half4 m1 = color + cTL + cT + cTR + cR + cBR + cB + cBL + cL;
    half4 m2 = color * color + cTL * cTL + cT * cT + cTR * cTR + cR * cR + cBR * cBR + cB * cB + cBL * cBL + cL * cL;
    // aabb from mean u and variance sigma2
    half4 mu = m1 / 9.0;
    half sigma = sqrt(m2 / 9.0 - mu * mu);
    #endif

    #if VARIANCE_CLIPPING_4TAP || VARIANCE_CLIPPING_8TAP
    float gamma = lerp(75.0, 0.75, velocityWeight); // scale down sigma for reduced ghosting 
    half4 cmin = mu - gamma * sigma;
    half4 cmax = mu + gamma * sigma;

    // clipping
    return clamp(aom1, cmin, cmax);
    #else
    return aom1;
    #endif
}

float4 TemporalFilter_Frag(Varyings input):SV_Target0
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);

    // fetch current frame data
    float4 color;
    float depth;
    FetchColorAndDepth(uv, color, depth);

    // fetch motion vectors, calculate previous frame uv
    float2 mv = FetchMotionVectors(uv);
    float2 previous_uv = uv - mv.xy;
    float mvl = length(mv);

    // fetch history
    float4 colorHistory = FetchColorHistory(previous_uv);
    float depthm1 = colorHistory.a;
    float mvlm1 = colorHistory.z;

    // velocity weight
    float velocityWeight = saturate(abs(mvl - mvlm1) * 300.0);

    // do disocclusion test
    half disocclusion = DisocclusionTest(previous_uv, depth, depthm1);
    // apply velocity weight and disocclusion
    colorHistory = colorHistory + saturate(dot(float2(velocityWeight, disocclusion), 1.0)) * (color - colorHistory);

    // do variance clipping
    colorHistory = VarianceClipping(uv, color, colorHistory, velocityWeight);

    // exponential accumulation buffer
    // http://www.klayge.org/material/4_11/Filmic%20SMAA%20v7.pdf (page 54)
    // http://developer.download.nvidia.com/gameworks/events/GDC2016/msalvi_temporal_supersampling.pdf (page 13)
    color = colorHistory + (1.0 - _AccumulationFactor) * (color - colorHistory);
    color.a = depth;
    return color;
}
#endif
