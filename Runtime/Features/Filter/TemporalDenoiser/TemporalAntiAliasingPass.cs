using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using URP_Extension.Features.Utility;

namespace Features.Filter.TemporalDenoiser
{
    public class TemporalAntiAliasingPass : ScriptableRenderPass
    {
        TemporalDenoiser denoiser;

        public TemporalAntiAliasingPass()
        {
            denoiser = new TemporalDenoiser();
        }

        private struct TAATextureAllocator
        {
            private String m_Name;
            private Vector2Int m_Size;
            private bool m_EnableMips;
            private bool m_UseDynamicScale;
            GraphicsFormat m_Format;

            public TAATextureAllocator(String newName, Vector2Int newSize,
                GraphicsFormat format = GraphicsFormat.R16_SFloat, bool enableMips = false,
                bool useDynamicScale = false)
            {
                m_Name = newName;
                m_Size = newSize;
                m_EnableMips = enableMips;
                m_UseDynamicScale = useDynamicScale;
                m_Format = format;
            }

            public RTHandle Allocator(string id, int frameIndex, RTHandleSystem rtHandleSystem)
            {
                return rtHandleSystem.Alloc(
                    m_Size.x, m_Size.y, 1, DepthBits.None, m_Format,
                    dimension: TextureDimension.Tex2D, enableRandomWrite: true, useMipMap: m_EnableMips,
                    useDynamicScale: m_UseDynamicScale, name: $"{id} {m_Name} {frameIndex}"
                );
            }
        }


        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var setting = VolumeManager.instance.stack.GetComponent<TemporalDenoiserSetting>();
            if (setting == null || !setting.IsActive())
            {
                return;
            }


            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();
            var camera = cameraData.camera;
            var colorTexture = resourceData.activeColorTexture;
            if (cameraData.historyManager == null)
            {
                return;
            }

            cameraData.historyManager.RequestAccess<TAAColorHistory>();
            var history = cameraData.historyManager.GetHistoryForWrite<TAAColorHistory>();
            if (history == null)
            {
                return;
            }

            var historyDesc = cameraData.cameraTargetDescriptor;
            historyDesc.graphicsFormat = GraphicsFormat.R16G16B16A16_SFloat;
            historyDesc.depthBufferBits = 0;
            historyDesc.msaaSamples = 1;
            historyDesc.enableRandomWrite = true;
            // Call the Update method of the camera history type.
            history.Update(ref historyDesc);


            // var prevDepth= cameraData.historyManager.GetHistoryForRead<TAAColorHistory>()?.GetCurrentTexture();

            var prevRT = renderGraph.ImportTexture(history.GetPreviousTexture());
            var currRT = renderGraph.ImportTexture(history.GetCurrentTexture());
            var motionRT = resourceData.motionVectorColor;
            var depthRT = resourceData.cameraDepth;
            // resourceData.cameraColor = denoiser.DoColorTemporalDenoiseCS(renderGraph, camera, motionRT, depthRT,
            //     colorTexture, prevRT, currRT, setting);
            resourceData.cameraColor = denoiser.DoColorTemporalDenoiseCS(renderGraph, camera, motionRT, depthRT,
                colorTexture, prevRT, currRT, setting);

        }
    }
}