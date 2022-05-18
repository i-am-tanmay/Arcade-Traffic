Shader "CUSTOM/WorldTex"
{
    Properties
    {
        _MainTex("Main Texture", 2D) = "white" {}
        _TexTiling("Texture Tiling", Range(0,10)) = 1.0
        _TexOffsetX("Texture Offset X", Range(0,10)) = 0.0
        _TexOffsetY("Texture Offset Y", Range(0,10)) = 0.0

        _LUT("LUT", 2D) = "white" {}
        _LUTLerp("Contribution", Range(0, 1)) = 1

    }
        SubShader
        {
            Tags
            {
                "RenderType" = "Opaque"
                "Queue" = "Geometry"
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


                TEXTURE2D(_MainTex);
                SAMPLER(sampler_MainTex);
                TEXTURE2D(_LUT);
                SAMPLER(sampler_LUT);
                CBUFFER_START(UnityPerMaterial)
                    float4 _MainTex_ST;
                    float _TexTiling;
                    float _TexOffsetX;
                    float _TexOffsetY;

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
                        float3 normal : NORMAL;
                        float2 uvLM : TEXCOORD0;
                        UNITY_VERTEX_INPUT_INSTANCE_ID
                    };

                    struct Varyings
                    {
                        float4 positionCS  : SV_POSITION;
                        float3 positionWS  : TEXCOORD0;
                        float3 normal : TEXCOORD2;
                        float2 uvLM : TEXCOORD1;
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

                        output.normal = TransformObjectToWorldNormal(input.normal);

                        output.uvLM = input.uvLM.xy * unity_LightmapST.xy + unity_LightmapST.zw;

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

                        half3 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.positionWS.xz * _TexTiling + float2(_TexOffsetX, _TexOffsetY)).rgb;
                        col.rgb *= scolor;

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

                        return float4(col * bakedGI, 1);
                    }

                    ENDHLSL
                }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"

        }
}