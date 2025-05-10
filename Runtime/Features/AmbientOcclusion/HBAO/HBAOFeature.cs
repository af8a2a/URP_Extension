using UnityEngine.Rendering.Universal;

namespace Features.AmbientOcclusion.HBAO
{
    [DisallowMultipleRendererFeature]
    public class HBAOFeature : ScriptableRendererFeature
    {
        private HBAOPass pass;

        public override void Create()
        {
            pass = new HBAOPass();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            pass.Setup();

            renderer.EnqueuePass(pass);
        }
    }
}