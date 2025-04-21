using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Features.Filter.TemporalDenoiser
{
    public class TemporalAntiAliasingFeature : ScriptableRendererFeature
    {
        TemporalAntiAliasingPass temporalAntiAliasingPass;

        public override void Create()
        {
            temporalAntiAliasingPass = new TemporalAntiAliasingPass
            {
                renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing
            };
        }

        
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var taa = VolumeManager.instance.stack.GetComponent<TemporalDenoiserSetting>();
            if (taa == null || !taa.IsActive())
            {
                return;
            }

            renderer.EnqueuePass(temporalAntiAliasingPass);
        }
    }
}