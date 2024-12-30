using System;
using Features.LPM;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class LPMFeature : ScriptableRendererFeature
{
    class LumaPreservingMapperPass : ScriptableRenderPass
    {
        private const string lpmShaderName = "LPM";

        [SerializeField] private Material material;
        private String lastKeyword = "SDR";

        static class ShaderConstants
        {
            public static readonly int _SoftGap = Shader.PropertyToID("_SoftGap");
            public static readonly int _HdrMax = Shader.PropertyToID("_HdrMax");
            public static readonly int _Exposure = Shader.PropertyToID("_Exposure");
            public static readonly int _LPMExposure = Shader.PropertyToID("_LPMExposure");
            public static readonly int _Contrast = Shader.PropertyToID("_Contrast");
            public static readonly int _ShoulderContrast = Shader.PropertyToID("_ShoulderContrast");
            public static readonly int _Saturation = Shader.PropertyToID("_Saturation");
            public static readonly int _Crosstalk = Shader.PropertyToID("_Crosstalk");
            public static readonly int _Intensity = Shader.PropertyToID("_LPMIntensity");
            public static readonly int _DisplayMinMaxLuminance = Shader.PropertyToID("_DisplayMinMaxLuminance");
        }

        private Material lpmMaterial
        {
            get
            {
                if (material == null)
                {
                    material = new Material(Shader.Find(lpmShaderName));
                }

                return material;
            }
        }



        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var volume = VolumeManager.instance.stack.GetComponent<LPMVolume>();
            if (volume == null || !volume.IsActive())
            {
                return;
            }

            var camera = renderingData.cameraData.camera;

            if (camera.cameraType == CameraType.Preview)
            {
                return;
            }

            lpmMaterial.SetFloat(ShaderConstants._SoftGap, volume.SoftGap.value);
            lpmMaterial.SetFloat(ShaderConstants._HdrMax, volume.HdrMax.value);
            lpmMaterial.SetFloat(ShaderConstants._LPMExposure, volume.LPMExposure.value);
            lpmMaterial.SetFloat(ShaderConstants._Exposure, MathF.Pow(2.0f, volume.Exposure.value));
            lpmMaterial.SetFloat(ShaderConstants._Contrast, volume.Contrast.value);
            lpmMaterial.SetFloat(ShaderConstants._ShoulderContrast, volume.ShoulderContrast.value);
            lpmMaterial.SetFloat(ShaderConstants._Intensity, volume.Intensity.value);

            lpmMaterial.SetVector(ShaderConstants._Saturation, volume.Saturation.value);
            lpmMaterial.SetVector(ShaderConstants._Crosstalk, volume.Crosstalk.value);
            var cmd = CommandBufferPool.Get("Luma Preserving Mapping");
            // if (HDROutputSettings.main.active)
            // {
            //     lpmMaterial.SetVector(ShaderConstants._DisplayMinMaxLuminance,
            //         new Vector2(renderingData.cameraData.hdrDisplayInformation.minToneMapLuminance,
            //             renderingData.cameraData.hdrDisplayInformation.maxToneMapLuminance));
            // }

            if (volume.displayMode.value != DisplayMode.SDR)
            {
                lpmMaterial.SetVector(ShaderConstants._DisplayMinMaxLuminance,
                    new Vector2(renderingData.cameraData.hdrDisplayInformation.minToneMapLuminance,
                        renderingData.cameraData.hdrDisplayInformation.maxToneMapLuminance));

            }
            //now only support SDR
            lpmMaterial.DisableKeyword(lastKeyword);
            switch (volume.displayMode.value)
            {
                case DisplayMode.SDR:
                    lpmMaterial.EnableKeyword("SDR");
                    lastKeyword = "SDR";
                    break;
                case DisplayMode.DISPLAYMODE_HDR10_SCRGB:
                    lpmMaterial.EnableKeyword("DISPLAYMODE_HDR10_SCRGB");
                    lastKeyword = "DISPLAYMODE_HDR10_SCRGB";
                    break;
                case DisplayMode.DISPLAYMODE_HDR10_2084:
                    lpmMaterial.EnableKeyword("DISPLAYMODE_HDR10_2084");
                    lastKeyword = "DISPLAYMODE_HDR10_2084";
                    break;
            }



            Blit(cmd, ref renderingData, lpmMaterial);

            context.ExecuteCommandBuffer(cmd);
            cmd.Release();
        }
    }

    public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
    LumaPreservingMapperPass m_ScriptablePass;

    /// <inheritdoc/>
    public override void Create()
    {
        m_ScriptablePass = new LumaPreservingMapperPass();

        // Configures where the render pass should be injected.
        m_ScriptablePass.renderPassEvent = renderPassEvent;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        var volume = VolumeManager.instance.stack.GetComponent<LPMVolume>();
        if (volume != null && volume.IsActive())
        {
            renderer.EnqueuePass(m_ScriptablePass);
        }
    }
}