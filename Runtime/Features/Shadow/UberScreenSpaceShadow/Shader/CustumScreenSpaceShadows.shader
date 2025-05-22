Shader "Hidden/CustomScreenSpaceShadows"
{
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True"
        }

        HLSLINCLUDE
        //Keep compiler quiet about Shadows.hlsl.
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ImageBasedLighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal.extension/ShaderLibrary/Shadow/ShadowsPCSS.hlsl"
        // Core.hlsl for XR dependencies
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

        TEXTURE2D_SHADOW(_PerObjectScreenSpaceShadowmapTexture);


        half SamplePerObjectShadowmap(float2 PositionSS)
        {
            half attenuation = half(SAMPLE_TEXTURE2D(_PerObjectScreenSpaceShadowmapTexture, sampler_PointClamp, PositionSS.xy).x);

            return attenuation;
        }



        half4 Fragment(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            #if UNITY_REVERSED_Z
            float deviceDepth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_PointClamp, input.texcoord.xy).r;
            #else
            float deviceDepth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_PointClamp, input.texcoord.xy).r;
            deviceDepth = deviceDepth * 2.0 - 1.0;
            #endif

            //Fetch shadow coordinates for cascade.
            float3 wpos = ComputeWorldSpacePosition(input.texcoord.xy, deviceDepth, unity_MatrixInvVP);
            float4 coords = TransformWorldToShadowCoord(wpos);

            // Screenspace shadowmap is only used for directional lights which use orthogonal projection.
        #if  defined(PCSS)

            half realtimeShadow = MainLightRealtimeShadow_PCSS(wpos,coords,input.texcoord.xy);
            #else
            
            half realtimeShadow = MainLightRealtimeShadow(coords);
            #endif

            #if defined(_PEROBJECT_SCREEN_SPACE_SHADOW)
            realtimeShadow=min(realtimeShadow,SamplePerObjectShadowmap(input.texcoord.xy));
            #endif

            return realtimeShadow;
        }
        ENDHLSL

        Pass
        {
            Name "ScreenSpaceShadows"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma multi_compile _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _PEROBJECT_SCREEN_SPACE_SHADOW
            #pragma multi_compile_fragment _ PCSS

            #pragma vertex   Vert
            #pragma fragment Fragment
            ENDHLSL
        }
    }
}