using System;
using Features.Filter.TemporalDenoiser;
using Features.InterleavedTexture;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
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

        private GroundTruthAmbientOcclusionPass m_SSAOPass = null;

        // Constants
        internal const string k_ShaderName = "Hidden/Universal Render Pipeline/GroundTruthAmbientOcclusion";
        internal const string k_OrthographicCameraKeyword = "_ORTHOGRAPHIC";
        internal const string k_NormalReconstructionLowKeyword = "_RECONSTRUCT_NORMAL_LOW";
        internal const string k_NormalReconstructionMediumKeyword = "_RECONSTRUCT_NORMAL_MEDIUM";
        internal const string k_NormalReconstructionHighKeyword = "_RECONSTRUCT_NORMAL_HIGH";
        internal const string k_SourceDepthKeyword = "_SOURCE_DEPTH";
        internal const string k_SourceDepthNormalsKeyword = "_SOURCE_DEPTH_NORMALS";

        internal bool afterOpaque => m_Settings.AfterOpaque;

        /// <inheritdoc/>
        public override void Create()
        {
            // Create the pass...
            m_SSAOPass ??= new GroundTruthAmbientOcclusionPass();

            GetMaterial();
        }

        /// <inheritdoc/>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (UniversalRenderer.IsOffscreenDepthTexture(ref renderingData.cameraData))
                return;

            if (!GetMaterial())
            {
                Debug.LogErrorFormat(
                    "{0}.AddRenderPasses(): Missing material. {1} render pass will not be added. Check for missing reference in the renderer resources.",
                    GetType().Name, name);
                return;
            }

            bool shouldAdd = m_SSAOPass.Setup(m_Settings, m_Material);

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
    }
}