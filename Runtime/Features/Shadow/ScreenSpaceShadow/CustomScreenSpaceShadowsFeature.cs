using System;
using Features.Shadow.ScreenSpaceShadow.URPShadow;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Features.Shadow.ScreenSpaceShadow
{
    //prepare for global setting
    [Serializable]
    internal class ScreenSpaceShadowsSettings
    {
    }

    [SupportedOnRenderer(typeof(UniversalRendererData))]
    [DisallowMultipleRendererFeature("Custom Screen Space Shadows")]
    public class CustomScreenSpaceShadowsFeature : ScriptableRendererFeature
    {
#if UNITY_EDITOR
        [UnityEditor.ShaderKeywordFilter.SelectIf(true, keywordNames: ShaderKeywordStrings.MainLightShadowScreen)]
        private const bool k_RequiresScreenSpaceShadowsKeyword = true;
#endif

        [SerializeField] private ScreenSpaceShadowsSettings m_Settings = new ScreenSpaceShadowsSettings();

        private URPScreenSpaceShadowsPass m_SSShadowsPass = null;
        private ScreenSpaceShadowsPostPass m_SSShadowsPostPass = null;


        /// <inheritdoc/>
        public override void Create()
        {
            if (m_SSShadowsPass == null)
                m_SSShadowsPass = new URPScreenSpaceShadowsPass();
            if (m_SSShadowsPostPass == null)
                m_SSShadowsPostPass = new ScreenSpaceShadowsPostPass();

            m_SSShadowsPass.renderPassEvent = RenderPassEvent.AfterRenderingGbuffer;
            m_SSShadowsPostPass.renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
        }


        /// <inheritdoc/>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (UniversalRenderer.IsOffscreenDepthTexture(ref renderingData.cameraData))
                return;


            var shadowSettings = VolumeManager.instance.stack.GetComponent<Shadows>();


            var algo = ShadowAlgo.URP;
            bool overrideSetting = !(shadowSettings is null || !shadowSettings.IsActive());

            if (overrideSetting)
            {
                algo = shadowSettings.shadowAlgo.value;
            }

            bool usesDeferredLighting = renderer is UniversalRenderer { usesDeferredLighting: true };


            switch (algo)
            {
                case ShadowAlgo.URP:
                    bool allowMainLightShadows = renderingData.shadowData.supportsMainLightShadows && renderingData.lightData.mainLightIndex != -1;

                    bool shouldEnqueue = allowMainLightShadows && m_SSShadowsPass.Setup(m_Settings);

                    if (shouldEnqueue)
                    {
                        m_SSShadowsPass.renderPassEvent = usesDeferredLighting
                            ? RenderPassEvent.AfterRenderingGbuffer
                            : RenderPassEvent.AfterRenderingPrePasses +
                              1; // We add 1 to ensure this happens after depth priming depth copy pass that might be scheduled

                        renderer.EnqueuePass(m_SSShadowsPass);
                        renderer.EnqueuePass(m_SSShadowsPostPass);
                    }

                    break;
                case ShadowAlgo.CSM8:

                    renderer.EnqueuePass(m_SSShadowsPostPass);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


        private class ScreenSpaceShadowsPostPass : ScriptableRenderPass
        {
            internal ScreenSpaceShadowsPostPass()
            {
                profilingSampler = new ProfilingSampler("Set Screen Space Shadow Keywords");
            }


            private static void ExecutePass(RasterCommandBuffer cmd, UniversalShadowData shadowData)
            {
                int cascadesCount = shadowData.mainLightShadowCascadesCount;
                bool mainLightShadows = shadowData.supportsMainLightShadows;
                bool receiveShadowsNoCascade = mainLightShadows && cascadesCount == 1;
                bool receiveShadowsCascades = mainLightShadows && cascadesCount > 1;

                // Before transparent object pass, force to disable screen space shadow of main light
                cmd.SetKeyword(ShaderGlobalKeywords.MainLightShadowScreen, false);

                // then enable main light shadows with or without cascades
                cmd.SetKeyword(ShaderGlobalKeywords.MainLightShadows, receiveShadowsNoCascade);
                cmd.SetKeyword(ShaderGlobalKeywords.MainLightShadowCascades, receiveShadowsCascades);
            }


            internal class PassData
            {
                internal ScreenSpaceShadowsPostPass pass;
                internal UniversalShadowData shadowData;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData, profilingSampler))
                {
                    UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

                    TextureHandle color = resourceData.activeColorTexture;
                    builder.SetRenderAttachment(color, 0, AccessFlags.Write);
                    passData.shadowData = frameData.Get<UniversalShadowData>();
                    passData.pass = this;

                    builder.AllowGlobalStateModification(true);

                    builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) => { ExecutePass(rgContext.cmd, data.shadowData); });
                }
            }
        }
    }
}