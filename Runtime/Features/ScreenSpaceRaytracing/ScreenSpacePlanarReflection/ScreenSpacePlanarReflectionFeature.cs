using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Features.ScreenSpaceRaytracing.ScreenSpacePlanarReflection
{
    [DisallowMultipleRendererFeature("ScreenSpacePlanarReflection Feature")]
    public sealed class ScreenSpacePlanarReflectionFeature : ScriptableRendererFeature
    {
        ScreenSpacePlanarReflectionPass m_SSPRRenderPass;

        public override void Create()
        {
            m_SSPRRenderPass = new ScreenSpacePlanarReflectionPass()
            {
                renderPassEvent = RenderPassEvent.BeforeRenderingTransparents,
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var sspr = VolumeManager.instance.stack.GetComponent<ScreenSpacePlanarReflection>();
            if (sspr is null || !sspr.IsActive())
            {
                return;
            }

            renderer.EnqueuePass(m_SSPRRenderPass);
        }
    }
}