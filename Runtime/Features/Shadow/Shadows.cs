using UnityEngine;
using UnityEngine.Rendering;

namespace Features.Shadow
{
    public sealed partial class Shadows : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("Penumbra controls shadows soften width. (For Per Object Shadow)")]
        public ClampedFloatParameter perObjectShadowPenumbra = new ClampedFloatParameter(1.0f, 0.001f, 3.0f);


        /// <inheritdoc/>
        public bool IsActive() => true; // Always enable screenSpaceShadows.
    }
}