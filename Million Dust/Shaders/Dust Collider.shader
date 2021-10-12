// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Rito/Dust Collider"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,0)
		_Intersection("Intersection", Range( 0 , 1)) = 0.1
		_Ambient("Ambient", Range( 0 , 1)) = 0.1
		_FresnelPower("Fresnel Power", Range( 0 , 20)) = 5
		_FresnelIntensity("Fresnel Intensity", Range( 0 , 20)) = 1
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0" "IgnoreProjector" = "True" "ForceNoShadowCasting" = "True" "IsEmissive" = "true"  }
		Cull Back
		CGPROGRAM
		#include "UnityCG.cginc"
		#pragma target 3.0
		#pragma surface surf Unlit alpha:fade keepalpha noshadow noambient novertexlights nolightmap  nodynlightmap nodirlightmap nofog nometa noforwardadd 
		struct Input
		{
			float4 screenPos;
			float3 worldPos;
			float3 worldNormal;
		};

		uniform float4 _Color;
		uniform float _Ambient;
		uniform float _Intersection;
		UNITY_DECLARE_DEPTH_TEXTURE( _CameraDepthTexture );
		uniform float4 _CameraDepthTexture_TexelSize;
		uniform float _FresnelIntensity;
		uniform float _FresnelPower;

		inline half4 LightingUnlit( SurfaceOutput s, half3 lightDir, half atten )
		{
			return half4 ( 0, 0, 0, s.Alpha );
		}

		void surf( Input i , inout SurfaceOutput o )
		{
			float4 ase_screenPos = float4( i.screenPos.xyz , i.screenPos.w + 0.00000000001 );
			float4 ase_screenPosNorm = ase_screenPos / ase_screenPos.w;
			ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
			float eyeDepth5 = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE( _CameraDepthTexture, ase_screenPosNorm.xy ));
			float3 ase_worldPos = i.worldPos;
			float3 ase_worldViewDir = normalize( UnityWorldSpaceViewDir( ase_worldPos ) );
			float3 ase_worldNormal = i.worldNormal;
			float fresnelNdotV11 = dot( ase_worldNormal, ase_worldViewDir );
			float fresnelNode11 = ( 0.0 + _FresnelIntensity * pow( 1.0 - fresnelNdotV11, _FresnelPower ) );
			float temp_output_14_0 = ( ( _Color.a * _Ambient ) + ( _Color.a * ( saturate( ( ( _Intersection + ase_screenPos.w ) - eyeDepth5 ) ) + fresnelNode11 ) ) );
			o.Emission = ( _Color * temp_output_14_0 ).rgb;
			o.Alpha = temp_output_14_0;
		}

		ENDCG
	}
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18900
375;156;1468;863;1386.146;280.1286;1.3;True;False
Node;AmplifyShaderEditor.ScreenPosInputsNode;3;-905.3029,227.2883;Float;False;1;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;4;-995.2861,145.2584;Inherit;False;Property;_Intersection;Intersection;1;0;Create;True;0;0;0;False;0;False;0.1;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;6;-703.3029,221.2883;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScreenDepthNode;5;-910.3029,394.2883;Inherit;False;0;True;1;0;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;7;-580.3029,263.2883;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;19;-925.9464,494.6714;Inherit;False;Property;_FresnelIntensity;Fresnel Intensity;4;0;Create;True;0;0;0;False;0;False;1;2;0;20;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;18;-927.2462,573.9714;Inherit;False;Property;_FresnelPower;Fresnel Power;3;0;Create;True;0;0;0;False;0;False;5;0;0;20;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;8;-455.3028,263.2883;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FresnelNode;11;-624.0026,413.7923;Inherit;True;Standard;WorldNormal;ViewDir;False;False;5;0;FLOAT3;0,0,1;False;4;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;12;-312.9026,322.6923;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;16;-474.456,163.6108;Inherit;False;Property;_Ambient;Ambient;2;0;Create;True;0;0;0;False;0;False;0.1;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;2;-399,-7.5;Inherit;False;Property;_Color;Color;0;0;Create;True;0;0;0;False;0;False;1,1,1,0;1,1,1,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;13;-185.2026,266.2922;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;15;-174.1556,105.1108;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;14;-40.25552,155.8106;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;17;72.84542,41.41061;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;24;256.1002,-7.799996;Float;False;True;-1;2;ASEMaterialInspector;0;0;Unlit;Rito/Dust Collider;False;False;False;False;True;True;True;True;True;True;True;True;False;False;True;True;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Transparent;0.5;True;False;0;False;Transparent;;Transparent;All;14;all;True;True;True;True;0;False;-1;False;1;False;-1;255;False;-1;255;False;-1;6;False;-1;0;False;-1;0;False;-1;3;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;False;2;5;False;-1;10;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;False;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;6;0;4;0
WireConnection;6;1;3;4
WireConnection;7;0;6;0
WireConnection;7;1;5;0
WireConnection;8;0;7;0
WireConnection;11;2;19;0
WireConnection;11;3;18;0
WireConnection;12;0;8;0
WireConnection;12;1;11;0
WireConnection;13;0;2;4
WireConnection;13;1;12;0
WireConnection;15;0;2;4
WireConnection;15;1;16;0
WireConnection;14;0;15;0
WireConnection;14;1;13;0
WireConnection;17;0;2;0
WireConnection;17;1;14;0
WireConnection;24;2;17;0
WireConnection;24;9;14;0
ASEEND*/
//CHKSM=E3BEDC6D6595BA94F266DBE7E3BD4CA8633437D1