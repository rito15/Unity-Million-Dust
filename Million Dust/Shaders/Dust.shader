Shader "Rito/Dust"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color("Color", Color) = (0.2, 0.2, 0.2, 1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent+1" "RenderType"="Transparent" "IgnoreProjector"="True" }
        ZWrite Off
		Lighting Off
        Fog { Mode Off }
        Blend SrcAlpha OneMinusSrcAlpha 

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
                float4 pos    : SV_POSITION;
                float3 uv     : TEXCOORD0;
                int isAlive   : TEXCOORD1;
            };

            struct Dust
            {
                float3 position;
                int isAlive;
            };

            // ========================================================================================
            //                                  Vertex Shader
            // ========================================================================================
            uniform float _Scale;
            StructuredBuffer<Dust> _DustBuffer;

            float4 CalculateVertex(float4 vertex, float3 worldPos)
            {
                float3 camUpVec      =  normalize( UNITY_MATRIX_V._m10_m11_m12 );
			    float3 camForwardVec = -normalize( UNITY_MATRIX_V._m20_m21_m22 );
			    float3 camRightVec   =  normalize( UNITY_MATRIX_V._m00_m01_m02 );
			    float4x4 camRotMat   = float4x4( camRightVec, 0, camUpVec, 0, camForwardVec, 0, 0, 0, 0, 1 );

                vertex = mul(vertex, camRotMat); // Billboard
                vertex.xyz *= _Scale;   // Scale
                vertex.xyz += worldPos; // Instance Position

                // World => VP => Clip
                return mul(UNITY_MATRIX_VP, vertex);
            }

            v2f vert (appdata_full v, uint instanceID : SV_InstanceID)
            {
                v2f o;

                o.isAlive = _DustBuffer[instanceID].isAlive;
                o.pos = CalculateVertex(v.vertex, _DustBuffer[instanceID].position);
                o.uv = v.texcoord;

                return o;
            }
            
            // ========================================================================================
            //                                  Fragment Shader
            // ========================================================================================
            sampler2D _MainTex;
            fixed4 _Color;

            fixed4 frag (v2f i) : SV_Target
            {
                // 죽은 먼지는 렌더링 X
                if(i.isAlive == FALSE)
                {
                    discard;
                }

                fixed4 col = tex2D(_MainTex, i.uv);
                col.rgb = _Color.rgb * col.a;

                return col;
            }
            ENDCG
        }
    }
}
