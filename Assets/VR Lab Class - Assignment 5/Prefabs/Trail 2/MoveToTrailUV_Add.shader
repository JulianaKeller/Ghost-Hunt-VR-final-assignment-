/*
Trail 컴포넌트가 Tiling 으로 설정되어야함. (그래야 스크롤을 위한 UV가 Trail 세그먼트 크기와 상관 없이 일정하게 나옴)
*/

Shader "MoveToTrailUV/MoveToTrailUV_Add"
{
    Properties
    {
        _MainTex("Main Texture (RGB)", 2D) = "white" {}
        _MainTexVFade("MainTex V Fade", Range(0, 1)) = 0
        _MainTexVFadePow("MainTex V Fade Pow", Float) = 1
        _MainTexPow("Main Texture Gamma", Float) = 1
        _MainTexMultiplier("Main Texture Multiplier", Float) = 1
        _TintTex("Tint Texture (RGB)", 2D) = "white" {}
        _Multiplier("Multiplier", Float) = 1
        _MainScrollSpeedU("Main Scroll U Speed", Float) = 10
        _MainScrollSpeedV("Main Scroll V Speed", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" } // CHANGED
        Blend One One // Additive
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc" // CHANGED

            struct appdata
            {
                float4 position : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float2 uvOrigin : TEXCOORD1;
                float4 positionHCS : SV_POSITION;
                fixed4 color : COLOR;
            };

            sampler2D _MainTex; // CHANGED
            sampler2D _TintTex; // CHANGED
            float4 _MainTex_ST;
            float _MainTexVFade;
            float _MainTexVFadePow;
            float _MainTexPow;
            float _MainTexMultiplier;
            float _Multiplier;
            float _MainScrollSpeedU;
            float _MainScrollSpeedV;
            float _MoveToMaterialUV;

            v2f vert(appdata IN)
            {
                v2f o;
                o.positionHCS = UnityObjectToClipPos(IN.position); // CHANGED
                o.uv = IN.uv * _MainTex_ST.xy + _MainTex_ST.zw;
                o.uv.x -= frac(_Time.y * _MainScrollSpeedU) + _MoveToMaterialUV;
                o.uv.y -= frac(_Time.y * _MainScrollSpeedV);
                o.uvOrigin = IN.uv;
                o.color = IN.color;
                return o;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 mainTex = tex2D(_MainTex, IN.uv); // CHANGED
                
                float vFade = 1 - abs(IN.uvOrigin.y - 0.5) * 2;
                vFade = pow(abs(vFade), _MainTexVFadePow);
                vFade = lerp(1, vFade, _MainTexVFade);
                mainTex.rgb *= vFade;
                mainTex.rgb = pow(abs(mainTex.rgb), _MainTexPow) * _MainTexMultiplier;
                
                float intensity = _Multiplier * IN.color.a;
                
                float avr = mainTex.r * 0.3333 + mainTex.g * 0.3334 + mainTex.b * 0.3333;
                avr = saturate(avr * intensity);
                fixed4 col = tex2D(_TintTex, float2(avr, 0.5)); // CHANGED
                
                float intensityHigh = max(1, intensity);
                col.rgb *= intensityHigh * IN.color.rgb;
                return col;
            }
            ENDCG
        }
    }
}

