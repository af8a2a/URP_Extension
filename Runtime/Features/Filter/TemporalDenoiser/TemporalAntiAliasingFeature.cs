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
                renderPassEvent = RenderPassEvent.AfterRenderingTransparents
            };
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
                    dimension: TextureDimension.Tex2D, useMipMap: m_EnableMips, useDynamicScale: m_UseDynamicScale,
                    name: $"{id} {m_Name} {frameIndex}"
                );
            }
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