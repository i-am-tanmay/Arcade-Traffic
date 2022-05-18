Shader "CUSTOM/ToonColor"
{
    Properties
    {
        _BaseColor("Color", Color) = (1, 1, 1, 1)

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
            Cull Off

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
                half4 _BaseColor;
                float _EmissionIntensity;

                float4 _LUT_ST;
                float4 _LUT_TexelSize;
                float _LUTLerp;
            CBUFFER_END

#define COLORS 32.0
#define MAXCOLOR 31.0
#define THRESHOLD 0.96875
#define THRESHBYCOL 0.0302734375

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half3 normal : NORMAL;
                float2 uvLM : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD1;
                float2 uv : TEXCOORD0;
                half3 normal : TEXCOORD2;
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
                output.normal = TransformObjectToWorldNormal(input.normal);

                output.uvLM = input.uvLM.xy * unity_LightmapST.xy + unity_LightmapST.zw;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                
                half3 col = _BaseColor.xyz;

                #ifdef _MAIN_LIGHT_SHADOWS
                VertexPositionInputs vertexInput = (VertexPositionInputs)0;
                vertexInput.positionWS = input.positionWS;

                float4 shadowCoord = GetShadowCoord(vertexInput);
                half shadowAttenutation = MainLightRealtimeShadow(shadowCoord);
                float3 scolor = lerp(half3(1, 1, 1), half3(0, 0, 0), (1.0 - shadowAttenutation));
                #else
                float3 scolor = 1;
                #endif

                

#ifdef LUT_CC
                float halfColX = 0.5 * _LUT_TexelSize.x;
                float halfColY = 0.5 * _LUT_TexelSize.y;

                float xOffset = halfColX + col.r * THRESHBYCOL;
                float yOffset = halfColY + col.g * THRESHOLD;
                float cell = floor(col.b * MAXCOLOR);

                float2 lutPos = float2(cell / COLORS + xOffset, yOffset);
                float3 gradedCol = SAMPLE_TEXTURE2D(_LUT, sampler_LUT, lutPos).xyz;

                col = lerp(col, gradedCol, _LUTLerp);
#endif

#ifdef LIGHTMAP_ON
                half3 bakedGI = SampleLightmap(input.uvLM, normalize(input.normal));
#else
                half3 bakedGI = SampleSH(normalize(input.normal));
#endif

                return float4(col * scolor * _EmissionIntensity * bakedGI, 1);
            }

            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
}