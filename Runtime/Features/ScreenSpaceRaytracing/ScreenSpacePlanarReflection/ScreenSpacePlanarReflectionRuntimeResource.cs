using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Features.ScreenSpaceRaytracing.ScreenSpacePlanarReflection
{
    [Serializable]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    public class ScreenSpacePlanarReflectionRuntimeResource : IRenderPipelineResources
    {
        [SerializeField] [HideInInspector] private int _version;
        public int version => _version;


        [SerializeField] [ResourcePath("Runtime/Features/ScreenSpaceRaytracing/ScreenSpacePlanarReflection/Shader/SSPRComputeShader.compute")]
        private ComputeShader m_SSPRShader;

        public ComputeShader SSPRShader
        {
            get => m_SSPRShader;
            set => this.SetValueAndNotify(ref m_SSPRShader, value, nameof(m_SSPRShader));
        }
    }
}