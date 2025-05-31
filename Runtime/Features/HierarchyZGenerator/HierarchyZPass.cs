using Features.MipmapGenerator;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using URP_Extension.Features.HierarchyZGenerator;

namespace Features.HierarchyZGenerator
{
    [DisallowMultipleRendererFeature]
    public class HierarchyZPass : ScriptableRenderPass
    {
        private int HierarchyZId = Shader.PropertyToID("_HierarchyZTexture");

        public class HierarchyZPassData
        {
            public int dimX;
            public int dimY;
            public HierarchyZData hierarchyZData;

            // Buffer handles for the compute buffers.
            public TextureHandle input;
            public TextureHandle output;
        }


        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var hizResource = frameData.GetOrCreate<HierarchyZData>();
            hizResource.HizTexture = MipGenerator.Instance.RenderDepthPyramid(renderGraph, frameData);
            
        }
    }
}