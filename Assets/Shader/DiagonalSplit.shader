Shader "Iris/DiagonalSplit"
{
    Properties
    {
        _MainTex ("Reaction Texture", 2D) = "black" {}
        _SplitProgress ("Split Progress", Range(0, 1)) = 0
        _EdgeSoftness ("Edge Softness", Range(0, 0.05)) = 0.01
        _EdgeColor ("Edge Color", Color) = (1, 1, 1, 0.8)
        _EdgeWidth ("Edge Line Width", Range(0, 0.02)) = 0.005
        _Tint ("Tint", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Overlay"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "DiagonalSplit"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float  _SplitProgress;
                float  _EdgeSoftness;
                half4  _EdgeColor;
                float  _EdgeWidth;
                half4  _Tint;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;

                // Diagonal line: top-right to bottom-left
                // d = 0 at bottom-left corner, d = 2 at top-right corner
                float d = uv.x + (1.0 - uv.y);

                // Threshold slides from 2.0 (offscreen right) to ~0.7 (covers upper-right portion)
                float threshold = lerp(2.2, 0.65, _SplitProgress);

                // Soft edge mask
                float mask = smoothstep(threshold - _EdgeSoftness, threshold + _EdgeSoftness, d);

                // Edge highlight line
                float edgeDist = abs(d - threshold);
                float edgeLine = 1.0 - smoothstep(0.0, _EdgeWidth, edgeDist);

                // Sample reaction texture
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                half4 col = tex * _Tint * input.color;

                // Combine: reaction texture in the masked area, edge line on top
                col.a *= mask;
                col.rgb = lerp(col.rgb, _EdgeColor.rgb, edgeLine * _EdgeColor.a * mask);

                return col;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
