using Features.ColorPyramid;
using UnityEngine.Rendering.Universal;
using URP_Extension.Features.ColorPyramid;
using URP_Extension.Features.HierarchyZGenerator;

namespace Features.HierarchyZGenerator
{
    [DisallowMultipleRendererFeature]
    public class DummyFeature : ScriptableRendererFeature
    {
        HierarchyZPass pass;
        ColorPyramidPass colorPyramid;

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
        }


        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(pass);
            renderer.EnqueuePass(colorPyramid);

        }
    }
}