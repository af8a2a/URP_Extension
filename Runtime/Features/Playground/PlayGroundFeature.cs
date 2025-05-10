using UnityEngine.Rendering.Universal;
using URP_Extension.Features.Playground;

namespace Features.Playground
{
    //use to test my feature
    [DisallowMultipleRendererFeature]
    public class PlayGroundFeature : ScriptableRendererFeature
    {
        PlayGroundPass pass;

        public override void Create()
        {
            pass = new PlayGroundPass();
            pass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            
            renderer.EnqueuePass(pass);
        }
    }
}