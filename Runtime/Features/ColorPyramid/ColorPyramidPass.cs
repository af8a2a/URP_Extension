using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using URP_Extension.Features.HierarchyZGenerator;

namespace URP_Extension.Features.ColorPyramid
{
    public class ColorPyramidPass : ScriptableRenderPass
    {
        private int HierarchyZId = Shader.PropertyToID("_ColorPyramidTexture");

        public class PassData
        {
            public int dimX;
            public int dimY;
            public ColorPyramidData ColorPyramidData;

            // Buffer handles for the compute buffers.
            public TextureHandle input;
            public TextureHandle output;
        }


        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();

            var depthTexture = resourceData.cameraColor;

            // Each threadgroup works on 64x64 texels
            // uint32_t dimX = (m_Width + 63) / 64;
            // uint32_t dimY = (m_Height + 63) / 64;
            // vkCmdDispatch(cb, dimX, dimY, 1);
            var destinationDesc = renderGraph.GetTextureDesc(depthTexture);
            destinationDesc.enableRandomWrite = true;
            destinationDesc.depthBufferBits = 0;
            destinationDesc.useMipMap = true;
            destinationDesc.name = $"ColorPyramid";

            TextureHandle colorTexture = renderGraph.CreateTexture(destinationDesc);

            var exist = frameData.Contains<ColorPyramidData>();
            var resource = frameData.GetOrCreate<ColorPyramidData>();
            if (!exist)
            {
                resource.ColorTexture = colorTexture;
            }
            
            

            using (var builder = renderGraph.AddUnsafePass(nameof(ColorPyramidPass), out PassData passData))
            {
                // Set the pass data so the data can be transfered from the recording to the execution.
                builder.AllowPassCulling(false);
                passData.input = depthTexture;
                passData.output = colorTexture;
                passData.dimX = destinationDesc.width;
                passData.dimY = destinationDesc.height;
                passData.ColorPyramidData = resource;
                // UseBuffer is used to setup render graph dependencies together with read and write flags.
                builder.UseTexture(passData.input, AccessFlags.ReadWrite);
                builder.UseTexture(passData.output, AccessFlags.ReadWrite);
                builder.SetGlobalTextureAfterPass(passData.output, HierarchyZId);
                // The execution function is also call SetRenderfunc for compute passes.
                builder.SetRenderFunc((PassData data, UnsafeGraphContext cgContext) =>
                    ExecutePass(data, cgContext));
            }
        }


        static void ExecutePass(PassData data, UnsafeGraphContext cgContext)
        {
            var cmd = CommandBufferHelpers.GetNativeCommandBuffer(cgContext.cmd);
            // Blitter.BlitCameraTexture(cmd, data.input, data.output);
            //note:D3D11 Not support ResourcesBarrier

            data.ColorPyramidData.MipCount = MipGenerator.MipGenerator.Instance.RenderColorGaussianPyramid(cmd,
                new Vector2Int(data.dimX,
                    data.dimY), data.input, data.output);
        }
    }
}