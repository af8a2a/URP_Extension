﻿using UnityEngine.Rendering.Universal;

namespace Features.Postprocessing.ToneMapping
{
    [DisallowMultipleRendererFeature]
    public class GranTurismoToneMappingFeature : ScriptableRendererFeature
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

        private GranTurismoToneMappingPass _pass;

        public override void Create()
        {
            _pass = new GranTurismoToneMappingPass()
            {
                renderPassEvent = renderPassEvent
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(_pass);
        }
    }
}