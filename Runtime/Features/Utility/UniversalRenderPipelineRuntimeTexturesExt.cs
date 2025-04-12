using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace URP_Extension.Features.Utility
{
    /// <summary>
    /// Class containing texture resources used in URP.
    /// </summary>
    [Serializable]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    public class UniversalRenderPipelineRuntimeTexturesExt : IRenderPipelineResources
    {
        public int version { get; }
    }
}