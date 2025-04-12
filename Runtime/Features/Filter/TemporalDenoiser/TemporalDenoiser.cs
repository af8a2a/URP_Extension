using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Features.Filter.TemporalDenoiser
{
    public class TemporalDenoiser : IDisposable
    {
        bool m_HistoryReady = false;
        private RTHandle input;
        private float feedback = 0.7f;
        RTHandle[] historyBuffer;

        Material TemporalDenoiserMaterial;
        static int indexWrite = 0;

        Matrix4x4 previewView;
        Matrix4x4 previewProj;
        Dictionary<Camera, TAAData> m_TAADatas=new ();

        internal static class ShaderKeywordStrings
        {
            internal static readonly string HighTAAQuality = "_HIGH_TAA";
            internal static readonly string MiddleTAAQuality = "_MIDDLE_TAA";
            internal static readonly string LOWTAAQuality = "_LOW_TAA";
        }

        internal static class ShaderConstants
        {
            public static readonly int _TAA_Params = Shader.PropertyToID("_TAA_Params");
            public static readonly int _TAA_pre_texture = Shader.PropertyToID("_TAA_Pretexture");
            public static readonly int _TAA_pre_vp = Shader.PropertyToID("_TAA_Pretexture");
            public static readonly int _TAA_PrevViewProjM = Shader.PropertyToID("_PrevViewProjM_TAA");
            public static readonly int _TAA_CurInvView = Shader.PropertyToID("_I_V_Current_jittered");
            public static readonly int _TAA_CurInvProj = Shader.PropertyToID("_I_P_Current_jittered");
        }

        public TemporalDenoiser()
        {
            TemporalDenoiserMaterial = new Material(Shader.Find("PostProcessing/TemporalFilter"));
        }

        public void Setup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var camera = renderingData.cameraData.camera;
            TAAData TaaData;
            if (!m_TAADatas.TryGetValue(camera, out TaaData))
            {
                TaaData = new TAAData();
                m_TAADatas.Add(camera, TaaData);
            }
        
            var stack = VolumeManager.instance.stack;
            var denoiserSetting = stack.GetComponent<TemporalDenoiserSetting>();
            if (denoiserSetting.IsActive())
            {
                UpdateTAAData(renderingData, TaaData, denoiserSetting);
                cmd.SetViewProjectionMatrices(renderingData.cameraData.camera.worldToCameraMatrix,
                    TaaData.projOverride);
            }
            else
            {
                m_TAADatas.Remove(camera);
            }
        }

        void UpdateTAAData(RenderingData renderingData, TAAData TaaData, TemporalDenoiserSetting Taa)
        {
            Camera camera = renderingData.cameraData.camera;
            Vector2 additionalSample = Utils.GenerateRandomOffset() * Taa.spread.value;
            TaaData.sampleOffset = additionalSample;
            TaaData.porjPreview = previewProj;
            TaaData.viewPreview = previewView;
            TaaData.projOverride = camera.orthographic
                ? Utils.GetJitteredOrthographicProjectionMatrix(camera, TaaData.sampleOffset)
                : Utils.GetJitteredPerspectiveProjectionMatrix(camera, TaaData.sampleOffset);
            TaaData.sampleOffset = new Vector2(TaaData.sampleOffset.x / camera.scaledPixelWidth,
                TaaData.sampleOffset.y / camera.scaledPixelHeight);
            previewView = camera.worldToCameraMatrix;
            previewProj = camera.projectionMatrix;
        }


        public void DoTemporalAntiAliasing(CameraData cameraData, CommandBuffer cmd, RTHandle rtHandle)
        {
            var camera = cameraData.camera;
            var stack = VolumeManager.instance.stack;
            var denoiserSetting = stack.GetComponent<TemporalDenoiserSetting>();

            // Never draw in Preview
            if (camera.cameraType == CameraType.Preview)
                return;
            var descriptor = new RenderTextureDescriptor(camera.scaledPixelWidth, camera.scaledPixelHeight,
                RenderTextureFormat.DefaultHDR, 16);
            EnsureArray(ref historyBuffer, 2);
            PrepareRenderTarget(ref historyBuffer[0], descriptor.width, descriptor.height, descriptor.colorFormat,
                FilterMode.Bilinear);
            PrepareRenderTarget(ref historyBuffer[1], descriptor.width, descriptor.height, descriptor.colorFormat,
                FilterMode.Bilinear);

            int indexRead = indexWrite;
            indexWrite = (++indexWrite) % 2;
            TAAData TaaData;
            if (!m_TAADatas.TryGetValue(camera, out TaaData))
            {
                return;
            }

            Matrix4x4 inv_p_jitterd = Matrix4x4.Inverse(TaaData.projOverride);
            Matrix4x4 inv_v_jitterd = Matrix4x4.Inverse(camera.worldToCameraMatrix);
            Matrix4x4 previous_vp = TaaData.porjPreview * TaaData.viewPreview;
            TemporalDenoiserMaterial.SetMatrix(ShaderConstants._TAA_CurInvView, inv_v_jitterd);
            TemporalDenoiserMaterial.SetMatrix(ShaderConstants._TAA_CurInvProj, inv_p_jitterd);
            TemporalDenoiserMaterial.SetMatrix(ShaderConstants._TAA_PrevViewProjM, previous_vp);
            TemporalDenoiserMaterial.SetVector(ShaderConstants._TAA_Params,
                new Vector3(TaaData.sampleOffset.x, TaaData.sampleOffset.y, denoiserSetting.feedback.value));
            TemporalDenoiserMaterial.SetTexture(ShaderConstants._TAA_pre_texture, historyBuffer[indexRead]);
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.HighTAAQuality,
                denoiserSetting.quality.value == MotionBlurQuality.High);
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MiddleTAAQuality,
                denoiserSetting.quality.value == MotionBlurQuality.Medium);
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.LOWTAAQuality, denoiserSetting.quality.value == MotionBlurQuality.Low);
            cmd.Blit(rtHandle, historyBuffer[indexWrite], TemporalDenoiserMaterial);
            cmd.Blit(historyBuffer[indexWrite], rtHandle);
        }

        void EnsureArray<T>(ref T[] array, int size, T initialValue = default(T))
        {
            if (array == null || array.Length != size)
            {
                array = new T[size];
                for (int i = 0; i != size; i++)
                    array[i] = initialValue;
            }
        }

        bool PrepareRenderTarget(ref RTHandle rt, int width, int height, RenderTextureFormat format,
            FilterMode filterMode, int depthBits = 0, int antiAliasing = 1)
        {
            var desc = new RenderTextureDescriptor(width, height, format, depthBits);
            RenderingUtils.ReAllocateIfNeeded(ref rt, desc, filterMode, TextureWrapMode.Clamp);
            return true; // new target
        }


        public void Dispose()
        {
            historyBuffer?.ToList().ForEach(buffer => buffer?.Release());
        }
    }
}