﻿using Features.ColorPyramid;
using Features.Core.Manager;
using Features.HierarchyZGenerator;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Features.Core
{
    [DisallowMultipleRendererFeature]
    public class CoreFeature : ScriptableRendererFeature
    {
        private readonly string[] m_GBufferPassNames = new string[] { "UniversalGBuffer" };

        HierarchyZPass pass;
        ColorPyramidPass colorPyramid;
        ForwardGBufferPass forwardGBufferPass;
        HistoryCapturePass historyCapturePass;
        public override void Create()
        {
            pass = new HierarchyZPass
            {
                renderPassEvent = RenderPassEvent.AfterRenderingTransparents
            };
            colorPyramid = new ColorPyramidPass()
            {
                renderPassEvent = RenderPassEvent.AfterRenderingTransparents
            };
            forwardGBufferPass = new ForwardGBufferPass(m_GBufferPassNames);

            historyCapturePass = new HistoryCapturePass()
            {
                renderPassEvent = RenderPassEvent.AfterRenderingTransparents
            };
        }
        

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var deferred = renderingData.universalRenderingData.renderingMode is RenderingMode.Deferred;

            if (HistoryBufferCaptureManager.instance.EnableHistoryPasses())
            {
                renderer.EnqueuePass(historyCapturePass);

            }
            
            if (ForwardGBufferManager.instance.EnableGBufferPasses() && !deferred)
            {
                renderer.EnqueuePass(forwardGBufferPass);
            }

            // renderer.EnqueuePass(pass);
            // renderer.EnqueuePass(colorPyramid);
        }
    }
}