Shader "PostProcessing/TemporalFilter"
{
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"
        }
        LOD 100
        ZTest Always ZWrite Off Cull Off
        Pass
        {

            Name "Temporal Denoise"
            Tags
            {
                "LightMode" = "Temporal Denoise"
                
            }



            HLSLPROGRAM
            #pragma multi_compile_local_fragment __ VARIANCE_CLIPPING_4TAP VARIANCE_CLIPPING_8TAP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            // The Blit.hlsl file provides the vertex shader (Vert),
            // input structure (Attributes) and output strucutre (Varyings)
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #pragma vertex Vert
            #pragma fragment TemporalFilter_Frag


            #pragma target 3.5

            
            #include "TemporalAccumulation.hlsl"


            ENDHLSL
        }

    }
}