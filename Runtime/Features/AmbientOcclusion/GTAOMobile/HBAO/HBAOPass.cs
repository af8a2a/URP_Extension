using Features.Utility;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using URP_Extension.Features.Utility;

namespace Features.AO.HBAO
{
    public class HBAOPass : ScriptableRenderPass
    {
        ComputeShader deinterleaveShader;
        int deinterleaveKernelID;

        ComputeShader NormalViewShader;
        int NormalViewKernelID;


        ComputeShader HBAOCalcShader;
        int HBAOCalcKernelID;

        ComputeShader HBAOReinterleaveShader;
        int HBAOReinterleaveKernelID;

        ComputeShader HBAOBlurShader;
        int HBAOBlurKernelID;

        public HBAOPass()
        {
            renderPassEvent = RenderPassEvent.AfterRenderingPrePasses+ 1;
            deinterleaveShader = Resources.Load<ComputeShader>("HBAODeinterleave");
            deinterleaveKernelID = deinterleaveShader.FindKernel("KMain");

            NormalViewShader = Resources.Load<ComputeShader>("HBAOViewNormal");
            NormalViewKernelID = NormalViewShader.FindKernel("KMain");

            HBAOCalcShader = Resources.Load<ComputeShader>("HBAOCalc");
            HBAOCalcKernelID = HBAOCalcShader.FindKernel("KMain");

            HBAOReinterleaveShader = Resources.Load<ComputeShader>("HBAOReinterleave");
            HBAOReinterleaveKernelID = HBAOReinterleaveShader.FindKernel("KMain");

            HBAOBlurShader = Resources.Load<ComputeShader>("HBAOBlur");
            HBAOBlurKernelID = HBAOBlurShader.FindKernel("KMain");
        }

        public void Setup()
        {
            ConfigureInput(ScriptableRenderPassInput.Normal | ScriptableRenderPassInput.Depth);
        }

        enum ProfileID
        {
            Deinterleave,
            NormalView,
            SSAOCalc,
            Reinterleave,
            SSAOBlur
        }

        class PassData
        {
            internal ComputeShader deinterleaveShader;
            internal int deinterleaveKernelID;

            internal ComputeShader NormalViewShader;
            internal int NormalViewKernelID;

            internal ComputeShader HBAOCalcShader;
            internal int HBAOCalcKernelID;

            internal ComputeShader HBAOReinterleaveShader;
            internal int HBAOReinterleaveKernelID;

            internal ComputeShader HBAOBlurShader;
            internal int HBAOBlurKernelID;


            internal HBAOSetting setting;

            internal float2 Dimension;
            internal float4 UVToView;
            internal Vector4[] Jitter; //16

            internal TextureHandle depthTexture;
            internal TextureHandle normalTexture;

            internal TextureHandle NormalViewTexture;
            internal TextureHandle deinterleaveDepthTexture;
            internal TextureHandle deinterleaveSSAOTexture;
            internal TextureHandle reinterleaveSSAOTexture;

            internal TextureHandle SSAOPing;
            internal TextureHandle SSAOPong;
        }


        private static class MersenneTwister
        {
            // Mersenne-Twister random numbers in [0,1).
            public static float[] Numbers = new float[]
            {
                //0.463937f,0.340042f,0.223035f,0.468465f,0.322224f,0.979269f,0.031798f,0.973392f,0.778313f,0.456168f,0.258593f,0.330083f,0.387332f,0.380117f,0.179842f,0.910755f,
                //0.511623f,0.092933f,0.180794f,0.620153f,0.101348f,0.556342f,0.642479f,0.442008f,0.215115f,0.475218f,0.157357f,0.568868f,0.501241f,0.629229f,0.699218f,0.707733f
                0.556725f, 0.005520f, 0.708315f, 0.583199f, 0.236644f, 0.992380f, 0.981091f, 0.119804f, 0.510866f, 0.560499f, 0.961497f, 0.557862f, 0.539955f,
                0.332871f, 0.417807f, 0.920779f,
                0.730747f, 0.076690f, 0.008562f, 0.660104f, 0.428921f, 0.511342f, 0.587871f, 0.906406f, 0.437980f, 0.620309f, 0.062196f, 0.119485f, 0.235646f,
                0.795892f, 0.044437f, 0.617311f
            };
        }


        static void ExecutePass(PassData data, ComputeGraphContext context)
        {
            var cmd = context.cmd;

            float2 QuarterResolution = new float2(data.Dimension.x / 4f, data.Dimension.y / 4f);
            float2 InvQuarterResolution = new float2(1.0f / (data.Dimension.x / 4f), 1.0f / (data.Dimension.y / 4f));

            using (new ProfilingScope(cmd, ProfilingSampler.Get(ProfileID.Deinterleave)))
            {
                cmd.SetComputeTextureParam(data.deinterleaveShader, data.deinterleaveKernelID, "DepthInput", data.depthTexture);
                cmd.SetComputeTextureParam(data.deinterleaveShader, data.deinterleaveKernelID, "DepthOutput", data.deinterleaveDepthTexture);
                var threadX = RenderingUtilsExt.DivRoundUp((int)data.Dimension.x, 8 * 4);
                var threadY = RenderingUtilsExt.DivRoundUp((int)data.Dimension.y, 8 * 4);
                cmd.DispatchCompute(data.deinterleaveShader, data.deinterleaveKernelID, threadX, threadY, 1);
            }

            // using (new ProfilingScope(cmd, ProfilingSampler.Get(ProfileID.NormalView)))
            // {
            //     cmd.SetComputeTextureParam(data.NormalViewShader, data.NormalViewKernelID, "NormalInput", data.normalTexture);
            //     cmd.SetComputeTextureParam(data.NormalViewShader, data.NormalViewKernelID, "NormalView", data.NormalViewTexture);
            //
            //     var threadX = RenderingUtilsExt.DivRoundUp((int)data.Dimension.x, 8);
            //     var threadY = RenderingUtilsExt.DivRoundUp((int)data.Dimension.y, 8);
            //     cmd.DispatchCompute(data.NormalViewShader, data.NormalViewKernelID, threadX, threadY, 1);
            // }

            using (new ProfilingScope(cmd, ProfilingSampler.Get(ProfileID.SSAOCalc)))
            {
                
                cmd.SetComputeTextureParam(data.HBAOCalcShader, data.HBAOCalcKernelID, "DepthInput", data.deinterleaveDepthTexture);
                cmd.SetComputeTextureParam(data.HBAOCalcShader, data.HBAOCalcKernelID, "NormalInput", data.normalTexture);
                cmd.SetComputeTextureParam(data.HBAOCalcShader, data.HBAOCalcKernelID, "HBAO", data.deinterleaveSSAOTexture);

                float R = data.setting.radius.value;
                float NDotVBias = R * 0.5f * data.setting.bias.value;
                float maxRadInPixels = Mathf.Max(16,
                    data.setting.maxRadiusPixels.value * Mathf.Sqrt(data.Dimension.x * data.Dimension.y / (1080.0f * 1920.0f)));
                maxRadInPixels /= 4;


                cmd.SetComputeFloatParam(data.HBAOCalcShader, "MaxDistance", data.setting.maxDistance.value);
                cmd.SetComputeFloatParam(data.HBAOCalcShader, "DistanceFalloff", data.setting.distanceFalloff.value);

                cmd.SetComputeFloatParam(data.HBAOCalcShader, "Radius", R);
                cmd.SetComputeFloatParam(data.HBAOCalcShader, "R2", R * R);
                cmd.SetComputeFloatParam(data.HBAOCalcShader, "NegInvR2", -1.0f / (R * R));

                cmd.SetComputeFloatParam(data.HBAOCalcShader, "NDotVBias", NDotVBias);
                cmd.SetComputeFloatParam(data.HBAOCalcShader, "PowExponent", math.max(data.setting.intensity.value, 0));
                cmd.SetComputeFloatParam(data.HBAOCalcShader, "AOMultiplier", 1.0f / (1.0f - NDotVBias));
                cmd.SetComputeFloatParam(data.HBAOCalcShader, "MaxRadiusPixels", maxRadInPixels);


                cmd.SetComputeVectorParam(data.HBAOCalcShader, "InvFullResolution", data.Dimension.xyxx);
                cmd.SetComputeVectorParam(data.HBAOCalcShader, "InvQuarterResolution", InvQuarterResolution.xyxx);
                cmd.SetComputeVectorParam(data.HBAOCalcShader, "_SSAO_UVToView", data.UVToView);
                cmd.SetComputeVectorArrayParam(data.HBAOCalcShader, "jitters", data.Jitter);

                var threadX = RenderingUtilsExt.DivRoundUp((int)QuarterResolution.x, 8);
                var threadY = RenderingUtilsExt.DivRoundUp((int)QuarterResolution.y, 8);
                cmd.DispatchCompute(data.HBAOCalcShader, data.HBAOCalcKernelID, threadX, threadY, 16);
            }

            using (new ProfilingScope(cmd, ProfilingSampler.Get(ProfileID.Reinterleave)))
            {
                cmd.SetComputeTextureParam(data.HBAOReinterleaveShader, data.HBAOReinterleaveKernelID, "AOInput", data.deinterleaveSSAOTexture);
                cmd.SetComputeTextureParam(data.HBAOReinterleaveShader, data.HBAOReinterleaveKernelID, "DepthOutput", data.reinterleaveSSAOTexture);

                var threadX = RenderingUtilsExt.DivRoundUp((int)QuarterResolution.x, 8);
                var threadY = RenderingUtilsExt.DivRoundUp((int)QuarterResolution.y, 8);

                cmd.DispatchCompute(data.HBAOReinterleaveShader, data.HBAOReinterleaveKernelID, threadX, threadY, 1);
            }

            using (new ProfilingScope(cmd, ProfilingSampler.Get(ProfileID.SSAOBlur)))
            {
                cmd.SetComputeTextureParam(data.HBAOBlurShader, data.HBAOBlurKernelID, "AODepthInput", data.reinterleaveSSAOTexture);
                cmd.SetComputeTextureParam(data.HBAOBlurShader, data.HBAOBlurKernelID, "SSAOOutput", data.SSAOPing);
                cmd.SetComputeFloatParam(data.HBAOBlurShader, "Sharpness", data.setting.sharpness.value * 100);
                cmd.SetComputeVectorParam(data.HBAOBlurShader, "InvResolutionDirection", new Vector4(1.0f / data.Dimension.x, 0, 0, 0));

                var threadX = RenderingUtilsExt.DivRoundUp((int)data.Dimension.x, 8);
                var threadY = RenderingUtilsExt.DivRoundUp((int)data.Dimension.y, 8);

                cmd.DispatchCompute(data.HBAOBlurShader, data.HBAOBlurKernelID, threadX, threadY, 1);
                cmd.SetComputeVectorParam(data.HBAOBlurShader, "InvResolutionDirection", new Vector4(0, 1.0f / data.Dimension.y, 0, 0));

                cmd.SetComputeTextureParam(data.HBAOBlurShader, data.HBAOBlurKernelID, "AODepthInput", data.SSAOPing);
                cmd.SetComputeTextureParam(data.HBAOBlurShader, data.HBAOBlurKernelID, "SSAOOutput", data.SSAOPong);


                cmd.DispatchCompute(data.HBAOBlurShader, data.HBAOBlurKernelID, threadX, threadY, 1);
            }

            cmd.SetKeyword(ShaderGlobalKeywords.ScreenSpaceOcclusion, true);
            cmd.SetGlobalVector("_AmbientOcclusionParam",
                new Vector4(1f, 0f, 0f, data.setting.directLightingStrength.value));
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            using (var builder = renderGraph.AddComputePass<PassData>("HBAO", out var data))
            {
                var cameraData = frameData.Get<UniversalCameraData>();
                var resourceData = frameData.Get<UniversalResourceData>();

                var depthTexture = resourceData.cameraDepthTexture;
                var normalTexture = resourceData.cameraNormalsTexture;

                var setting = VolumeManager.instance.stack.GetComponent<HBAOSetting>();
                if (!setting || !setting.IsActive())
                {
                    return;
                }

                
                // var normalViewTexture = builder.CreateTransientTexture(new TextureDesc(Vector2.one)
                // {
                //     enableRandomWrite = true,
                //     colorFormat = GraphicsFormat.R8G8B8A8_SNorm
                // });

                var deinterleaveTexture = builder.CreateTransientTexture(new TextureDesc(Vector2.one * 0.25f)
                {
                    enableRandomWrite = true,
                    dimension = TextureDimension.Tex2DArray,
                    slices = 16,
                    colorFormat = GraphicsFormat.R32_SFloat
                });


                var deinterleaveSSAOTexture = builder.CreateTransientTexture(new TextureDesc(Vector2.one * 0.25f)
                {
                    enableRandomWrite = true,
                    dimension = TextureDimension.Tex2DArray,
                    slices = 16,
                    colorFormat = GraphicsFormat.R16G16_SFloat
                });

                var reinterleaveSSAOTexture = builder.CreateTransientTexture(new TextureDesc(Vector2.one)
                {
                    enableRandomWrite = true,
                    colorFormat = GraphicsFormat.R16G16_SFloat
                });
                var SSAOPing = builder.CreateTransientTexture(new TextureDesc(Vector2.one)
                {
                    enableRandomWrite = true,
                    colorFormat = GraphicsFormat.R16G16_SFloat
                });
                var SSAOPong = renderGraph.CreateTexture(new TextureDesc(Vector2.one)
                {
                    enableRandomWrite = true,
                    colorFormat = GraphicsFormat.R16_SFloat
                });


                var camera = cameraData.camera;

                data.setting = setting;

                data.Dimension = new float2(camera.pixelWidth, camera.pixelHeight);

                var projMatrix = cameraData.GetProjectionMatrix();
                float invTanHalfFOVxAR = projMatrix.m00; // m00 => 1.0f / (tanHalfFOV * aspectRatio)
                float invTanHalfFOV = projMatrix.m11; // m11 => 1.0f / tanHalfFOV
                data.UVToView = new Vector4(2.0f / invTanHalfFOVxAR, -2.0f / invTanHalfFOV, -1.0f / invTanHalfFOVxAR, 1.0f / invTanHalfFOV);

                var jitter = new Vector4[4 * 4];
                for (int i = 0, j = 0; i < jitter.Length; ++i)
                {
                    float r1 = MersenneTwister.Numbers[j++];
                    float r2 = MersenneTwister.Numbers[j++];
                    jitter[i] = new Vector2(r1, r2);
                }

                data.Jitter = jitter;


                data.depthTexture = depthTexture;
                data.normalTexture = normalTexture;
                data.deinterleaveDepthTexture = deinterleaveTexture;
                // data.NormalViewTexture = normalViewTexture;
                data.deinterleaveSSAOTexture = deinterleaveSSAOTexture;
                data.reinterleaveSSAOTexture = reinterleaveSSAOTexture;
                data.SSAOPing = SSAOPing;
                data.SSAOPong = SSAOPong;


                data.deinterleaveShader = deinterleaveShader;
                data.deinterleaveKernelID = deinterleaveKernelID;

                data.NormalViewShader = NormalViewShader;
                data.NormalViewKernelID = NormalViewKernelID;

                data.HBAOCalcShader = HBAOCalcShader;
                data.HBAOCalcKernelID = HBAOCalcKernelID;


                data.HBAOReinterleaveShader = HBAOReinterleaveShader;
                data.HBAOReinterleaveKernelID = HBAOReinterleaveKernelID;


                data.HBAOBlurShader = HBAOBlurShader;
                data.HBAOBlurKernelID = HBAOBlurKernelID;

                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);
                builder.UseTexture(data.depthTexture);
                builder.UseTexture(data.normalTexture);
                builder.UseTexture(data.deinterleaveDepthTexture, AccessFlags.ReadWrite);
                // builder.UseTexture(data.NormalViewTexture, AccessFlags.ReadWrite);
                builder.UseTexture(data.deinterleaveSSAOTexture, AccessFlags.ReadWrite);
                builder.UseTexture(data.reinterleaveSSAOTexture, AccessFlags.ReadWrite);
                builder.UseTexture(data.SSAOPing, AccessFlags.ReadWrite);
                builder.UseTexture(data.SSAOPong, AccessFlags.ReadWrite);
                builder.SetGlobalTextureAfterPass(data.SSAOPong, Shader.PropertyToID("_ScreenSpaceOcclusionTexture"));

                builder.SetRenderFunc((PassData passdata, ComputeGraphContext context) => ExecutePass(passdata, context));
                resourceData.ssaoTexture = SSAOPong;
            }
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.ScreenSpaceOcclusion, false);
        }
    }
}