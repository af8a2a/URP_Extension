using UnityEngine.Rendering.Universal;

namespace URP_Extension.Features.ScreenSpaceRaytracing
{
    public class ScreenSpaceReflectionFeature : ScriptableRendererFeature
    {
        ForwardGBufferPass m_GBufferPass;
        BackfaceDepthPass m_BackfaceDepthPass;
        ScreenSpaceReflectionPass m_ScreenSpaceReflectionPass;
        private readonly string[] m_GBufferPassNames = new string[] { "UniversalGBuffer" };

        public override void Create()
        {
            m_GBufferPass = new ForwardGBufferPass(m_GBufferPassNames);
            m_BackfaceDepthPass = new BackfaceDepthPass();
            m_ScreenSpaceReflectionPass = new ScreenSpaceReflectionPass();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            

            renderer.EnqueuePass(m_GBufferPass);
            renderer.EnqueuePass(m_BackfaceDepthPass);
            renderer.EnqueuePass(m_ScreenSpaceReflectionPass);
        }
    }
}