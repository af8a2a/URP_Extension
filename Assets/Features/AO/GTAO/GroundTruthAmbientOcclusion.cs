using System;
using Features.Filter.TemporalDenoiser;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

//reference by https://github.com/bladesero/GTAO_URP
namespace Features.AO.GTAO
{
    [Serializable]
    internal class ScreenSpaceAmbientOcclusionSettings
    {
        // Parameters
        [SerializeField] internal bool Downsample = false;
        [SerializeField] internal bool AfterOpaque = false;
        [SerializeField] internal DepthSource Source = DepthSource.DepthNormals;
        [SerializeField] internal NormalQuality NormalSamples = NormalQuality.Medium;
        [SerializeField] internal float Intensity = 3.0f;
        [SerializeField] internal float DirectLightingStrength = 0.25f;
        [SerializeField] internal float Radius = 0.035f;
        [SerializeField] internal int SampleCount = 4;

        // Enums
        internal enum DepthSource
        {
            Depth = 0,
            DepthNormals = 1
        }

        internal enum NormalQuality
        {
            Low,
            Medium,
            High
        }
    }

    [DisallowMultipleRendererFeature]
    [Tooltip(
        "The Ambient Occlusion effect darkens creases, holes, intersections and surfaces that are close to each other.")]
    internal class GroundTruthAmbientOcclusion : ScriptableRendererFeature
    {
        // Serialized Fields
        [SerializeField, HideInInspector] private Shader m_Shader = null;

        [SerializeField]
        private ScreenSpaceAmbientOcclusionSettings m_Settings = new ScreenSpaceAmbientOcclusionSettings();

        // Private Fields
        private Material m_Material;
        private ScreenSpaceAmbientOcclusionPass m_SSAOPass = null;
        // Constants
        private const string k_ShaderName = "Hidden/Universal Render Pipeline/GroundTruthAmbientOcclusion";
        private const string k_OrthographicCameraKeyword = "_ORTHOGRAPHIC";
        private const string k_NormalReconstructionLowKeyword = "_RECONSTRUCT_NORMAL_LOW";
        private const string k_NormalReconstructionMediumKeyword = "_RECONSTRUCT_NORMAL_MEDIUM";
        private const string k_NormalReconstructionHighKeyword = "_RECONSTRUCT_NORMAL_HIGH";
        private const string k_SourceDepthKeyword = "_SOURCE_DEPTH";
        private const string k_SourceDepthNormalsKeyword = "_SOURCE_DEPTH_NORMALS";

        internal bool afterOpaque => m_Settings.AfterOpaque;

        /// <inheritdoc/>
        public override void Create()
        {
            // Create the pass...
            if (m_SSAOPass == null)
            {
                m_SSAOPass = new ScreenSpaceAmbientOcclusionPass();
            }


            GetMaterial();
        }
        
        /// <inheritdoc/>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!GetMaterial())
            {
                Debug.LogErrorFormat(
                    "{0}.AddRenderPasses(): Missing material. {1} render pass will not be added. Check for missing reference in the renderer resources.",
                    GetType().Name, name);
                return;
            }
    
            bool shouldAdd = m_SSAOPass.Setup(m_Settings,  m_Material);
            if (renderingData.cameraData.isPreviewCamera||renderingData.cameraData.postProcessEnabled==false)
            {
                return;
            }

            if (shouldAdd)
            {
                renderer.EnqueuePass(m_SSAOPass);
            }
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(m_Material);
        }

        private bool GetMaterial()
        {
            if (m_Material != null)
            {
                return true;
            }

            if (m_Shader == null)
            {
                m_Shader = Shader.Find(k_ShaderName);
                if (m_Shader == null)
                {
                    return false;
                }
            }

            m_Material = CoreUtils.CreateEngineMaterial(m_Shader);

            return m_Material != null;
        }

        // The SSAO Pass
        private class ScreenSpaceAmbientOcclusionPass : ScriptableRenderPass
        {
            // Properties
            //private bool isRendererDeferred => m_Renderer != null && m_Renderer is UniversalRenderer && ((UniversalRenderer)m_Renderer).renderingMode == RenderingMode.Deferred;
            private bool isRendererDeferred = false;

            // Private Variables
            private bool m_SupportsR8RenderTextureFormat =
                SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.R8);

            private Material m_Material;
            private Vector4[] m_CameraTopLeftCorner = new Vector4[2];
            private Vector4[] m_CameraXExtent = new Vector4[2];
            private Vector4[] m_CameraYExtent = new Vector4[2];
            private Vector4[] m_CameraZExtent = new Vector4[2];
            private Matrix4x4[] m_CameraViewProjections = new Matrix4x4[2];
            private ProfilingSampler m_ProfilingSampler = new ProfilingSampler("GTAO");

            private RTHandle m_AOPassRT;
            private RTHandle m_BlurHorizonRT;
            private RTHandle m_BlurVerticalRT;

            private RTHandle m_FinalRT;
            public RTHandle TargetRT => m_FinalRT;
            private ScreenSpaceAmbientOcclusionSettings m_CurrentSettings;

            // Constants
            private const string k_SSAOTextureName = "_ScreenSpaceOcclusionTexture";
            private const string k_SSAOAmbientOcclusionParamName = "_AmbientOcclusionParam";

            // Statics

            private static readonly int s_SSAOParamsID = Shader.PropertyToID("_SSAOParams");


            private static readonly int s_CameraViewXExtentID = Shader.PropertyToID("_CameraViewXExtent");
            private static readonly int s_CameraViewYExtentID = Shader.PropertyToID("_CameraViewYExtent");
            private static readonly int s_CameraViewZExtentID = Shader.PropertyToID("_CameraViewZExtent");
            private static readonly int s_ProjectionParams2ID = Shader.PropertyToID("_ProjectionParams2");
            private static readonly int s_CameraViewProjectionsID = Shader.PropertyToID("_CameraViewProjections");

            private static readonly int s_CameraViewTopLeftCornerID =
                Shader.PropertyToID("_CameraViewTopLeftCorner");

            private static readonly int SSAO_UVToView_ID = Shader.PropertyToID("_SSAO_UVToView");
            private TemporalDenoiser m_TemporalDenoiser = null;

            private enum ShaderPasses
            {
                AO = 0,
                BlurHorizontal = 1,
                BlurVertical = 2,
                BlurFinal = 3,
                AfterOpaque = 4
            }

            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                
            }

            internal ScreenSpaceAmbientOcclusionPass()
            {
                m_CurrentSettings = new ScreenSpaceAmbientOcclusionSettings();
                m_TemporalDenoiser = new TemporalDenoiser();
            }

            internal bool Setup(ScreenSpaceAmbientOcclusionSettings featureSettings,Material material)
            {
                m_Material = material;
                m_CurrentSettings = featureSettings;
                ScreenSpaceAmbientOcclusionSettings.DepthSource source;
                if (isRendererDeferred)
                {
                    renderPassEvent = featureSettings.AfterOpaque
                        ? RenderPassEvent.AfterRenderingOpaques
                        : RenderPassEvent.AfterRenderingGbuffer;
                    source = ScreenSpaceAmbientOcclusionSettings.DepthSource.DepthNormals;
                }
                else
                {
                    // Rendering after PrePasses is usually correct except when depth priming is in play:
                    // then we rely on a depth resolve taking place after the PrePasses in order to have it ready for SSAO.
                    // Hence we set the event to RenderPassEvent.AfterRenderingPrePasses + 1 at the earliest.
                    renderPassEvent = featureSettings.AfterOpaque
                        ? RenderPassEvent.AfterRenderingOpaques
                        : RenderPassEvent.AfterRenderingPrePasses + 1;
                    source = m_CurrentSettings.Source;
                }


                switch (source)
                {
                    case ScreenSpaceAmbientOcclusionSettings.DepthSource.Depth:
                        ConfigureInput(ScriptableRenderPassInput.Depth|ScriptableRenderPassInput.Motion);
                        break;
                    case ScreenSpaceAmbientOcclusionSettings.DepthSource.DepthNormals:
                        ConfigureInput(ScriptableRenderPassInput
                            .Normal|ScriptableRenderPassInput.Motion); // need depthNormal prepass for forward-only geometry
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                return m_Material != null
                       && m_CurrentSettings.Intensity > 0.0f
                       && m_CurrentSettings.Radius > 0.0f
                       && m_CurrentSettings.SampleCount > 0;
            }

            /// <inheritdoc/>
            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                RenderTextureDescriptor cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
                int downsampleDivider = m_CurrentSettings.Downsample ? 2 : 1;

                // Update SSAO parameters in the material
                Vector4 ssaoParams = new Vector4(
                    m_CurrentSettings.Intensity, // Intensity
                    m_CurrentSettings.Radius, // Radius
                    1.0f / downsampleDivider, // Downsampling
                    m_CurrentSettings.SampleCount // Sample count
                );
                m_Material.SetVector(s_SSAOParamsID, ssaoParams);

#if ENABLE_VR && ENABLE_XR_MODULE
                int eyeCount =
 renderingData.cameraData.xr.enabled && renderingData.cameraData.xr.singlePassEnabled ? 2 : 1;
#else
                int eyeCount = 1;
#endif
                for (int eyeIndex = 0; eyeIndex < eyeCount; eyeIndex++)
                {
                    Matrix4x4 view = renderingData.cameraData.GetViewMatrix(eyeIndex);
                    Matrix4x4 proj = renderingData.cameraData.GetProjectionMatrix(eyeIndex);
                    m_CameraViewProjections[eyeIndex] = proj * view;

                    // camera view space without translation, used by SSAO.hlsl ReconstructViewPos() to calculate view vector.
                    Matrix4x4 cview = view;
                    cview.SetColumn(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
                    Matrix4x4 cviewProj = proj * cview;
                    Matrix4x4 cviewProjInv = cviewProj.inverse;

                    Vector4 topLeftCorner = cviewProjInv.MultiplyPoint(new Vector4(-1, 1, -1, 1));
                    Vector4 topRightCorner = cviewProjInv.MultiplyPoint(new Vector4(1, 1, -1, 1));
                    Vector4 bottomLeftCorner = cviewProjInv.MultiplyPoint(new Vector4(-1, -1, -1, 1));
                    Vector4 farCentre = cviewProjInv.MultiplyPoint(new Vector4(0, 0, 1, 1));
                    m_CameraTopLeftCorner[eyeIndex] = topLeftCorner;
                    m_CameraXExtent[eyeIndex] = topRightCorner - topLeftCorner;
                    m_CameraYExtent[eyeIndex] = bottomLeftCorner - topLeftCorner;
                    m_CameraZExtent[eyeIndex] = farCentre;
                }

                float fovRad = renderingData.cameraData.camera.fieldOfView * Mathf.Deg2Rad;
                float invHalfTanFov = 1 / Mathf.Tan(fovRad * 0.5f);
                Vector2 focalLen =
                    new Vector2(
                        invHalfTanFov *
                        (((float)renderingData.cameraData.camera.pixelHeight / (float)downsampleDivider) /
                         ((float)renderingData.cameraData.camera.pixelWidth / (float)downsampleDivider)),
                        invHalfTanFov);
                Vector2 invFocalLen = new Vector2(1 / focalLen.x, 1 / focalLen.y);

                m_Material.SetVector(SSAO_UVToView_ID,
                    new Vector4(2 * invFocalLen.x, 2 * invFocalLen.y, -1 * invFocalLen.x, -1 * invFocalLen.y));
                m_Material.SetVector(s_ProjectionParams2ID,
                    new Vector4(1.0f / renderingData.cameraData.camera.nearClipPlane, 0.0f, 0.0f, 0.0f));
                m_Material.SetMatrixArray(s_CameraViewProjectionsID, m_CameraViewProjections);
                m_Material.SetVectorArray(s_CameraViewTopLeftCornerID, m_CameraTopLeftCorner);
                m_Material.SetVectorArray(s_CameraViewXExtentID, m_CameraXExtent);
                m_Material.SetVectorArray(s_CameraViewYExtentID, m_CameraYExtent);
                m_Material.SetVectorArray(s_CameraViewZExtentID, m_CameraZExtent);

                // Update keywords
                CoreUtils.SetKeyword(m_Material, k_OrthographicCameraKeyword,
                    renderingData.cameraData.camera.orthographic);

                ScreenSpaceAmbientOcclusionSettings.DepthSource source = this.isRendererDeferred
                    ? ScreenSpaceAmbientOcclusionSettings.DepthSource.DepthNormals
                    : m_CurrentSettings.Source;

                if (source == ScreenSpaceAmbientOcclusionSettings.DepthSource.Depth)
                {
                    switch (m_CurrentSettings.NormalSamples)
                    {
                        case ScreenSpaceAmbientOcclusionSettings.NormalQuality.Low:
                            CoreUtils.SetKeyword(m_Material, k_NormalReconstructionLowKeyword, true);
                            CoreUtils.SetKeyword(m_Material, k_NormalReconstructionMediumKeyword, false);
                            CoreUtils.SetKeyword(m_Material, k_NormalReconstructionHighKeyword, false);
                            break;
                        case ScreenSpaceAmbientOcclusionSettings.NormalQuality.Medium:
                            CoreUtils.SetKeyword(m_Material, k_NormalReconstructionLowKeyword, false);
                            CoreUtils.SetKeyword(m_Material, k_NormalReconstructionMediumKeyword, true);
                            CoreUtils.SetKeyword(m_Material, k_NormalReconstructionHighKeyword, false);
                            break;
                        case ScreenSpaceAmbientOcclusionSettings.NormalQuality.High:
                            CoreUtils.SetKeyword(m_Material, k_NormalReconstructionLowKeyword, false);
                            CoreUtils.SetKeyword(m_Material, k_NormalReconstructionMediumKeyword, false);
                            CoreUtils.SetKeyword(m_Material, k_NormalReconstructionHighKeyword, true);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                switch (source)
                {
                    case ScreenSpaceAmbientOcclusionSettings.DepthSource.DepthNormals:
                        CoreUtils.SetKeyword(m_Material, k_SourceDepthKeyword, false);
                        CoreUtils.SetKeyword(m_Material, k_SourceDepthNormalsKeyword, true);
                        break;
                    default:
                        CoreUtils.SetKeyword(m_Material, k_SourceDepthKeyword, true);
                        CoreUtils.SetKeyword(m_Material, k_SourceDepthNormalsKeyword, false);
                        break;
                }

                // Set up the descriptors
                RenderTextureDescriptor descriptor = cameraTargetDescriptor;
                descriptor.msaaSamples = 1;
                descriptor.depthBufferBits = 0;
                descriptor.width /= downsampleDivider;
                descriptor.height /= downsampleDivider;

                descriptor.colorFormat = RenderTextureFormat.ARGB32;
                RenderingUtils.ReAllocateIfNeeded(ref m_AOPassRT, descriptor, name: "AOPassRT");

                RenderingUtils.ReAllocateIfNeeded(ref m_BlurHorizonRT, descriptor, name: "BlurHorizonRT");
                RenderingUtils.ReAllocateIfNeeded(ref m_BlurVerticalRT, descriptor, name: "BlurVerticalRT");

                descriptor.colorFormat = m_SupportsR8RenderTextureFormat
                    ? RenderTextureFormat.R8
                    : RenderTextureFormat.ARGB32;
                RenderingUtils.ReAllocateIfNeeded(ref m_FinalRT, descriptor, name: "FinalRT");

                m_TemporalDenoiser.Setup(cmd,renderingData,m_FinalRT);

                // Get temporary render textures
                // m_Material.SetTexture(s_SSAOTexture1ID, m_AOPassRT);
                // m_Material.SetTexture(s_SSAOTexture2ID, m_BlurRT);
                // m_Material.SetTexture(s_SSAOTexture3ID, m_BlurRT);
                // m_Material.SetTexture(s_SSAOTextureFinalID, m_FinalRT);

                // Configure targets and clear color
                // ConfigureTarget(m_CurrentSettings.AfterOpaque ? m_Renderer.cameraColorTarget : s_SSAOTexture2ID);
                ConfigureClear(ClearFlag.None, Color.white);
            }

            /// <inheritdoc/>
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (m_Material == null)
                {
                    Debug.LogErrorFormat(
                        "{0}.Execute(): Missing material. ScreenSpaceAmbientOcclusion pass will not execute. Check for missing reference in the renderer resources.",
                        GetType().Name);
                    return;
                }

                CommandBuffer cmd = CommandBufferPool.Get();
                using (new ProfilingScope(cmd, m_ProfilingSampler))
                {
                    if (!m_CurrentSettings.AfterOpaque)
                    {
                        CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.ScreenSpaceOcclusion, true);
                    }


                    // Execute the SSAO
                    Blitter.BlitCameraTexture(cmd, renderingData.cameraData.renderer.cameraColorTargetHandle,
                        m_AOPassRT, m_Material, (int)ShaderPasses.AO);
                    // Execute the Blur Passes
                    Blitter.BlitCameraTexture(cmd, m_AOPassRT, m_BlurHorizonRT, m_Material,
                        (int)ShaderPasses.BlurHorizontal);
                    Blitter.BlitCameraTexture(cmd, m_BlurHorizonRT, m_BlurVerticalRT, m_Material,
                        (int)ShaderPasses.BlurVertical);
                    Blitter.BlitCameraTexture(cmd, m_BlurVerticalRT, m_FinalRT, m_Material,
                        (int)ShaderPasses.BlurFinal);
                    
                    // _denoiser.Setup(m_FinalRT);
                    // _denoiser.CaptureHistory(cmd,m_FinalRT);
                    //
                    // SetSourceSize(cmd, m_BlurHorizonRT.rt.descriptor);
                    // RenderAndSetBaseMap(cmd, m_SSAOTexture2Target, m_SSAOTexture3Target, ShaderPasses.BlurVertical);
                    // RenderAndSetBaseMap(cmd, m_SSAOTexture3Target, m_SSAOTextureFinalTarget,
                    //     ShaderPasses.BlurFinal);

                    // Set the global SSAO texture and AO Params
                    cmd.SetGlobalTexture(k_SSAOTextureName, m_FinalRT);
                    m_TemporalDenoiser.Execute(cmd,ref renderingData);

                    cmd.SetGlobalVector(k_SSAOAmbientOcclusionParamName,
                        new Vector4(1f, 0f, 0f, m_CurrentSettings.DirectLightingStrength));

                    // If true, SSAO pass is inserted after opaque pass and is expected to modulate lighting result now.
                    if (m_CurrentSettings.AfterOpaque)
                    {

                        CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.ScreenSpaceOcclusion, m_CurrentSettings.AfterOpaque);
                        // This implicitly also bind depth attachment. Explicitly binding m_Renderer.cameraDepthTarget does not work.
                        Blitter.BlitCameraTexture(cmd, m_FinalRT,
                            renderingData.cameraData.renderer.cameraColorTargetHandle, m_Material,
                            (int)ShaderPasses.AfterOpaque);
                    }
                }
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            /// <inheritdoc/>
            public override void OnCameraCleanup(CommandBuffer cmd)
            {
                if (cmd == null)
                {
                    throw new ArgumentNullException("cmd");
                }

                if (!m_CurrentSettings.AfterOpaque)
                {
                    CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.ScreenSpaceOcclusion, false);
                }

            }


        }
    }
}