using Features.MipmapGenerator;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using URP_Extension.Features.ColorPyramid;

namespace Features.ColorPyramid
{
    public class ColorPyramidPass : ScriptableRenderPass
    {
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resource = frameData.GetOrCreate<ColorPyramidData>();
            resource.ColorTexture = MipGenerator.Instance.RenderColorPyramid(renderGraph, frameData);
        }
    }
}