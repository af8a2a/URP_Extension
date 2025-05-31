using System;
using Features.Utility;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace URP_Extension.Features.Utility
{
    public enum BlueNoiseTexFormat
    {
        _128R,
        _128RG
    }

    /// <summary>
    /// A bank of nvidia pre-generated spatiotemporal blue noise textures.
    /// ref: https://github.com/NVIDIAGameWorks/SpatiotemporalBlueNoiseSDK/tree/main
    /// </summary>
    [Serializable]
    public sealed class BlueNoiseSystem : IDisposable
    {
        private static Lazy<BlueNoiseSystem> m_Instance = new Lazy<BlueNoiseSystem>();

        public static BlueNoiseSystem Instance => m_Instance.Value;

        public static int blueNoiseArraySize = 64;



        [ResourceFormattedPaths("", 0, 64)] Texture2DArray m_TextureArray128R;
        Texture2DArray m_TextureArray128RG;

        RTHandle m_TextureHandle128R;
        RTHandle m_TextureHandle128RG;


        public Texture2DArray textureArray128R
        {
            get { return m_TextureArray128R; }
        }

        public Texture2DArray textureArray128RG
        {
            get { return m_TextureArray128RG; }
        }

        public RTHandle textureHandle128R
        {
            get { return m_TextureHandle128R; }
        }

        public RTHandle textureHandle128RG
        {
            get { return m_TextureHandle128RG; }
        }


        DitheredTextureSet m_DitheredTextureSet1SPP;
        DitheredTextureSet m_DitheredTextureSet8SPP;
        DitheredTextureSet m_DitheredTextureSet256SPP;


        public BlueNoiseSystem()
        {
            var textures = GraphicsSettings.GetRenderPipelineSettings<RuntimeTexture>();
            InitTextures(128, TextureFormat.R16, textures.blueNoise128RTex  , out m_TextureArray128R, out m_TextureHandle128R);
            InitTextures(128, TextureFormat.RG32, textures.blueNoise128RGTex, out m_TextureArray128RG, out m_TextureHandle128RG);

            m_DitheredTextureSet1SPP = new DitheredTextureSet
            {
                owenScrambled256Tex = textures.owenScrambled256Tex,
                scramblingTile = textures.scramblingTile1SPP,
                rankingTile = textures.rankingTile1SPP,
                scramblingTex = textures.scramblingTex
            };

            m_DitheredTextureSet8SPP = new DitheredTextureSet
            {
                owenScrambled256Tex = textures.owenScrambled256Tex,
                scramblingTile = textures.scramblingTile8SPP,
                rankingTile = textures.rankingTile8SPP,
                scramblingTex = textures.scramblingTex
            };

            m_DitheredTextureSet256SPP = new DitheredTextureSet
            {
                owenScrambled256Tex = textures.owenScrambled256Tex,
                scramblingTile = textures.scramblingTile256SPP,
                rankingTile = textures.rankingTile256SPP,
                scramblingTex = textures.scramblingTex
            };


            ExternalSystemManager.DisposeEvents += ClearAll;
        }

        public static readonly int s_STBNVec1Texture = Shader.PropertyToID("_STBNVec1Texture");
        public static readonly int s_STBNVec2Texture = Shader.PropertyToID("_STBNVec2Texture");
        public static readonly int s_STBNIndex = Shader.PropertyToID("_STBNIndex");
        public static readonly int _OwenScrambledRGTexture = Shader.PropertyToID("_OwenScrambledRGTexture");
        public static readonly int _OwenScrambledTexture = Shader.PropertyToID("_OwenScrambledTexture");
        public static readonly int _ScramblingTileXSPP = Shader.PropertyToID("_ScramblingTileXSPP");
        public static readonly int _RankingTileXSPP = Shader.PropertyToID("_RankingTileXSPP");
        public static readonly int _ScramblingTexture = Shader.PropertyToID("_ScramblingTexture");


        // Structure that holds all the dithered sampling texture that shall be binded at dispatch time.
        internal struct DitheredTextureSet
        {
            public Texture2D owenScrambled256Tex;
            public Texture2D scramblingTile;
            public Texture2D rankingTile;
            public Texture2D scramblingTex;
        }

        internal DitheredTextureSet DitheredTextureSet1SPP() => m_DitheredTextureSet1SPP;

        internal DitheredTextureSet DitheredTextureSet8SPP() => m_DitheredTextureSet8SPP;

        internal DitheredTextureSet DitheredTextureSet256SPP() => m_DitheredTextureSet256SPP;

        /// <summary>
        /// Cleanups up internal textures.
        /// </summary>
        public void Dispose()
        {
            CoreUtils.Destroy(m_TextureArray128R);
            CoreUtils.Destroy(m_TextureArray128RG);

            RTHandles.Release(m_TextureHandle128R);
            RTHandles.Release(m_TextureHandle128RG);

            m_TextureArray128R = null;
            m_TextureArray128RG = null;
        }

        static void InitTextures(int size, TextureFormat format, Texture2D[] sourceTextures,
            out Texture2DArray destinationArray, out RTHandle destinationHandle)
        {
            Assert.IsNotNull(sourceTextures);

            int len = sourceTextures.Length;

            Assert.IsTrue(len > 0);

            destinationArray = new Texture2DArray(size, size, len, format, false, true);
            destinationArray.hideFlags = HideFlags.HideAndDontSave;

            for (int i = 0; i < len; i++)
            {
                var noiseTex = sourceTextures[i];
                // Fail safe; should never happen unless the resources asset is broken
                if (noiseTex == null)
                {
                    continue;
                }

                Graphics.CopyTexture(noiseTex, 0, 0, destinationArray, i, 0);
            }

            destinationHandle = RTHandles.Alloc(destinationArray);
        }

        /// <summary>
        /// Bind spatiotemporal blue noise texture with given index (loop in blueNoiseArraySize).
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="textureIndex"></param>
        public static void BindSTBNParams(BlueNoiseTexFormat format, ComputeCommandBuffer cmd,
            ComputeShader computeShader, int kernel, TextureHandle texture, int frameCount)
        {
            var texID = (format == BlueNoiseTexFormat._128R) ? s_STBNVec1Texture : s_STBNVec2Texture;
            cmd.SetComputeTextureParam(computeShader, kernel, texID, texture);
            cmd.SetComputeIntParam(computeShader, s_STBNIndex, frameCount % blueNoiseArraySize);
        }
        
        
        internal static void BindDitheredTextureSet(CommandBuffer cmd, DitheredTextureSet ditheredTextureSet)
        {
            cmd.SetGlobalTexture(_OwenScrambledTexture,ditheredTextureSet.owenScrambled256Tex);
            cmd.SetGlobalTexture(_ScramblingTileXSPP,  ditheredTextureSet.scramblingTile);
            cmd.SetGlobalTexture(_RankingTileXSPP,     ditheredTextureSet.rankingTile);
            cmd.SetGlobalTexture(_ScramblingTexture,   ditheredTextureSet.scramblingTex);
        }

        

        public static void ClearAll()
        {
            Instance?.Dispose();
            m_Instance = null;
        }
    }
}