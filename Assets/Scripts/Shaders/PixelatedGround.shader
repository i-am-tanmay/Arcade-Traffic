Shader "CUSTOM/PixelatedGround"
{
    Properties
    {
        _TilingX("Tiling X", Range(0,1000)) = 1
        _TilingY("Tiling Y", Range(0,1000)) = 1

        _ClampMin("Clamp Min", Range(0,1)) = .2
        _LerpMin("Lerp Min", Range(0,1)) = .17
        _LerpMax("Lerp Max", Range(0,1)) = .75
        _ClampMax("Clamp Max", Range(0,1)) = 1

        _EmissionIntensity("Emission Intensity", Range(0,10)) = 1
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

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _TilingX;
                float _TilingY;
                float _ClampMin;
                float _ClampMax;
                float _LerpMin;
                float _LerpMax;
                float _EmissionIntensity;
            CBUFFER_END

            // Some useful functions
            float3 mod289(float3 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
            float2 mod289(float2 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
            float3 permute(float3 x) { return mod289(((x * 34.0) + 1.0) * x); }

            float noisefunc(float2 v) {

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

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
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
                float4 color : COLOR;
                float2 uvLM : TEXCOORD2;
                half3 normalWS : TEXCOORD3;
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
                
                VertexNormalInputs vertexNormalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.normalWS = vertexNormalInput.normalWS;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 scolor = 1;

                #ifdef _MAIN_LIGHT_SHADOWS
                VertexPositionInputs vertexInput = (VertexPositionInputs)0;
                vertexInput.positionWS = input.positionWS;

                float4 shadowCoord = GetShadowCoord(vertexInput);
                half shadowAttenutation = MainLightRealtimeShadow(shadowCoord);
                scolor = lerp(half3(1, 1, 1), half3(0, 0, 0), (1.0 - shadowAttenutation));
                #endif

                // Scale the space to see the grid
                input.uv = floor(input.uv * float2(_TilingX, _TilingY));

                float sp = (noisefunc(input.uv) * .5 + .5);
                sp = lerp(_LerpMin, _LerpMax, clamp(sp, _ClampMin, _ClampMax));
                scolor *= input.color.xyz * sp * _EmissionIntensity;

                #ifdef LIGHTMAP_ON
                    half3 bakedGI = SampleLightmap(input.uvLM, normalize(input.normalWS));
                #else
                    half3 bakedGI = SampleSH(normalize(input.normalWS));
                #endif

                return float4(scolor * bakedGI, 1);
            }

            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/Meta"
    }
}