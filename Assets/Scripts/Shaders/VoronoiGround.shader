Shader "CUSTOM/VoronoiGround"
{
    Properties
    {
        _TilingX("Tiling X", Range(0,1000)) = 1
        _TilingY("Tiling Y", Range(0,1000)) = 1

        _ClampMin("Clamp Min", Range(0,1)) = .2
        _ClampMax("Clamp Max", Range(0,1)) = 1

        _EmissionIntensity("Emission Intensity", Range(0,10)) = 1

        _LUT("LUT", 2D) = "white" {}
        _LUTLerp("Contribution", Range(0, 1)) = 1
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

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE

            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON

            #pragma multi_compile _ LUT_CC

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_LUT);
            SAMPLER(sampler_LUT);
            CBUFFER_START(UnityPerMaterial)
                float _TilingX;
                float _TilingY;
                float _ClampMin;
                float _ClampMax;
                float _EmissionIntensity;

                float4 _LUT_ST;
                float4 _LUT_TexelSize;
                float _LUTLerp;
            CBUFFER_END

#define COLORS 32.0
#define MAXCOLOR 31.0
#define THRESHOLD 0.96875
#define THRESHBYCOL 0.0302734375

                float2 randfunc(float2 p) { return frac(sin(float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)))) * 43758.5453); }

                struct Attributes
                {
                    float4 positionOS : POSITION;
                    float3 normal : NORMAL;
                    float2 uv : TEXCOORD0;
                    float4 color : COLOR;
                    float2 uvLM : TEXCOORD1;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct Varyings
                {
                    float4 positionCS  : SV_POSITION;
                    float3 positionWS : TEXCOORD1;
                    float2 uv : TEXCOORD0;
                    float3 normal : TEXCOORD2;
                    float4 color : COLOR;
                    float2 uvLM : TEXCOORD3;
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
                    output.positionWS = vertexInput.positionWS;
                    output.uv = input.uv;
                    output.color = input.color;

                    output.uvLM = input.uvLM.xy * unity_LightmapST.xy + unity_LightmapST.zw;

                    output.normal = TransformObjectToWorldNormal(input.normal);

                    return output;
                }

                half4 frag(Varyings input) : SV_Target
                {
                    UNITY_SETUP_INSTANCE_ID(input);
                    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                    #ifdef _MAIN_LIGHT_SHADOWS
                    VertexPositionInputs vertexInput = (VertexPositionInputs)0;
                    vertexInput.positionWS = input.positionWS;

                    float4 shadowCoord = GetShadowCoord(vertexInput);
                    half shadowAttenutation = MainLightRealtimeShadow(shadowCoord);
                    float3 scolor = lerp(half3(1, 1, 1), half3(0, 0, 0), (1.0 - shadowAttenutation));
                    #else
                    float3 scolor = 1;
                    #endif
                    
                    // VORONOI
                    float3 color = 0;
                    float2 st = input.uv;
                    st *= float2(_TilingX,_TilingY);

                    float2 i_st = floor(st);
                    float2 f_st = frac(st);

                    float m_dist = 10.;  // minimum distance
                    float2 m_point;        // minimum point
                    float m_dist2 = 10.;  // minimum distance
                    float2 m_point2;        // minimum point

                    for (int j = -1; j <= 1; j++) {
                        for (int i = -1; i <= 1; i++) {
                            float2 neighbor = float2(i,j);
                            float2 pt = randfunc(i_st + neighbor);
                            pt = 0.5 + 0.1 * sin(6.2831 * pt);
                            //pt = .5;
                            float2 diff = neighbor + pt - f_st;
                            float dist = length(diff);

                            if (dist < m_dist) {
                                m_dist2 = m_dist;
                                m_point2 = m_point;
                                m_dist = dist;
                                m_point = pt;
                            }
                        }
                    }

                    // Assign a color using the closest point position
                    color += clamp(dot(m_point2 + m_point, float2(.3, .6)), _ClampMin, _ClampMax);

#ifdef LIGHTMAP_ON
                    half3 bakedGI = SampleLightmap(input.uvLM, normalize(input.normal));
#else
                    half3 bakedGI = SampleSH(normalize(input.normal));
#endif

                    float3 finalcolor = input.color.xyz * color * scolor;

#ifdef LUT_CC
                    float halfColX = 0.5 * _LUT_TexelSize.x;
                    float halfColY = 0.5 * _LUT_TexelSize.y;

                    float xOffset = halfColX + finalcolor.r * THRESHBYCOL;
                    float yOffset = halfColY + finalcolor.g * THRESHOLD;
                    float cell = floor(finalcolor.b * MAXCOLOR);

                    float2 lutPos = float2(cell / COLORS + xOffset, yOffset);
                    float3 gradedCol = SAMPLE_TEXTURE2D(_LUT, sampler_LUT, lutPos).xyz;

                    finalcolor = lerp(finalcolor, gradedCol, _LUTLerp);
#endif

                    return float4(finalcolor * bakedGI * _EmissionIntensity, 1);
                }

                ENDHLSL
            }

            UsePass "Universal Render Pipeline/Lit/Meta"
    }
}