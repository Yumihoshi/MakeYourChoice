
Shader "Pool/PoolWater"
{
    Properties
    {
        _Color ("Water Color", Color) = (0.18, 0.46, 0.71, 0.55)
        _WaveSpeed ("Wave Speed", Float) = 0.35
        _WaveScale ("Wave Scale", Float) = 1.6
        _Fresnel ("Fresnel Strength", Float) = 0.35
        _NormalStrength ("Normal Strength", Float) = 0.25
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings  { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 positionWS : TEXCOORD1; };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _WaveSpeed;
                float _WaveScale;
                float _Fresnel;
                float _NormalStrength;
            CBUFFER_END

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                OUT.uv = IN.uv;
                return OUT;
            }

            float4 frag (Varyings IN) : SV_Target
            {
                float t = _Time.y * _WaveSpeed;
                float2 uv = IN.uv * _WaveScale;

                // 两组正弦叠加的水纹，法线用解析式求导，不依赖 dFdx/dFdy。
                float a = 6.28318 * uv.x + t * 2.0;
                float b = 5.0 * uv.y - t * 1.6;
                float wave = (sin(a) + sin(b)) * 0.5;

                float dx = 0.5 * cos(a) * 6.28318 * _WaveScale;
                float dy = 0.5 * cos(b) * 5.0 * _WaveScale;
                float3 n = normalize(float3(-dx * _NormalStrength, 1.0, -dy * _NormalStrength));

                float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - IN.positionWS);
                float fres = pow(1.0 - saturate(dot(n, viewDir)), 3.0) * _Fresnel;

                float3 col = _Color.rgb + fres + wave * 0.02;
                float alpha = saturate(_Color.a + fres * 0.5);
                return float4(col, alpha);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
