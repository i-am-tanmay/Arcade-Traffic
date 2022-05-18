Shader "CUSTOM/SkidMark"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _MainTex("Albedo (RGB)", 2D) = "white" {}
    }
        SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        Offset -4, -4
        ZWrite Off
        Alphatest Greater 0
        ColorMaterial AmbientAndDiffuse
        Lighting Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            ColorMask RGBA
                SetTexture[_MainTex]{
                    Combine texture, texture * primary
                }
                SetTexture[_MainTex]{
                    Combine primary * previous
                }
        }
    }
        Fallback "Transparent/VertexLit", 2
}