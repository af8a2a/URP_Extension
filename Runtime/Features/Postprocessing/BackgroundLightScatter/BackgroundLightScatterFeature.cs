using Features.ScreenSpaceRaytracing;
using UnityEngine.Rendering.Universal;

namespace Features.Postprocessing.BackgroundLightScatter
{
    public class BackgroundLightScatterFeature : ScriptableRendererFeature
    {
        private readonly string[] m_PassNames = new string[] { "CharacterMask" };

        BackgroundLightClassifyPass _classifyPass;
        BackgroundLightScatterPass _scatterPass;
        public override void Create()
        {
            _classifyPass = new BackgroundLightClassifyPass(m_PassNames);
            _scatterPass = new BackgroundLightScatterPass()
            {
                renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing,
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(_classifyPass);
            renderer.EnqueuePass(_scatterPass);

        }
    }
}