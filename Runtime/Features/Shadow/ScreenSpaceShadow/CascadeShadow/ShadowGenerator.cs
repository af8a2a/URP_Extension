using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Features.Shadow.ScreenSpaceShadow.CascadeShadow
{
    public class ShadowGenerator
    {
        
        private static class MainLight8CascadeShadowConstantBuffer
        {
            public static int _WorldToShadow;
            public static int _ShadowParams;

            public static int _CascadeShadowSplitSpheresArray;
            public static int _CascadeZDistanceArray;
            public static int _ShadowOffsetArray;

            public static int _ShadowmapSize;
        }

        const int k_MaxCascades = 8;
        const int k_ShadowmapBufferBits = 16;
        float m_MaxShadowDistance;
        int m_ShadowmapWidth;
        int m_ShadowmapHeight;
        int m_ShadowCasterCascadesCount;
        bool m_SupportsBoxFilterForShadows;

        int m_ShadowmapCacheWidth;
        int m_ShadowmapCacheHeight;
        int m_ShadowCascadeResolution;
        int m_FarCascadesCount;
        int m_CurrentCacheCascadesIndex;
        bool m_NeedRefreshAllShadowmapCache;
        int[] m_RenderCascadeIndexArray;

        const int SHADER_NUMTHREAD_X = 8; //match compute shader's [numthread(x)]
        const int SHADER_NUMTHREAD_Y = 8; //match compute shader's [numthread(y)]
        ComputeShader m_CacheCompute;


        Matrix4x4[] m_MainLightShadowMatrices;
        ShadowSliceData[] m_CascadeSlices;
        Vector4[] m_CascadeSplitDistances;

        ProfilingSampler m_ProfilingSetupSampler = new ProfilingSampler("Setup Custom Main Shadowmap");


        public Vector2 GetShadowmapSize()
        {
            return new Vector2(m_ShadowmapCacheWidth, m_ShadowmapCacheHeight);
        }

        public int GetCascadeResolution()
        {
            return m_ShadowCascadeResolution;
        }


        public ShadowGenerator()
        {
            m_MainLightShadowMatrices = new Matrix4x4[k_MaxCascades + 1];
            m_CascadeSlices = new ShadowSliceData[k_MaxCascades];
            m_CascadeSplitDistances = new Vector4[k_MaxCascades];

            MainLight8CascadeShadowConstantBuffer._WorldToShadow = Shader.PropertyToID("_MainLightWorldToShadow");
            MainLight8CascadeShadowConstantBuffer._ShadowParams = Shader.PropertyToID("_MainLightShadowParams");

            MainLight8CascadeShadowConstantBuffer._CascadeShadowSplitSpheresArray = Shader.PropertyToID("_CascadeShadowSplitSpheresArray");
            MainLight8CascadeShadowConstantBuffer._CascadeZDistanceArray = Shader.PropertyToID("_CascadeZDistanceArray");
            MainLight8CascadeShadowConstantBuffer._ShadowOffsetArray = Shader.PropertyToID("_MainLightShadowOffsetArray");

            MainLight8CascadeShadowConstantBuffer._ShadowmapSize = Shader.PropertyToID("_MainLightShadowmapSize");

        }


        TextureHandle RenderCascadeShadow(RenderGraph renderGraph, ContextContainer frameData)
        {
            return TextureHandle.nullHandle;
        }
    }
}