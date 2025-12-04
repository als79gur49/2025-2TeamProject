Shader "Custom/CloudUnlit"
{
    Properties
    {
        _MainTex        ("Cloud Texture (R = Mask)", 2D) = "white" {}
        _TintColor      ("Tint Color", Color) = (1,1,1,1)
        _AlphaMultiplier("Alpha Multiplier", Range(0,1)) = 0.6
        _ScrollSpeed    ("UV Scroll (XY)", Vector) = (0,0,0,0)
        _MaskPower      ("Mask Power", Range(0.2, 4)) = 1.5
        _EdgeInner      ("Edge Inner Radius", Range(0,1)) = 0.3
        _EdgeOuter      ("Edge Outer Radius", Range(0,1)) = 0.55
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4    _MainTex_ST;
            fixed4    _TintColor;
            float     _AlphaMultiplier;
            float4    _ScrollSpeed;
            float     _MaskPower;
            float     _EdgeInner;
            float     _EdgeOuter;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);

                float2 uv = TRANSFORM_TEX(v.uv, _MainTex);
                // 시간에 따라 UV 스크롤 (구름이 흘러가는 효과)
                uv += _ScrollSpeed.xy * _Time.y;
                o.uv = uv;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 텍스쳐 샘플
                fixed4 tex = tex2D(_MainTex, i.uv);

                // 기본 마스크 (R 채널)
                fixed mask = tex.r;

                // 1) 마스크 곡선 조정 – 덩어리 강조 / 잔털 줄이기
                mask = pow(mask, _MaskPower);

                // 2) 카드 가장자리 부드럽게: UV 중심에서의 거리로 원형 그라디언트 생성
                float2 uvCenter = i.uv - 0.5;            // (0.5,0.5) 기준 오프셋
                float  dist     = length(uvCenter);      // 중심으로부터의 거리
                float  edgeRad  = 1.0 - smoothstep(_EdgeInner, _EdgeOuter, dist);
                // 중심은 1, 가장자리는 0으로 떨어지는 값

                mask *= edgeRad;

                // 알파 계산
                fixed alpha = saturate(mask * _AlphaMultiplier * _TintColor.a);

                // 색은 순수 TintColor만 사용
                fixed3 rgb  = _TintColor.rgb;

                return fixed4(rgb, alpha);
            }
            ENDCG
        }
    }
}
