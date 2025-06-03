using UnityEngine.Rendering.Universal;

namespace Features.Postprocessing.Exposure
{
    public class ExposureFeature : ScriptableRendererFeature
    {
        ExposurePass exposurePass;

        public override void Create()
        {
            exposurePass = new ExposurePass()
            {
                renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing
            };
        }
        

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(exposurePass);
        }
    }
}