// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Rito/Box Collider"
{
	Properties
	{
		_Width("Width", Range( 0 , 1)) = 0.2
		_Power("Power", Range( 0 , 8)) = 1
		_Smoothness("Smoothness", Range( 0 , 1)) = 0.2
		[HDR]_Color("Color", Color) = (1,1,1,0)
		[Space(6)]_Intersection1("Intersection", Range( 0 , 1)) = 0.1
		_IntersectionIntensity("Intersection Intensity", Range( 0 , 1)) = 1
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0" "IgnoreProjector" = "True" "ForceNoShadowCasting" = "True" "IsEmissive" = "true"  }
		Cull Back
		CGPROGRAM
		#include "UnityCG.cginc"
		#pragma target 3.0
		#pragma multi_compile_instancing
		#pragma surface surf Unlit alpha:fade keepalpha noshadow noambient novertexlights nolightmap  nodynlightmap nodirlightmap nofog nometa noforwardadd 
		struct Input
		{
			float2 uv_texcoord;
			float4 screenPos;
		};

		uniform float4 _Color;
		uniform float _Width;
		uniform float _Smoothness;
		uniform float _Power;
		uniform float _Intersection1;
		UNITY_DECLARE_DEPTH_TEXTURE( _CameraDepthTexture );
		uniform float4 _CameraDepthTexture_TexelSize;
		uniform float _IntersectionIntensity;

		inline half4 LightingUnlit( SurfaceOutput s, half3 lightDir, half atten )
		{
			return half4 ( 0, 0, 0, s.Alpha );
		}

		void surf( Input i , inout SurfaceOutput o )
		{
			o.Emission = _Color.rgb;
			float temp_output_14_0 = ( 0.5 * ( 1.0 - _Width ) );
			float2 temp_cast_1 = (temp_output_14_0).xx;
			float2 temp_cast_2 = (( temp_output_14_0 + _Smoothness )).xx;
			float2 temp_cast_3 = (0.5).xx;
			float2 smoothstepResult10 = smoothstep( temp_cast_1 , temp_cast_2 , abs( ( i.uv_texcoord - temp_cast_3 ) ));
			float2 break9 = smoothstepResult10;
			float4 ase_screenPos = float4( i.screenPos.xyz , i.screenPos.w + 0.00000000001 );
			float4 ase_screenPosNorm = ase_screenPos / ase_screenPos.w;
			ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
			float eyeDepth22 = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE( _CameraDepthTexture, ase_screenPosNorm.xy ));
			o.Alpha = ( pow( saturate( ( break9.x + break9.y ) ) , _Power ) + ( saturate( ( ( _Intersection1 + ase_screenPos.w ) - eyeDepth22 ) ) * _IntersectionIntensity ) );
		}

		ENDCG
	}
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18900
281;93;1468;851;1597.098;367.8946;1.388757;True;False
Node;AmplifyShaderEditor.RangedFloatNode;7;-1603.588,235.9653;Inherit;False;Property;_Width;Width;0;0;Create;True;0;0;0;False;0;False;0.2;0.2900154;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;4;-1340.788,133.4654;Inherit;False;Constant;_05;0.5;0;0;Create;True;0;0;0;False;0;False;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TexCoordVertexDataNode;2;-1388.187,4.165368;Inherit;False;0;2;0;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.OneMinusNode;16;-1341.011,240.7824;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;12;-1324.388,317.1655;Inherit;False;Property;_Smoothness;Smoothness;2;0;Create;True;0;0;0;False;0;False;0.2;0.263312;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;3;-1187.788,34.46539;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;14;-1188.41,215.4825;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.AbsOpNode;5;-1057.788,34.46539;Inherit;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;11;-1033.088,250.7654;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;10;-869.7918,35.4654;Inherit;True;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT2;1,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;21;-828.5166,333.6688;Inherit;False;Property;_Intersection1;Intersection;4;0;Create;True;0;0;0;False;1;Space(6);False;0.1;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ScreenPosInputsNode;20;-756.5873,437.3288;Float;False;1;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;23;-554.5874,431.3288;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;9;-654.7916,34.46542;Inherit;False;FLOAT2;1;0;FLOAT2;0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.ScreenDepthNode;22;-761.5873,604.3288;Inherit;False;0;True;1;0;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;8;-544.0532,37.29102;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;24;-431.5877,473.3288;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;25;-306.5878,473.3288;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;13;-433.9412,37.94273;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;18;-524.7607,262.4853;Inherit;False;Property;_Power;Power;1;0;Create;True;0;0;0;False;0;False;1;0;0;8;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;28;-444.4302,580.6263;Inherit;False;Property;_IntersectionIntensity;Intersection Intensity;5;0;Create;True;0;0;0;False;0;False;1;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;17;-212.1318,99.91827;Inherit;True;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;27;-125.0159,476.4697;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;26;31.91321,191.7743;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;19;-187.8065,-78.10121;Inherit;False;Property;_Color;Color;3;1;[HDR];Create;True;0;0;0;False;0;False;1,1,1,0;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;157.6002,-95.39728;Float;False;True;-1;2;ASEMaterialInspector;0;0;Unlit;Rito/Box Collider;False;False;False;False;True;True;True;True;True;True;True;True;False;False;True;True;True;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Transparent;0.5;True;False;0;False;Transparent;;Transparent;All;14;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;False;2;5;False;-1;10;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;False;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;16;0;7;0
WireConnection;3;0;2;0
WireConnection;3;1;4;0
WireConnection;14;0;4;0
WireConnection;14;1;16;0
WireConnection;5;0;3;0
WireConnection;11;0;14;0
WireConnection;11;1;12;0
WireConnection;10;0;5;0
WireConnection;10;1;14;0
WireConnection;10;2;11;0
WireConnection;23;0;21;0
WireConnection;23;1;20;4
WireConnection;9;0;10;0
WireConnection;8;0;9;0
WireConnection;8;1;9;1
WireConnection;24;0;23;0
WireConnection;24;1;22;0
WireConnection;25;0;24;0
WireConnection;13;0;8;0
WireConnection;17;0;13;0
WireConnection;17;1;18;0
WireConnection;27;0;25;0
WireConnection;27;1;28;0
WireConnection;26;0;17;0
WireConnection;26;1;27;0
WireConnection;0;2;19;0
WireConnection;0;9;26;0
ASEEND*/
//CHKSM=C382247C11F3EF547467927D3371199E355FA250