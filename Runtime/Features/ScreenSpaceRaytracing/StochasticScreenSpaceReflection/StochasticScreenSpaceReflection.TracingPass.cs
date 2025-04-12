using UnityEngine.Rendering;
using URP_Extension.Features.Utility;

namespace URP_Extension.Features.ScreenSpaceRaytracing.StochasticScreenSpaceReflection
{
    public partial class StochasticScreenSpaceReflectionPass
    {
        static void ExecuteTracingPass(SSRPassData data, ComputeCommandBuffer cmd)
        {
            var hitColorHandle = data.rayHitColorTexture;
            var currAccumHandle = data.currAccumulateTexture;
            var prevAccumHandle = data.prevAccumulateTexture;

            // if (data.usedAlgo == ScreenSpaceReflectionAlgorithm.Approximation)
            // {
            //     data.cs.EnableKeyword("SSR_APPROX");
            //     hitColorHandle = data.ssrLightingTexture;
            // }
            // else
            // {
            //     data.cs.DisableKeyword("SSR_APPROX");
            // }

            // // ScreenSpace Tracing
            using (new ProfilingScope(cmd, m_SSRTracingProfilingSampler))
            {
                BlueNoiseSystem.BindSTBNParams(BlueNoiseTexFormat._128RG, cmd, data.cs, data.tracingKernel,
                    data.blueNoiseArray, data.camHistoryFrameCount);
                cmd.SetComputeTextureParam(data.cs, data.tracingKernel,
                    ShaderConstants._CameraDepthPyramidTexture,
                    data.depthPyramidTexture);
                cmd.SetComputeTextureParam(data.cs, data.tracingKernel, ShaderConstants._SSRRayInfoTexture,
                    data.rayInfoTexture);

                cmd.SetComputeTextureParam(data.cs, data.tracingKernel,
                    ShaderConstants._ColorPyramidTexture,
                    data.prevColorPyramidTexture);
                cmd.SetComputeTextureParam(data.cs, data.tracingKernel,
                    ShaderConstants._CameraMotionVectorsTexture,
                    data.motionVectorTexture);
                cmd.SetComputeTextureParam(data.cs, data.tracingKernel, ShaderConstants._RayHitColorTexture,
                    hitColorHandle);

                cmd.SetComputeTextureParam(data.cs, data.tracingKernel, ShaderConstants._SkyTexture,
                    data.reflectProbe);

                // cmd.SetComputeBufferParam(data.cs, data.tracingKernel, ShaderConstants._DepthPyramidMipLevelOffsets,
                //     data.depthPyramidMipLevelOffsets);
                cmd.SetComputeBufferParam(data.cs, data.tracingKernel, ShaderConstants.gTileList,
                    data.tileListBuffer);
                cmd.SetComputeBufferParam(data.cs, data.tracingKernel, ShaderConstants._DispatchRayCoordBuffer,
                    data.tileListBuffer);

                cmd.DispatchCompute(data.cs, data.tracingKernel, data.dispatchIndirectBuffer, 0);
            }
        }
    }
}