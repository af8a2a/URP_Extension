using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using URP_Extension.Features.Utility;

namespace URP_Extension.Features.ScreenSpaceRaytracing.StochasticScreenSpaceReflection
{
    public partial class StochasticScreenSpaceReflectionPass
    {
        static void ExecuteClassifyTilesPass(SSRPassData data, ComputeCommandBuffer cmd)
        {
            using (new ProfilingScope(cmd, m_SSRClassifyTilesProfilingSampler))
            {
                cmd.SetComputeTextureParam(data.cs, data.classifyTilesKernel, "_GBuffer2",
                    data.gbuffer2);

                cmd.SetComputeBufferParam(data.cs, data.classifyTilesKernel, "gDispatchIndirectBuffer",
                    data.dispatchIndirectBuffer);
                cmd.SetComputeBufferParam(data.cs, data.classifyTilesKernel, "gTileList",
                    data.tileListBuffer);

                cmd.DispatchCompute(data.cs, data.classifyTilesKernel,
                    RenderingUtilsExt.DivRoundUp(data.TraceTextureSize.x, 8),
                    RenderingUtilsExt.DivRoundUp(data.TraceTextureSize.y, 8), 1);
            }
        }
    }
    // public class SSSRClassifyTilesPass : ScriptableRenderPass
    // {
    //     private static string m_SSRClassifyTilesProfilerTag = "SSRClassifyTiles";
    //
    //     private static ProfilingSampler SSRClassifyTilesProfilingSampler =
    //         new ProfilingSampler(m_SSRClassifyTilesProfilerTag);
    //
    //     // Private Variables
    //     private ComputeShader computeShader;
    //     private int kernel;
    //
    //     internal class PassData
    //     {
    //         internal ComputeShader cs;
    //         internal int classifyTilesKernel;
    //
    //         // Classify tiles
    //         internal TextureHandle gbuffer2;
    //         internal BufferHandle dispatchIndirectBuffer;
    //         internal BufferHandle tileListBuffer;
    //         internal Vector2Int TextureSize;
    //     }
    //
    //     public SSSRClassifyTilesPass()
    //     {
    //         this.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
    //         computeShader = Resources.Load<ComputeShader>("ScreenSpaceReflections");
    //         kernel = computeShader.FindKernel("ScreenSpaceReflectionsClassifyTiles");
    //     }
    //
    //     static void ExecutePass(PassData data, ComputeCommandBuffer cmd)
    //     {
    //         using (new ProfilingScope(cmd, SSRClassifyTilesProfilingSampler))
    //         {
    //             cmd.SetComputeTextureParam(data.cs, data.classifyTilesKernel, "_GBuffer2",
    //                 data.gbuffer2);
    //
    //             cmd.SetComputeBufferParam(data.cs, data.classifyTilesKernel, "gDispatchIndirectBuffer",
    //                 data.dispatchIndirectBuffer);
    //             cmd.SetComputeBufferParam(data.cs, data.classifyTilesKernel, "gTileList",
    //                 data.tileListBuffer);
    //
    //             cmd.DispatchCompute(data.cs, data.classifyTilesKernel,
    //                 RenderingUtilsExt.DivRoundUp(data.TextureSize.x, 8),
    //                 RenderingUtilsExt.DivRoundUp(data.TextureSize.y, 8), 1);
    //         }
    //     }
    //
    //     public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    //     {
    //         UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
    //         UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
    //
    //
    //         // Set passData
    //         using (var builder = renderGraph.AddComputePass("Render SSR", out PassData passData))
    //         {
    //             TextureDesc texDesc = new TextureDesc(cameraData.cameraTargetDescriptor);
    //             texDesc.msaaSamples = MSAASamples.None;
    //             texDesc.depthBufferBits = DepthBits.None;
    //             texDesc.enableRandomWrite = true;
    //             texDesc.filterMode = FilterMode.Point;
    //             texDesc.wrapMode = TextureWrapMode.Clamp;
    //
    //
    //             var tileListBufferDesc = new BufferDesc(
    //                 RenderingUtilsExt.DivRoundUp(texDesc.width, 8) * RenderingUtilsExt.DivRoundUp(texDesc.height, 8),
    //                 sizeof(uint))
    //             {
    //                 name = "SSRTileListBuffer"
    //             };
    //             var dispatchIndirect = GraphicsBufferSystem.instance.GetGraphicsBuffer<uint>(
    //                 GraphicsBufferSystemBufferID.SSRDispatchIndirectBuffer, 3, "SSRDispatIndirectBuffer",
    //                 GraphicsBuffer.Target.IndirectArguments);
    //
    //
    //             passData.dispatchIndirectBuffer = renderGraph.ImportBuffer(dispatchIndirect);
    //             passData.tileListBuffer = renderGraph.CreateBuffer(tileListBufferDesc);
    //             passData.gbuffer2 = resourceData.gBuffer[2];
    //             passData.classifyTilesKernel = kernel;
    //             passData.TextureSize = new Vector2Int(texDesc.width, texDesc.height);
    //             passData.cs = computeShader;
    //
    //
    //             // Declare input/output textures
    //             builder.UseBuffer(passData.dispatchIndirectBuffer, AccessFlags.ReadWrite);
    //             builder.UseBuffer(passData.tileListBuffer, AccessFlags.ReadWrite);
    //             builder.UseTexture(resourceData.gBuffer[2]); // Normal GBuffer
    //
    //             builder.AllowPassCulling(false);
    //             builder.AllowGlobalStateModification(true);
    //             builder.SetRenderFunc((PassData data, ComputeGraphContext context) =>
    //             {
    //                 ExecutePass(data, context.cmd);
    //             });
    //         }
    //     }
    // }
}