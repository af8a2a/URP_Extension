using UnityEngine.Rendering.Universal;

namespace Features.Postprocessing.Bloom.MobileBloom
{
    public class MobileBloomFeature:ScriptableRendererFeature
    {
        private MobileBloomPass pass;
        public override void Create()
        {
            pass = new MobileBloomPass()
            {
                renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(pass);
        }
    }
}