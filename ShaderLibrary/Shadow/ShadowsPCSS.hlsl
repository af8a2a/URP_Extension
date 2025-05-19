#ifndef SHADOWS_PCSS_INCLUDED
#define SHADOWS_PCSS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow/ShadowSamplingDisk.hlsl"


CBUFFER_START(PCSSData)
    float4 _PerCascadePCSSData[MAX_SHADOW_CASCADES];
    float4 _DirLightShadowUVMinMax; // xy: shadow uv min, max: shadow uv max
    float4 _DirLightShadowPenumbraParams; // x: soft shadow width, y: scatter occlusion width.
    float4 _DirLightShadowScatterParams; // xyz: shadow subsurface scatter channel, w:shadow scatter mode.
CBUFFER_END

// Limitation:
// Note that in cascade shadows, all occluders behind the near plane will get clamped to the near plane
// This will lead to the closest blocker sometimes being reported as much closer to the receiver than it really is
#if UNITY_REVERSED_Z
#define Z_OFFSET_DIRECTION 1
#else
#define Z_OFFSET_DIRECTION (-1)
#endif


// PreFilter finds the border of shadows. 
float PreFilterSearch(float sampleCount, float filterSize, float3 shadowCoord, float cascadeIndex, float2 random)
{
    float numBlockers = 0.0;

    float radial2ShadowmapDepth = _PerCascadePCSSData[cascadeIndex].x;
    float texelSizeWS = _PerCascadePCSSData[cascadeIndex].y;
    float farToNear = _PerCascadePCSSData[cascadeIndex].z;
    float blockerInvTangent = _PerCascadePCSSData[cascadeIndex].w;

    float2 minCoord = _DirLightShadowUVMinMax.xy;
    float2 maxCoord = _DirLightShadowUVMinMax.zw;
    float sampleCountInverse = rcp((float)sampleCount);
    float sampleCountBias = 0.5 * sampleCountInverse;

    // kernel my be too large, and there can be no valid depth compare result.
    // we must calculate it again in later PCSS.
    float coordOutOfBoundCount = 0;

    for (int i = 1; i < sampleCount && i < DISK_SAMPLE_COUNT; i++)
    {
        float sampleRadius = sqrt((float)i * sampleCountInverse + sampleCountBias);
        float2 offset = fibonacciSpiralDirection[i] * sampleRadius;
        offset = float2(offset.x * random.y + offset.y * random.x,
                        offset.x * -random.x + offset.y * random.y);
        offset *= filterSize;
        offset *= _MainLightShadowmapSize.x; // coord to uv


        float2 sampleCoord = shadowCoord.xy + offset;

        float radialOffset = filterSize * sampleRadius * texelSizeWS;
        float zoffset = radialOffset / farToNear * blockerInvTangent;

        float depthLS = shadowCoord.z + (Z_OFFSET_DIRECTION) * zoffset;
    
        float shadowMapDepth = SAMPLE_TEXTURE2D_ARRAY_LOD(_DirectionalLightsShadowmapTexture, sampler_PointClamp, sampleCoord, cascadeIndex, 0).x;

        bool isOutOfCoord = any(sampleCoord < minCoord) || any(sampleCoord > maxCoord);
        if (!isOutOfCoord && COMPARE_DEVICE_DEPTH_CLOSER(shadowMapDepth, depthLS))
        {
            numBlockers += 1.0;
        }

        if (isOutOfCoord)
        {
            coordOutOfBoundCount++;
        }
    }

    // Out of bound, we must calculate it again in later PCSS.
    if (coordOutOfBoundCount > 0)
    {
        numBlockers = 1.0;
    }

    // We must cover zero offset.
    
    float shadowMapDepth = SAMPLE_TEXTURE2D_ARRAY_LOD(_DirectionalLightsShadowmapTexture, sampler_PointClamp, shadowCoord.xy, cascadeIndex, 0).x;
    if (!(any(shadowCoord.xy < minCoord) || any(shadowCoord.xy > maxCoord)) &&
        COMPARE_DEVICE_DEPTH_CLOSER(shadowMapDepth, shadowCoord.z))
    {
        numBlockers += 1.0;
    }

    return numBlockers;
}


// Return x:avgerage blocker depth, y:num blockers
float2 BlockerSearch(float sampleCount, float filterSize, float3 shadowCoord, float2 random, float cascadeIndex)
{
    float avgBlockerDepth = 0.0;
    float depthSum = 0.0;
    float numBlockers = 0.0;

    float radial2ShadowmapDepth = _PerCascadePCSSData[cascadeIndex].x;
    float texelSizeWS = _PerCascadePCSSData[cascadeIndex].y;
    float farToNear = _PerCascadePCSSData[cascadeIndex].z;
    float blockerInvTangent = _PerCascadePCSSData[cascadeIndex].w;

    float2 minCoord = _DirLightShadowUVMinMax.xy;
    float2 maxCoord = _DirLightShadowUVMinMax.zw;
    float sampleCountInverse = rcp((float)sampleCount);
    float sampleCountBias = 0.5 * sampleCountInverse;

    for (int i = 0; i < sampleCount && i < DISK_SAMPLE_COUNT; i++)
    {
        float sampleDistNorm;
        float2 offset = 0.0;
        offset = ComputeFibonacciSpiralDiskSampleUniform_Directional(i, sampleCountInverse, sampleCountBias, sampleDistNorm);
        offset = float2(offset.x * random.y + offset.y * random.x,
                        offset.x * -random.x + offset.y * random.y);
        offset *= filterSize;
        offset *= _MainLightShadowmapSize.x; // coord to uv

        float2 sampleCoord = shadowCoord.xy + offset;

        float radialOffset = filterSize * sampleDistNorm * texelSizeWS;
        float zoffset = radialOffset / farToNear * blockerInvTangent;

        float depthLS = shadowCoord.z + (Z_OFFSET_DIRECTION) * zoffset;

        float shadowMapDepth = SAMPLE_TEXTURE2D_ARRAY_LOD(_DirectionalLightsShadowmapTexture, sampler_PointClamp, sampleCoord, cascadeIndex, 0).x;
        if (!(any(sampleCoord < minCoord) || any(sampleCoord > maxCoord)) &&
            COMPARE_DEVICE_DEPTH_CLOSER(shadowMapDepth, depthLS))
        {
            depthSum += shadowMapDepth;
            numBlockers += 1.0;
        }
    }

    if (numBlockers > 0.0)
    {
        avgBlockerDepth = depthSum / numBlockers;
    }

    return float2(avgBlockerDepth, numBlockers);
}


float PCSSFilter(float sampleCount, float filterSize, float3 shadowCoord, float2 random, float cascadeIndex, float maxPCSSoffset)
{
    float numBlockers = 0.0;
    float totalSamples = 0.0;

    float radial2ShadowmapDepth = _PerCascadePCSSData[cascadeIndex].x;
    float texelSizeWS = _PerCascadePCSSData[cascadeIndex].y;
    float farToNear = _PerCascadePCSSData[cascadeIndex].z;
    float blockerInvTangent = _PerCascadePCSSData[cascadeIndex].w;

    float2 minCoord = _DirLightShadowUVMinMax.xy;
    float2 maxCoord = _DirLightShadowUVMinMax.zw;
    float sampleCountInverse = rcp((float)sampleCount);
    float sampleCountBias = 0.5 * sampleCountInverse;

    for (int i = 0; i < sampleCount && i < DISK_SAMPLE_COUNT; i++)
    {
        float sampleDistNorm;
        float2 offset = 0.0;
        offset = ComputeFibonacciSpiralDiskSampleUniform_Directional(i, sampleCountInverse, sampleCountBias, sampleDistNorm);
        offset = float2(offset.x * random.y + offset.y * random.x,
                        offset.x * -random.x + offset.y * random.y);
        offset *= filterSize;
        offset *= _MainLightShadowmapSize.x; // coord to uv

        float2 sampleCoord = shadowCoord.xy + offset;

        float radialOffset = filterSize * sampleDistNorm * texelSizeWS;
        float zoffset = radialOffset / farToNear * blockerInvTangent;

        float depthLS = shadowCoord.z + (Z_OFFSET_DIRECTION) * min(zoffset, maxPCSSoffset);

        if (!(any(sampleCoord < minCoord) || any(sampleCoord > maxCoord)))
        {
            float shadowSample = SAMPLE_TEXTURE2D_ARRAY_SHADOW(_DirectionalLightsShadowmapTexture, sampler_LinearClampCompare, float3(sampleCoord, depthLS),
                                                               cascadeIndex).x;
            numBlockers += shadowSample;
            totalSamples++;
        }
    }

    return totalSamples > 0 ? numBlockers / totalSamples : 1.0;
}


#endif /* SHADOWS_PCSS_INCLUDED */
