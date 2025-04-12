using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace URP_Extension.Features.HierarchyZGenerator
{
    public class HierarchyZData : ContextItem
    {
        public TextureHandle HizTexture = TextureHandle.nullHandle;
        public int MipCount = 0;
        public override void Reset()
        {
            HizTexture = TextureHandle.nullHandle;
        }
    }
}