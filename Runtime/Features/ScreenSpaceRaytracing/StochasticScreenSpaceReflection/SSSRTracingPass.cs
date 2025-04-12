// using UnityEngine;
// using UnityEngine.Rendering;
// using UnityEngine.Rendering.RenderGraphModule;
// using UnityEngine.Rendering.Universal;
// using URP_Extension.Features.Utility;
//
// namespace URP_Extension.Features.ScreenSpaceRaytracing.StochasticScreenSpaceReflection
// {
//     public class SSSRTracingPass : ScriptableRenderPass
//     {
//         private static string m_SSRTracingProfilerTag = "SSRTracing";
//         private static ProfilingSampler SSSRTracingProfilingSampler = new ProfilingSampler(m_SSRTracingProfilerTag);
//
//         private ComputeShader computeShader;
//         private int kernel;
//
//         internal class PassData
//         {
//             internal ComputeShader cs;
//             internal int tracingKernel;
//             internal int camHistoryFrameCount;
//
//
//             internal TextureHandle cameraDepthTexture;
//             internal TextureHandle depthPyramidTexture;
//             internal int depthPyramidMipLevel;
//
//             internal TextureHandle motionVectorTexture;
//             internal TextureHandle prevColorPyramidTexture;
//             internal TextureHandle rayHitColorTexture;
//             internal TextureHandle hitPointTexture;
//
//             
//             internal TextureHandle blueNoiseArray;
//             internal TextureHandle ssrLightingTexture;
//             internal TextureHandle rayInfoTexture;
//             internal TextureHandle rayDirTexture;
//
//             internal TextureHandle gbuffer2;
//             internal BufferHandle dispatchIndirectBuffer;
//             internal BufferHandle tileListBuffer;
//             internal Vector2Int TextureSize;
//
//             internal ScreenSpaceReflectionAlgorithm usedAlgo;
//         }
//
//         public SSSRTracingPass()
//         {
//             this.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
//             computeShader = Resources.Load<ComputeShader>("ScreenSpaceReflections");
//             kernel = computeShader.FindKernel("ScreenSpaceReflectionsTracing");
//         }
//
//         static void ExecutePass(PassData data, ComputeCommandBuffer cmd)
//         {
//             
//             var hitColorHandle = data.rayHitColorTexture;
//             
//             if (data.usedAlgo == ScreenSpaceReflectionAlgorithm.Approximation)
//             {
//                 data.cs.EnableKeyword("SSR_APPROX");
//                 hitColorHandle = data.ssrLightingTexture;
//             }
//             else
//             {
//                 data.cs.DisableKeyword("SSR_APPROX");
//             }
//             
//
//             
//             using (new ProfilingScope(cmd, SSSRTracingProfilingSampler))
//             {
//                 BlueNoiseSystem.BindSTBNParams(BlueNoiseTexFormat._128RG, cmd, data.cs, data.tracingKernel,
//                     data.blueNoiseArray, data.camHistoryFrameCount);
//                 cmd.SetComputeTextureParam(data.cs, data.tracingKernel, "_CameraDepthPyramidTexture",
//                     data.depthPyramidTexture);
//                 cmd.SetComputeTextureParam(data.cs, data.tracingKernel, "_SSRRayInfoTexture",
//                     data.rayInfoTexture);
//
//                 cmd.SetComputeTextureParam(data.cs, data.tracingKernel, "_ColorPyramidTexture",
//                     data.prevColorPyramidTexture);
//                 cmd.SetComputeTextureParam(data.cs, data.tracingKernel, "_CameraMotionVectorsTexture",
//                     data.motionVectorTexture);
//                 cmd.SetComputeTextureParam(data.cs, data.tracingKernel, "_RayHitColorTexture",
//                     hitColorHandle);
//                 // cmd.SetComputeTextureParam(data.cs, data.tracingKernel, _SkyTexture, data.reflectProbe);
//
//
//                 // cmd.SetComputeBufferParam(data.cs, data.tracingKernel, ShaderConstants._DepthPyramidMipLevelOffsets,
//                 //     data.depthPyramidMipLevelOffsets);
//                 cmd.SetComputeBufferParam(data.cs, data.tracingKernel, "gTileList", data.tileListBuffer);
//
//                 cmd.DispatchCompute(data.cs, data.tracingKernel, data.dispatchIndirectBuffer, 0);
//             }
//         }
//
//         public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
//         {
//             UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
//             UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
//
//
//             // Set passData
//             using (var builder = renderGraph.AddComputePass("Render SSR", out PassData passData))
//             {
//                 TextureDesc texDesc = new TextureDesc(cameraData.cameraTargetDescriptor);
//                 texDesc.msaaSamples = MSAASamples.None;
//                 texDesc.depthBufferBits = DepthBits.None;
//                 texDesc.enableRandomWrite = true;
//                 texDesc.filterMode = FilterMode.Point;
//                 texDesc.wrapMode = TextureWrapMode.Clamp;
//
//
//                 var tileListBufferDesc = new BufferDesc(
//                     RenderingUtilsExt.DivRoundUp(texDesc.width, 8) * RenderingUtilsExt.DivRoundUp(texDesc.height, 8),
//                     sizeof(uint))
//                 {
//                     name = "SSRTileListBuffer"
//                 };
//                 var dispatchIndirect = GraphicsBufferSystem.instance.GetGraphicsBuffer<uint>(
//                     GraphicsBufferSystemBufferID.SSRDispatchIndirectBuffer, 3, "SSRDispatIndirectBuffer",
//                     GraphicsBuffer.Target.IndirectArguments);
//
//
//                 passData.dispatchIndirectBuffer = renderGraph.ImportBuffer(dispatchIndirect);
//                 passData.tileListBuffer = renderGraph.CreateBuffer(tileListBufferDesc);
//                 passData.gbuffer2 = resourceData.gBuffer[2];
//                 passData.classifyTilesKernel = kernel;
//                 passData.TextureSize = new Vector2Int(texDesc.width, texDesc.height);
//                 passData.cs = computeShader;
//
//
//                 // Declare input/output textures
//                 builder.UseBuffer(passData.dispatchIndirectBuffer, AccessFlags.ReadWrite);
//                 builder.UseBuffer(passData.tileListBuffer, AccessFlags.ReadWrite);
//                 builder.UseTexture(resourceData.gBuffer[2]); // Normal GBuffer
//
//                 builder.AllowPassCulling(false);
//                 builder.AllowGlobalStateModification(true);
//                 builder.SetRenderFunc((PassData data, ComputeGraphContext context) =>
//                 {
//                     ExecutePass(data, context.cmd);
//                 });
//             }
//         }
//     }
// }