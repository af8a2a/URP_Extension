﻿using UnityEngine.Rendering.Universal;

namespace Features.Postprocessing.Diffusion
{
    public class DiffusionFeature : ScriptableRendererFeature
    {
        private DiffusionPass _diffusionPass;
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;

        public override void Create()
        {
            _diffusionPass = new DiffusionPass();
            _diffusionPass.renderPassEvent = renderPassEvent;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(_diffusionPass);
        }
    }

}