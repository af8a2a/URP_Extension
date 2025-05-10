using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Features.Postprocessing.DepthOfField.DiaphragmDOF
{
    [DisallowMultipleRendererFeature]
    public class DepthOfFieldFeature : ScriptableRendererFeature
    {
        DiaphragmDoFPass diaphragmDoFPass;

        public override void Create()
        {
            diaphragmDoFPass = new DiaphragmDoFPass();
            diaphragmDoFPass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }


        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var setting = VolumeManager.instance.stack.GetComponent<PhysicallyDepthOfField>();
            if (setting == null || !setting.IsActive())
            {
                return;
            }

            diaphragmDoFPass.Setup(setting);

            renderer.EnqueuePass(diaphragmDoFPass);
        }
    }
}