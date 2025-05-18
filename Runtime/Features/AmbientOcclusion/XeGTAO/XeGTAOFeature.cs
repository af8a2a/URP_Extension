﻿using UnityEngine.Rendering.Universal;

namespace Features.AmbientOcclusion.XeGTAO
{
    
    public class XeGTAOFeature : ScriptableRendererFeature
    {
        XeGTAOPass pass;

        
        public override void Create()
        {
            pass = new XeGTAOPass()
            {
                renderPassEvent = RenderPassEvent.AfterRenderingPrePasses
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            pass.Setup();
            renderer.EnqueuePass(pass);
        }
    }
}