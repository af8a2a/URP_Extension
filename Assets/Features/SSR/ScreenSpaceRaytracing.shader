﻿Shader "Screen Space Raytracing"
{

    Properties
    {
        [HideInInspector] _Seed("Private: Random Seed", Float) = 0.0
        [HideInInspector] _MinSmoothness("Minimum Smoothness", Float) = 0.4
        [HideInInspector] _FadeSmoothness("Smoothness Fade Start", Float) = 0.6
        [HideInInspector] _EdgeFade("Screen Edge Fade Distance", Float) = 0.1
        [HideInInspector] _Thickness("Object Thickness", Float) = 0.25
        [HideInInspector] _StepSize("Step Size", Float) = 0.4
        [HideInInspector] _StepSizeMultiplier("Step Size Multiplier", Float) = 1.33
        [HideInInspector] _MaxStep("Max Ray Steps", Float) = 16.0
        [HideInInspector] _DownSample("Private: Ray Marching Resolution", Float) = 1.0
        [HideInInspector] _AccumulationFactor("Accumulation Factor", Float) = 0.75
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 100


        Pass
        {
            Name "Screen Space Reflection Approximation"
            Tags
            {
                "LightMode" = "Screen Space Reflection Approximation"
            }

            HLSLPROGRAM
            half _Seed;
            half _MinSmoothness;
            half _FadeSmoothness;
            half _EdgeFade;
            half _Thickness;
            half _StepSize;
            half _StepSizeMultiplier;
            half _MaxStep;
            half _DownSample;
            half _AccumulationFactor;

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            #include "SSR.hlsl"

            // The Blit.hlsl file provides the vertex shader (Vert),
            // input structure (Attributes) and output strucutre (Varyings)
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #pragma vertex Vert
            #pragma fragment SSR

            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_local_fragment _ _BACKFACE_ENABLED

            #pragma target 3.5


            float4 SSR(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 screenUV = input.texcoord;

                float depth = SAMPLE_TEXTURE2D_X_LOD(_CameraDepthTexture, sampler_CameraDepthTexture, screenUV, 0).r;

                #if !UNITY_REVERSED_Z
				depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, depth);
                #endif
                bool isBackground;
                #if (UNITY_REVERSED_Z == 1)
                isBackground = depth == 0.0 ? true : false;
                #else
				isBackground = depth == 1.0 ? true : false; // OpenGL Platforms.
                #endif
                if (isBackground)
                    return half4(SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_BlitTexture, screenUV, 0.0).rgb, 0.0);

                half4 gBuffer2 = SAMPLE_TEXTURE2D_X_LOD(_GBuffer2, my_point_clamp_sampler,
                                                        UnityStereoTransformScreenSpaceTex(screenUV), 0);

                if (gBuffer2.a < _MinSmoothness)
                    return half4(SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_BlitTexture, screenUV, 0.0).rgb, 0.0);

                // The world position reconstruction failed on XR platforms.
                // Reason: "UNITY_MATRIX_I_VP" is not set as a stereo matrix.
                float3 positionWS = ComputeWorldSpacePosition(screenUV, depth, UNITY_MATRIX_I_VP);

                half3 invViewDirWS;
                if (unity_OrthoParams.w == 0.0)
                    invViewDirWS = normalize(positionWS - _WorldSpaceCameraPos);
                else
                    invViewDirWS = -normalize(UNITY_MATRIX_V[2].xyz);


                half3 normalWS = UnpackNormal(gBuffer2.xyz);

                Ray ray;
                ray.position = positionWS;
                ray.direction = reflect(invViewDirWS, normalWS);

                half dither = half(InterleavedGradientNoise(screenUV * _ScreenSize.xy, 0)) * 0.25 - 0.125 + half(
                    GenerateRandomFloat(screenUV)) * 0.1 - 0.05;
                RayHit rayHit = RayMarching(ray, dither, length(depth));

                UNITY_BRANCH
                if (rayHit.distance > REAL_EPS)
                {
                    // The surfaceData of current pixel.
                    RayHit screenHit;
                    HitSurfaceDataFromGBuffers(screenUV, screenHit.albedo, screenHit.specular, screenHit.occlusion,
                                                                    screenHit.normal, screenHit.smoothness);

                    half3 reflectColor = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_BlitTexture, rayHit.uv, 0.0).rgb;

                    // [Match URP Fresnel] Use slightly simpler fresnelTerm (Pow4 vs Pow5) as a small optimization.
                    half fresnel = (max(screenHit.smoothness, 0.04) - 0.04) * Pow4(
                        1.0 - saturate(dot(screenHit.normal, -invViewDirWS))) + 0.04;

                    half3 sceneColor = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_BlitTexture,
                        UnityStereoTransformScreenSpaceTex(screenUV), 0.0).rgb;
                    reflectColor *= screenHit.occlusion;

                    // [Approximate Blending] We have to blend the results without separating environment reflections.
                    half reflectivity = ReflectivitySpecular(screenHit.specular);
                    reflectColor = lerp(reflectColor, reflectColor * screenHit.specular,
                                        saturate(reflectivity - fresnel));
                    return half4(lerp(sceneColor, reflectColor,
                                      saturate(reflectivity + fresnel) * EdgeOfScreenFade(
                                          UnityStereoTransformScreenSpaceTex(screenUV))), 1.0);
                }
                else
                {
                    // We should also output other parts of the scene for correct trilinear interpolation.
                    return half4(SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_BlitTexture, screenUV, 0.0).rgb, 0.0);
                }
            }
            ENDHLSL
        }

        Pass
        {
            Name "Composite"
            Tags
            {
                "LightMode" = "Screen Space Reflection Approximation"
            }

            // Preserve source alpha
            Blend SrcAlpha OneMinusSrcAlpha, SrcAlpha SrcAlpha

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            // The Blit.hlsl file provides the vertex shader (Vert),
            // input structure (Attributes) and output strucutre (Varyings)
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #pragma vertex Vert
            #pragma fragment frag

            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_local_fragment _ _SSR_APPROX_COLOR_MIPMAPS

            #pragma target 3.5
            // Color Pyramid
            //#pragma require 2darray

            CBUFFER_START(UnityPerMaterial)
                half _Seed;
                half _MinSmoothness;
                half _FadeSmoothness;
                half _EdgeFade;
                half _Thickness;
                half _StepSize;
                half _StepSizeMultiplier;
                half _MaxStep;
                half _DownSample;
                half _AccumulationFactor;
            CBUFFER_END

            TEXTURE2D(_ScreenSpaceReflectionSourceTexture);
            #include "SSR.hlsl"

            // TODO: should we generate gaussian color pyramid to improve rough reflections in approximation mode?
            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 screenUV = input.texcoord;

                float depth = SAMPLE_TEXTURE2D_X_LOD(_CameraDepthTexture, sampler_CameraDepthTexture, screenUV, 0).r;

                #if !UNITY_REVERSED_Z
				depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, depth);
                #endif
                bool isBackground;
                #if (UNITY_REVERSED_Z == 1)
                isBackground = depth == 0.0 ? true : false;
                #else
				isBackground = depth == 1.0 ? true : false; // OpenGL Platforms.
                #endif

                if (isBackground)
                    return half4(0.0, 0.0, 0.0, 0.0);

                half smoothness = SAMPLE_TEXTURE2D_X_LOD(_GBuffer2, my_point_clamp_sampler, screenUV, 0.0).a;
                if (smoothness == 0.0)
                    return half4(0.0, 0.0, 0.0, 0.0);

                half fadeSmoothness = (_FadeSmoothness < smoothness)
                                          ? 1.0
                                          : (smoothness - _MinSmoothness) * rcp(_FadeSmoothness - _MinSmoothness);

                half smoothness2 = smoothness * smoothness;
                half smoothness4 = smoothness2 * smoothness2;

                #if defined(_SSR_APPROX_COLOR_MIPMAPS)
				half oneMinusSmoothness4 = 1.0 - smoothness4;
				half3 reflectColor = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_BlitTexture, UnityStereoTransformScreenSpaceTex(screenUV), oneMinusSmoothness4 * 5.0).rgb;
                #else
                half3 reflectColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture,
                    UnityStereoTransformScreenSpaceTex(screenUV)).rgb;
                #endif
                return half4(reflectColor, smoothness4 * fadeSmoothness);
            }
            ENDHLSL
        }


    }
}