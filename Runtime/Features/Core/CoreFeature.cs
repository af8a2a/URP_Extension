using Features.ColorPyramid;
using Features.CoreFeature;
using Features.HierarchyZGenerator;
using UnityEngine.Rendering.Universal;

namespace Features.Core
{
    [DisallowMultipleRendererFeature]
    public class CoreFeature : ScriptableRendererFeature
    {
        private readonly string[] m_GBufferPassNames = new string[] { "UniversalGBuffer" };

        HierarchyZPass pass;
        ColorPyramidPass colorPyramid;
        ForwardGBufferPass forwardGBufferPass;

        public override void Create()
        {
            pass = new HierarchyZPass
            {
                renderPassEvent = RenderPassEvent.AfterRenderingTransparents
            };
            colorPyramid = new ColorPyramidPass()
            {
                renderPassEvent = RenderPassEvent.AfterRenderingTransparents
            };
            forwardGBufferPass = new ForwardGBufferPass(m_GBufferPassNames);
        }


        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var deferred = renderingData.universalRenderingData.renderingMode is RenderingMode.Deferred;
            if (ForwardGBufferManager.instance.EnableGBufferPasses() && !deferred)
            {
                renderer.EnqueuePass(forwardGBufferPass);
            }

            renderer.EnqueuePass(pass);
            renderer.EnqueuePass(colorPyramid);
        }
    }
}