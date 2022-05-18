Shader "CUSTOM/NightSkyArtistic"
{
    Properties
    {
        _CloudColors("Cloud Color Map", 2D) = "white" {}
        _TilingClouds("Tiling Clouds", Range(0,1000)) = 10
        _CloudsDetail("Clouds Detail", Range(2,20)) = 10
        _CloudDensity("Clouds Density", Range(0,1)) = .85
        _CloudSize("Clouds Size", Range(0,1)) = .1
        _CloudSpeed("CloudSpeed", Range(0,10)) = .01
        _CloudIntensity("Cloud Intensity", Range(0,10)) = 1

        _TilingStars("Tiling Stars", Range(0,1000)) = 500
        _StarDensity("Star Density", Range(0,1)) = .3
        _StarSize("Star Size", Range(0.1,1)) = .2

    }
        SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_CloudColors);
            SAMPLER(sampler_CloudColors);
            CBUFFER_START(UnityPerMaterial)
                float4 _CloudColors_ST;
                float _TilingStars;
                float _TilingClouds;
                float _CloudDensity;
                float _CloudSize;
                float _CloudsDetail;
                float _CloudSpeed;
                float _CloudIntensity;

                float _StarDensity;
                float _StarSize;
            CBUFFER_END

                float mapvalue(float value, float minfrom, float maxfrom, float minto, float maxto) { return minto + ((maxto - minto) / (maxfrom - minfrom)) * (value - minfrom); }

                float2 randfunc(float2 p) { return frac(sin(float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)))) * 43758.5453); }
                half distance_minkowski(float2 a, float2 b, float p) { return pow(pow(abs(a.x - b.x), p) + pow(abs(a.y - b.y), p), 1 / p); }
                half distance_manhattan(float2 a, float2 b) { return abs(a.x - b.x) + abs(a.y - b.y); }

                float cloudnoise(float2 st) {
                    float2 i = floor(st);
                    float2 f = frac(st);

                    float2 u = f * f * (3.0 - 2.0 * f);

                    return lerp(lerp(dot(randfunc(i + float2(0.0, 0.0)), f - float2(0.0, 0.0)),
                        dot(randfunc(i + float2(1.0, 0.0)), f - float2(1.0, 0.0)), u.x),
                        lerp(dot(randfunc(i + float2(0.0, 1.0)), f - float2(0.0, 1.0)),
                            dot(randfunc(i + float2(1.0, 1.0)), f - float2(1.0, 1.0)), u.x), u.y);
                }

                float3 HUEtoRGB(float H)
                {
                    float R = abs(H * 6 - 3) - 1;
                    float G = 2 - abs(H * 6 - 2);
                    float B = 2 - abs(H * 6 - 4);
                    return saturate(float3(R, G, B));
                }
                float3 HSVtoRGB(float3 HSV)
                {
                    float3 RGB = HUEtoRGB(HSV.x);
                    return ((RGB - 1) * HSV.y + 1) * HSV.z;
                }

                float3 mod289(float3 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
                float2 mod289(float2 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
                float3 permute(float3 x) { return mod289(((x * 34.0) + 1.0) * x); }

                float simplexnoise(float2 v) {

                    // Precompute values for skewed triangular grid
                    const float4 C = float4(0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439);

                    // First corner (x0)
                    float2 i = floor(v + dot(v, C.yy));
                    float2 x0 = v - i + dot(i, C.xx);

                    // Other two corners (x1, x2)
                    float2 i1 = float(0.0);
                    i1 = (x0.x > x0.y) ? float2(1.0, 0.0) : float2(0.0, 1.0);
                    float2 x1 = x0.xy + C.xx - i1;
                    float2 x2 = x0.xy + C.zz;

                    // Do some permutations to avoid
                    // truncation effects in permutation
                    i = mod289(i);
                    float3 p = permute(
                        permute(i.y + float3(0.0, i1.y, 1.0))
                        + i.x + float3(0.0, i1.x, 1.0));

                    float3 m = max(0.5 - float3(
                        dot(x0, x0),
                        dot(x1, x1),
                        dot(x2, x2)
                        ), 0.0);

                    m = m * m;
                    m = m * m;

                    // Gradients:
                    //  41 pts uniformly over a line, mapped onto a diamond
                    //  The ring size 17*17 = 289 is close to a multiple
                    //      of 41 (41*7 = 287)

                    float3 x = 2.0 * frac(p * C.www) - 1.0;
                    float3 h = abs(x) - 0.5;
                    float3 ox = floor(x + 0.5);
                    float3 a0 = x - ox;

                    // Normalise gradients implicitly by scaling m
                    // Approximation of: m *= inversesqrt(a0*a0 + h*h);
                    m *= 1.79284291400159 - 0.85373472095314 * (a0 * a0 + h * h);

                    // Compute final noise value at P
                    float3 g = float3(0, 0, 0);
                    g.x = a0.x * x0.x + h.x * x0.y;
                    g.yz = a0.yz * float2(x1.x, x2.x) + h.yz * float2(x1.y, x2.y);
                    return 130.0 * dot(m, g);
                }

                float cloudrand(float2 _st) { return frac(sin(dot(_st.xy, float2(12.9898, 78.233))) * 43758.5453123); }
                float cloudnoise2(float2 _st) {
                    float2 i = floor(_st);
                    float2 f = frac(_st);

                    // Four corners in 2D of a tile
                    float a = cloudrand(i);
                    float b = cloudrand(i + float2(1.0, 0.0));
                    float c = cloudrand(i + float2(0.0, 1.0));
                    float d = cloudrand(i + float2(1.0, 1.0));

                    float2 u = f * f * (3.0 - 2.0 * f);

                    return lerp(a, b, u.x) +
                        (c - a) * u.y * (1.0 - u.x) +
                        (d - b) * u.x * u.y;
                }
                float fbm(float2 _st) {
                    float v = 0.0;
                    float a = 0.5;
                    float2 shift = 100;
                    // Rotate to reduce axial bias
                    float2x2 rot = float2x2(cos(0.5), sin(0.5),
                        -sin(0.5), cos(0.50));
                    for (int i = 0; i < _CloudsDetail; ++i) {
                        v += a * cloudnoise2(_st);
                        _st = mul(rot ,_st) * 2.0 + shift;
                        a *= 0.5;
                    }
                    return v;
                }

                struct Attributes
                {
                    float4 positionOS : POSITION;
                    float2 uv : TEXCOORD0;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct Varyings
                {
                    float4 positionCS  : SV_POSITION;
                    float2 uv : TEXCOORD0;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                    UNITY_VERTEX_OUTPUT_STEREO
                };

                Varyings vert(Attributes input)
                {
                    Varyings output = (Varyings)0;

                    UNITY_SETUP_INSTANCE_ID(input);
                    UNITY_TRANSFER_INSTANCE_ID(input, output);
                    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                    output.positionCS = vertexInput.positionCS;
                    output.uv = input.uv;

                    return output;
                }

                half4 frag(Varyings input) : SV_Target
                {
                    UNITY_SETUP_INSTANCE_ID(input);
                    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                    // VORONOI
                    float3 color = 0;
                    float2 st = input.uv * _TilingStars;

                    float2 i_st = floor(st);
                    float2 f_st = frac(st);

                    float m_dist = 10.;  // minimum distance
                    float2 m_point;        // minimum point

                    for (int j = -1; j <= 1; j++) {
                        for (int i = -1; i <= 1; i++) {
                            float2 neighbor = float2(i,j);
                            float2 pt = randfunc(i_st + neighbor);
                            pt = 0.5 + 0.5 * sin(6.2831 * pt);
                            //pt = .5;
                            float2 diff = neighbor + pt - f_st;
                            //float dist = distance_minkowski(pt, neighbor-f_st, _StarSize);
                            float dist = distance_manhattan(pt, neighbor-f_st);

                            if (dist < m_dist) {
                                m_dist = dist;
                                m_point = pt;
                            }
                        }
                    }

                    // Assign a color using the closest point position
                    //color += dot(m_point, float2(.3, .6));
                    color += 1. - step(_StarDensity, m_dist);


                    // CLOUDS
                    float2 st2 = input.uv * _TilingClouds + _Time.y * _CloudSpeed;
                    float cloudclr;// = (simplexnoise(st2) * .5 + .5);
                    
                    float2 q = 0;
                    q.x = fbm(st2 + 0.00);
                    q.y = fbm(st2 + float2(1.0,1.0));
                    float2 r = 0;
                    r.x = fbm(st2 + 1.0 * q + float2(1.7, 9.2) + 0.15);
                    r.y = fbm(st2 + 1.0 * q + float2(8.3, 2.8) + 0.126);

                    float f = fbm(st2 + r);
                    cloudclr = (f * f * f + _CloudDensity * 2 * f * f + _CloudDensity * f);

                    cloudclr = (cloudclr < _CloudSize) ? 0 : mapvalue(cloudclr, _CloudSize, 1, 0, .5);

                    // GRADIENT
                    half3 colorcloud = SAMPLE_TEXTURE2D(_CloudColors, sampler_CloudColors, float2(cloudclr, 0)).xyz * _CloudIntensity;


                    return float4((1-color), 1);
                }

                ENDHLSL
            }
    }
}