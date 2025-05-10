using Features.ColorPyramid;
using Features.HierarchyZGenerator;
using UnityEngine.Rendering.Universal;
using URP_Extension.Features.ColorPyramid;

namespace Features.CoreFeature
{
    [DisallowMultipleRendererFeature]
    public class CoreFeature : ScriptableRendererFeature
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