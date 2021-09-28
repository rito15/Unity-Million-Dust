Shader "Rito/Dust"
{
    Properties
    {
        //_MainTex ("Texture", 2D) = "white" {}
        _Color("Color", Color) = (0.2, 0.2, 0.2, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #define TRUE 1
            #define FALSE 0

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 normal : TEXCOORD0;
                int isAlive : TEXCOORD1;
            };

            struct Dust
            {
                float3 position;
                int isAlive;
            };

            uniform float _Scale;
            StructuredBuffer<Dust> _DustBuffer;

            v2f vert (appdata_full v, uint instanceID : SV_InstanceID)
            {
                v2f o;
                // 먼지 생존 여부 받아와서 프래그먼트 쉐이더에 전달
                o.isAlive = _DustBuffer[instanceID].isAlive;

                // 먼지 크기 결정
                v.vertex *= _Scale; 

                // 먼지 위치 결정
                float3 instancePos = _DustBuffer[instanceID].position;
                float3 worldPos = v.vertex + instancePos;

                o.pos = mul(UNITY_MATRIX_VP, float4(worldPos, 1));
                o.normal = v.normal;
                return o;
            }

            fixed4 _Color;

            fixed4 frag (v2f i) : SV_Target
            {
                // 죽은 먼지는 렌더링 X
                if(i.isAlive == FALSE)
                {
                    discard;
                }

                return _Color;
            }
            ENDCG
        }
    }
}
