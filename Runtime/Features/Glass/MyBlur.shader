Shader "FullScreenBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }
        LOD 100

        UsePass "Diffusion/BLUR HORIZONTAL"
        UsePass "Diffusion/BLUR VERTICAL"

    }
}