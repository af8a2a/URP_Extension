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
            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();
            var camera = cameraData.camera;
            var colorTexture = resourceData.activeColorTexture;

            var camHistoryRTSystem = HistoryFrameRTSystem.GetOrCreate(cameraData.camera);
            if (camHistoryRTSystem == null)
            {
                return;
            }

            int actualWidth = cameraData.cameraTargetDescriptor.width;
            int actualHeight = cameraData.cameraTargetDescriptor.height;

            if (camHistoryRTSystem.GetCurrentFrameRT(HistoryFrameType.TAAColorBuffer) == null)
            {
                var textureAllocator = new TAATextureAllocator(nameof(TemporalDenoiser),
                    new Vector2Int(actualWidth, actualHeight), cameraData.cameraTargetDescriptor.graphicsFormat);

                camHistoryRTSystem.ReleaseHistoryFrameRT(HistoryFrameType.TAAColorBuffer);
                camHistoryRTSystem.AllocHistoryFrameRT((int)HistoryFrameType.TAAColorBuffer, cameraData.camera.name,
                    textureAllocator.Allocator, 2);
            }

            var prevRT =
                renderGraph.ImportTexture(camHistoryRTSystem.GetPreviousFrameRT(HistoryFrameType.TAAColorBuffer));
            var currRT =
                renderGraph.ImportTexture(camHistoryRTSystem.GetCurrentFrameRT(HistoryFrameType.TAAColorBuffer));
            var motionRT = resourceData.motionVectorColor;
            var depthRT = resourceData.cameraDepth;
            resourceData.cameraColor =
                denoiser.DoColorTemporalDenoiseCS(renderGraph, camera, motionRT, depthRT, colorTexture, prevRT, currRT);
        }
    }
}