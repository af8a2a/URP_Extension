using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using URP_Extension.Features.HierarchyZGenerator;

namespace URP_Extension.Features.ScreenSpaceRaytracing.StochasticScreenSpaceReflection
{
    public class StochasticScreenSpaceReflectionFeature : ScriptableRendererFeature
    {
        private ForwardGBufferPass m_GBufferPass;
        // private HierarchyZPass m_HierarchyZPass;
        private StochasticScreenSpaceReflectionPass stochasticScreenSpaceReflectionPass;

        private readonly string[] m_GBufferPassNames = new string[] { "UniversalGBuffer" };
        [SerializeField] Cubemap cubemap = null;

        public override void Create()
        {
            m_GBufferPass = new ForwardGBufferPass(m_GBufferPassNames);
            // m_HierarchyZPass = new HierarchyZPass();
            stochasticScreenSpaceReflectionPass =
                new StochasticScreenSpaceReflectionPass(RenderPassEvent.AfterRenderingTransparents + 1);
            // _sssrClassifyTilesPass = new SSSRClassifyTilesPass();
            m_GBufferPass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
            // m_HierarchyZPass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
            // _sssrClassifyTilesPass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents + 1;
        }


        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            stochasticScreenSpaceReflectionPass.Setup();
            // var sssr = VolumeManager.instance.stack.GetComponent<StochasticScreenSpaceReflection>();
            // _mPass.Setup(sssr);
            //
            renderer.EnqueuePass(m_GBufferPass);
            // renderer.EnqueuePass(m_HierarchyZPass);
            renderer.EnqueuePass(stochasticScreenSpaceReflectionPass);
        }
    }
}