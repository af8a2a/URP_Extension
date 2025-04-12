Shader "Volumetric Light"
{
    HLSLINCLUDE
    #define MAIN_LIGHT_CALCULATE_SHADOWS  //定义阴影采样
    #define _MAIN_LIGHT_SHADOWS_CASCADE //启用级联阴影

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"  //阴影计算库
    #include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"


    float3 GetWorldPosition(float3 positionHCS)
    {
        /* get world space position from clip position */

        float2 UV = positionHCS.xy / _ScaledScreenParams.xy;
        #if UNITY_REVERSED_Z
        real depth = SampleSceneDepth(UV);
        #else
    real depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(UV));
        #endif
        return ComputeWorldSpacePosition(UV, depth, UNITY_MATRIX_I_VP);
    }

    float GetLightAttenuation(float3 position)
    {
        float4 shadowPos = TransformWorldToShadowCoord(position); //把采样点的世界坐标转到阴影空间
        float intensity = MainLightRealtimeShadow(shadowPos); //进行shadow map采样
        return intensity; //返回阴影值
    }

    #define MAX_RAY_LENGTH 20
    float _StepTime;
    float _Intensity;

    half4 Frag(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);
        float3 worldPos = GetWorldPosition(input.positionCS); //像素的世界坐标
        float3 startPos = _WorldSpaceCameraPos; //摄像机上的世界坐标
        float3 dir = normalize(worldPos - startPos); //视线方向

        float rayLength = length(worldPos - startPos); //视线长度
        rayLength = min(rayLength, MAX_RAY_LENGTH); //限制最大步进长度，MAX_RAY_LENGTH这里设置为20
        float3 final = startPos + dir * rayLength;
        half3 intensity = 0; //累计光强
        float2 step = 1.0 / _StepTime; //定义单次插值大小，_StepTime为步进次数
        for (float i = 0; i < 1; i += step) //光线步进
        {
            float3 currentPosition = lerp(startPos, final, i); //当前世界坐标
            float atten = GetLightAttenuation(currentPosition) * _Intensity; //阴影采样，_Intensity为强度因子
            float3 light = atten;
            intensity += light;
        }
        intensity /= _StepTime;

        Light mainLight = GetMainLight(); //引入场景灯光数据
        return half4(mainLight.color*intensity, 1); //查看结果
    }

    half4 Blur(Varyings input):SV_TARGET
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);

        half4 tex = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv); //中心像素
        //四角像素
        //注意这个【_BlurRange】，这就是扩大卷积核范围的参数
        tex += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv+float2(-1,-1)*_BlitTextureSize.xy*2);
        tex += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv+float2(1,-1)*_BlitTextureSize.xy*2);
        tex += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv+float2(-1,1)*_BlitTextureSize.xy*2);
        tex += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv+float2(1,1)*_BlitTextureSize.xy*2);
        return tex / 5.0; //像素平均
    }

    half4 BlendFrag(Varyings input): SV_TARGET
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);

        half4 sceneColor = half4(SampleSceneColor(uv), 1);
        half4 lightColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv);
        return lightColor + sceneColor;
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
            Name "Compute"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }
        Pass
        {
            Name "BlurH"

            HLSLPROGRAM
            #include "KawaseBlur.hlsl"
            #pragma vertex Vert
            #pragma fragment FragBlurH
            ENDHLSL
        }
        Pass
        {
            Name "BlurV"

            HLSLPROGRAM
            #include "KawaseBlur.hlsl"
            #pragma vertex Vert
            #pragma fragment FragBlurV
            ENDHLSL
        }

        Pass
        {
            Name "Blend"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment BlendFrag
            ENDHLSL
        }

    }
}