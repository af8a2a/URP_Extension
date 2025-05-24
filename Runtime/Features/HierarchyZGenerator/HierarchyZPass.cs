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
            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();

            var depthTexture = resourceData.cameraDepthTexture;

            // Each threadgroup works on 64x64 texels
            // uint32_t dimX = (m_Width + 63) / 64;
            // uint32_t dimY = (m_Height + 63) / 64;
            // vkCmdDispatch(cb, dimX, dimY, 1);
            var destinationDesc = renderGraph.GetTextureDesc(depthTexture);

            destinationDesc.depthBufferBits = 0;
            destinationDesc.useMipMap = true;

            destinationDesc.name = $"Hi-Z_Texture";
            destinationDesc.clearBuffer = false;
            destinationDesc.dimension = TextureDimension.Tex2D;
            destinationDesc.enableRandomWrite = true;
            destinationDesc.depthBufferBits = 0;
            destinationDesc.colorFormat = GraphicsFormat.R32_SFloat;

            TextureHandle hierarchyZTexture = renderGraph.CreateTexture(destinationDesc);

            var hizExist = frameData.Contains<HierarchyZData>();
            var hizResource = frameData.GetOrCreate<HierarchyZData>();
            if (!hizExist)
            {
                hizResource.HizTexture = hierarchyZTexture;
            }

            using (var builder = renderGraph.AddUnsafePass(nameof(HierarchyZPass), out HierarchyZPassData passData))
            {
                // Set the pass data so the data can be transfered from the recording to the execution.
                builder.AllowPassCulling(false);
                passData.input = depthTexture;
                passData.output = hierarchyZTexture;
                passData.dimX = destinationDesc.width;
                passData.dimY = destinationDesc.height;
                passData.hierarchyZData = hizResource;
                // UseBuffer is used to setup render graph dependencies together with read and write flags.
                builder.UseTexture(passData.input, AccessFlags.ReadWrite);
                builder.UseTexture(passData.output, AccessFlags.ReadWrite);
                builder.SetGlobalTextureAfterPass(passData.output, HierarchyZId);
                // The execution function is also call SetRenderfunc for compute passes.
                builder.SetRenderFunc((HierarchyZPassData data, UnsafeGraphContext cgContext) =>
                    ExecutePass(data, cgContext));
            }
        }


        static void ExecutePass(HierarchyZPassData data, UnsafeGraphContext cgContext)
        {
            var cmd = CommandBufferHelpers.GetNativeCommandBuffer(cgContext.cmd);
            // Blitter.BlitCameraTexture(cmd, data.input, data.output);
            //note:D3D11 Not support ResourcesBarrier

            data.hierarchyZData.MipCount = MipmapGenerator.MipGenerator.Instance.RenderDepthPyramid(cmd,
                new Vector2Int(data.dimX,
                    data.dimY), data.input, data.output);
        }
    }
}