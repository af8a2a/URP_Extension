﻿using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Features.Sky
{
    [DisallowMultipleRendererFeature("Sky Render Feature")]
    public sealed class SkyFeature : ScriptableRendererFeature
    {
        private SkyPass _pass;

        public override void Create()
        {
            _pass = new SkyPass();
        }


        // void OnEnable()
        // {
        //     var shaders = GraphicsSettings.GetRenderPipelineSettings<SkyRuntimeResources>();
        //     
        //     SkySystem.instance.Build(UniversalRenderPipeline.asset, shaders);
        //     
        // }
        //
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(_pass);
        }
    }
}