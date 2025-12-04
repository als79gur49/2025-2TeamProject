Shader "URP/OutlineAlwaysOnTop_Unlit"
{
    Properties
    {
        _OutlineColor ("Outline Color (HDR)", Color) = (1,0.85,0.1,1)
        _OutlineWidth ("Outline Width (World)", Range(0,0.1)) = 0.02

        _RimPower  ("Rim Power", Range(0,8)) = 3.0
        _RimStart  ("Rim Start", Range(0,1)) = 0.30
        _RimEnd    ("Rim End",   Range(0,1)) = 0.95
    }

    SubShader
    {
        // 항상 위에 오버레이: 투명 큐 + ZWrite Off + ZTest Always
        Tags{
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "RenderPipeline"="UniversalRenderPipeline"
        }
        LOD 100

        Pass
        {
            Name "Outline"
            // 외곽선: 앞면을 컬링하고 뒷면만 그린다(뒤집힌 헐)
            Cull Front

            // 투명 블렌딩(원하면 Additive로 변경 가능: Blend One One)
            Blend SrcAlpha OneMinusSrcAlpha

            ZWrite Off
            ZTest  Always

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex   vert
            #pragma fragment frag

            // URP 코어 유틸
            #include "UnityCG.cginc"

            float4 _OutlineColor;
            float  _OutlineWidth;
            float  _RimPower;
            float  _RimStart, _RimEnd;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos   : SV_POSITION;
                float3 nWS   : TEXCOORD0; // 월드 노멀
                float3 wpos  : TEXCOORD1; // 월드 위치
            };

            v2f vert (appdata v)
            {
                v2f o;

                // 월드 노멀/위치
                float3 nWS = UnityObjectToWorldNormal(v.normal);
                float4 wpos = mul(unity_ObjectToWorld, v.vertex);

                // 월드 공간으로 아웃라인 두께만큼 확장(비균등 스케일에도 안전)
                wpos.xyz += normalize(nWS) * _OutlineWidth;

                o.pos  = UnityWorldToClipPos(wpos.xyz);
                o.wpos = wpos.xyz;
                o.nWS  = normalize(nWS);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 카메라 시선 벡터
                float3 V = normalize(_WorldSpaceCameraPos.xyz - i.wpos);

                // 림 마스크: abs(N·V) 기반
                float ndv = abs(dot(i.nWS, V));
                float rim = 1.0 - saturate(ndv);
                rim = pow(saturate(rim), max(_RimPower, 1e-4));
                rim = smoothstep(_RimStart, _RimEnd, rim);

                // 색/알파: 내부는 0, 외곽만 표시
                float3 rgb = _OutlineColor.rgb * rim;
                float  a   = _OutlineColor.a   * rim;

                return float4(rgb, a);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
