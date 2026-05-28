Shader "Iris/Drape"
{
    Properties
    {
        [Header(Texture)]
        _MainTex ("Albedo", 2D) = "white" {}
        _Color   ("Tint",   Color) = (1, 1, 1, 1)

        [Header(PSX Effects)]
        _VertexSnapResolution ("Vertex Snap Resolution", Vector) = (160, 120, 0, 0)
        _AffineIntensity      ("Affine Intensity", Range(0, 1)) = 1.0

        [Header(Wind)]
        _WindStrength ("Wind Strength", Range(0, 1)) = 0
        _WindSpeed    ("Wind Speed", Range(0.5, 5)) = 1.5
        _WindAmplitude ("Wind Amplitude", Range(0, 0.5)) = 0.15
        _WindFrequency ("Wind Frequency", Range(0.5, 5)) = 2.0
        _GustStrength  ("Gust Strength", Range(0, 0.3)) = 0.08
        _GustSpeed     ("Gust Speed", Range(0.1, 2)) = 0.4

        [Header(Wind Direction)]
        _WindDirX ("Wind Direction X", Range(-1, 1)) = 1
        _WindDirZ ("Wind Direction Z", Range(-1, 1)) = 0

        [Header(Drape)]
        _HangAxis ("Hang Axis (0=Y 1=custom)", Range(0, 1)) = 0
        _PinTop   ("Pin Top Amount", Range(0, 1)) = 1

        [Header(Depth)]
        _DepthBias ("Depth Bias", Float) = 0.0

        [HideInInspector] _ZTest ("ZTest", Float) = 4
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off // drapes are thin, visible from both sides

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            ZTest [_ZTest]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
                float  fogFactor   : TEXCOORD2;
                float2 uvCorrect   : TEXCOORD3;
                float3 uvAffine    : TEXCOORD4;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4  _Color;
                float4 _VertexSnapResolution;
                half   _AffineIntensity;
                float  _DepthBias;
                half   _WindStrength;
                half   _WindSpeed;
                half   _WindAmplitude;
                half   _WindFrequency;
                half   _GustStrength;
                half   _GustSpeed;
                half   _WindDirX;
                half   _WindDirZ;
                half   _PinTop;
            CBUFFER_END

            // Simple hash for per-vertex variation
            float Hash(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;

                float3 posOS = input.positionOS.xyz;

                // Weight: how much this vertex moves. Top of drape (high Y in object space) is pinned.
                // UV.y = 0 at top, 1 at bottom for standard drape mesh layout.
                // Also support object-space Y: normalize Y range so top = 0, bottom = 1.
                float weight = input.uv.y; // assumes UV maps top-to-bottom

                // Square the weight so motion accelerates toward the bottom
                weight = weight * weight;

                // Apply wind displacement in object space
                if (_WindStrength > 0.001)
                {
                    float time = _Time.y * _WindSpeed;

                    // Primary wave: sine along the drape height
                    float wave = sin(time * _WindFrequency + posOS.y * 4.0 + posOS.x * 2.0);

                    // Secondary wave: slower, different frequency for organic motion
                    float wave2 = sin(time * _WindFrequency * 0.7 + posOS.y * 2.5 + posOS.x * 3.0 + 1.7);

                    // Gust: slow large-scale variation
                    float gust = sin(time * _GustSpeed + Hash(posOS.xz) * 6.28) * _GustStrength;

                    // Combine waves
                    float displacement = (wave * 0.6 + wave2 * 0.4) * _WindAmplitude + gust;
                    displacement *= _WindStrength * weight;

                    // Per-vertex noise so adjacent vertices don't move in perfect sync
                    float noise = Hash(posOS.xz + floor(_Time.y * 3.0)) * 0.3 + 0.7;
                    displacement *= noise;

                    // Apply along wind direction (object space X/Z)
                    float2 windDir = normalize(float2(_WindDirX, _WindDirZ) + 0.001);
                    posOS.x += displacement * windDir.x;
                    posOS.z += displacement * windDir.y;

                    // Slight vertical lift on the peaks (fabric billows up)
                    posOS.y += abs(displacement) * 0.15;
                }

                // Standard transform
                VertexPositionInputs posInputs = GetVertexPositionInputs(posOS);
                float4 clipPos = posInputs.positionCS;

                // PSX vertex snapping
                float2 snapRes = _VertexSnapResolution.xy;
                if (snapRes.x > 0 && snapRes.y > 0)
                {
                    clipPos.xy = floor(clipPos.xy / clipPos.w * snapRes + 0.5)
                               / snapRes * clipPos.w;
                }

                // Depth bias
                clipPos.z += _DepthBias * 0.0001;

                output.positionCS = clipPos;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionWS = posInputs.positionWS;
                output.fogFactor = ComputeFogFactor(clipPos.z);

                // UV: perspective-correct and affine
                float2 uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.uvCorrect = uv;
                float w = clipPos.w;
                output.uvAffine = float3(uv * w, w);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Affine UV interpolation (PSX style)
                float2 uvAffine = input.uvAffine.xy / input.uvAffine.z;
                float2 uv = lerp(input.uvCorrect, uvAffine, _AffineIntensity);

                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                half4 color = texColor * _Color;

                // Simple N dot L lighting
                float3 normalWS = normalize(input.normalWS);
                // Flip normal for back faces so both sides light correctly
                normalWS = normalWS * (dot(normalWS, GetWorldSpaceViewDir(input.positionWS)) > 0 ? 1 : -1);

                Light mainLight = GetMainLight();
                half NdotL = saturate(dot(normalWS, mainLight.direction));
                half3 lighting = mainLight.color * NdotL + half3(0.15, 0.15, 0.18); // ambient floor

                color.rgb *= lighting;

                // Fog
                color.rgb = MixFog(color.rgb, input.fogFactor);

                return color;
            }
            ENDHLSL
        }

        // ── Shadow caster pass so drapes cast shadows ────────────────
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            #pragma vertex vertShadow
            #pragma fragment fragShadow

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4  _Color;
                float4 _VertexSnapResolution;
                half   _AffineIntensity;
                float  _DepthBias;
                half   _WindStrength;
                half   _WindSpeed;
                half   _WindAmplitude;
                half   _WindFrequency;
                half   _GustStrength;
                half   _GustSpeed;
                half   _WindDirX;
                half   _WindDirZ;
                half   _PinTop;
            CBUFFER_END

            float Hash(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            Varyings vertShadow(Attributes input)
            {
                Varyings output;
                float3 posOS = input.positionOS.xyz;

                float weight = input.uv.y * input.uv.y;

                if (_WindStrength > 0.001)
                {
                    float time = _Time.y * _WindSpeed;
                    float wave = sin(time * _WindFrequency + posOS.y * 4.0 + posOS.x * 2.0);
                    float wave2 = sin(time * _WindFrequency * 0.7 + posOS.y * 2.5 + posOS.x * 3.0 + 1.7);
                    float gust = sin(time * _GustSpeed + Hash(posOS.xz) * 6.28) * _GustStrength;
                    float displacement = (wave * 0.6 + wave2 * 0.4) * _WindAmplitude + gust;
                    displacement *= _WindStrength * weight;
                    float noise = Hash(posOS.xz + floor(_Time.y * 3.0)) * 0.3 + 0.7;
                    displacement *= noise;
                    float2 windDir = normalize(float2(_WindDirX, _WindDirZ) + 0.001);
                    posOS.x += displacement * windDir.x;
                    posOS.z += displacement * windDir.y;
                    posOS.y += abs(displacement) * 0.15;
                }

                output.positionCS = TransformObjectToHClip(posOS);
                return output;
            }

            half4 fragShadow(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
