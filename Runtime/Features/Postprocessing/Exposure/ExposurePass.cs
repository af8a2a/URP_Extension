using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Features.Postprocessing.Exposure
{
    public partial class ExposurePass:ScriptableRenderPass
    {
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            DoFixedExposure(renderGraph, frameData);
        }

    }
}