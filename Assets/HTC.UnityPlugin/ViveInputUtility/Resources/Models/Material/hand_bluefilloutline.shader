// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "hand_bluefilloutline"
{
	Properties
	{
		[HDR]_handglow_color("handglow_color", Color) = (1,1,1,0)
		_line_opacity("line_opacity", Range( 0 , 1)) = 0.5
		_thickness("thickness", Range( 0 , 0.1)) = 0.001
		_power("power", Range( 0 , 1)) = 0.5
		[HDR]_linecolor("linecolor", Color) = (1,1,1,0)
		_opacity("opacity", Range( 0 , 1)) = 0.2979249
		_hand_color("hand_color", Color) = (0.004360993,0.3530807,0.9245283,0)
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Pass
		{
			ColorMask 0
			ZWrite On
		}

		Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0"}
		Cull Front
		CGPROGRAM
		#pragma target 3.0
		#pragma surface outlineSurf Outline nofog alpha:fade  keepalpha noshadow noambient novertexlights nolightmap nodynlightmap nodirlightmap nometa noforwardadd vertex:outlineVertexDataFunc 
		
		
		
		
		struct Input
		{
			half filler;
		};
		uniform float _thickness;
		uniform float4 _linecolor;
		uniform float _line_opacity;
		
		void outlineVertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			float outlineVar = _thickness;
			v.vertex.xyz += ( v.normal * outlineVar );
		}
		inline half4 LightingOutline( SurfaceOutput s, half3 lightDir, half atten ) { return half4 ( 0,0,0, s.Alpha); }
		void outlineSurf( Input i, inout SurfaceOutput o )
		{
			o.Emission = (_linecolor).rgba.rgb;
			o.Alpha = _line_opacity;
		}
		ENDCG
		

		Tags{ "RenderType" = "Opaque"  "Queue" = "Transparent+0" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
		Cull Back
		Blend SrcAlpha OneMinusSrcAlpha
		
		CGPROGRAM
		#pragma target 3.0
		#pragma surface surf Unlit keepalpha noshadow vertex:vertexDataFunc 
		struct Input
		{
			float3 worldPos;
			float3 worldNormal;
		};

		uniform float4 _hand_color;
		uniform float4 _handglow_color;
		uniform float _power;
		uniform float _opacity;

		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			v.vertex.xyz += 0;
		}

		inline half4 LightingUnlit( SurfaceOutput s, half3 lightDir, half atten )
		{
			return half4 ( 0, 0, 0, s.Alpha );
		}

		void surf( Input i , inout SurfaceOutput o )
		{
			float3 ase_worldPos = i.worldPos;
			float3 ase_worldViewDir = normalize( UnityWorldSpaceViewDir( ase_worldPos ) );
			float3 ase_worldNormal = i.worldNormal;
			float fresnelNdotV5 = dot( ase_worldNormal, ase_worldViewDir );
			float fresnelNode5 = ( 0.0 + 1.0 * pow( 1.0 - fresnelNdotV5, _power ) );
			float4 temp_output_9_0 = ( _handglow_color * fresnelNode5 );
			o.Emission = ( _hand_color + temp_output_9_0 ).rgb;
			o.Alpha = ( temp_output_9_0 + _opacity ).r;
		}

		ENDCG
	}
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18400
274;250;1247;619;1810.997;444.2097;1.782319;True;False
Node;AmplifyShaderEditor.RangedFloatNode;1;-1274.458,39.50004;Inherit;False;Property;_power;power;4;0;Create;True;0;0;False;0;False;0.5;0.31;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;2;-862.4654,363.1876;Inherit;False;Property;_linecolor;linecolor;5;1;[HDR];Create;True;0;0;False;0;False;1,1,1,0;0.01378214,0.3410338,0.9739395,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.FresnelNode;5;-981.9887,63.71713;Inherit;False;Standard;WorldNormal;ViewDir;False;False;5;0;FLOAT3;0,0,1;False;4;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;5;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;6;-1036.399,-182.4641;Inherit;False;Property;_handglow_color;handglow_color;1;1;[HDR];Create;True;0;0;False;0;False;1,1,1,0;0.360376,1.50457,2.297397,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;3;-559.6226,658.3411;Inherit;False;Property;_thickness;thickness;3;0;Create;True;0;0;False;0;False;0.001;0.001;0;0.1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;4;-635.4855,503.4536;Inherit;False;Property;_line_opacity;line_opacity;2;0;Create;True;0;0;False;0;False;0.5;0.5;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;7;-562.3966,405.3846;Inherit;False;True;True;True;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;9;-651.3372,31.55554;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;11;-623.1006,278.4909;Inherit;False;Property;_opacity;opacity;6;0;Create;True;0;0;False;0;False;0.2979249;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;10;-637.3572,-311.4575;Inherit;False;Property;_hand_color;hand_color;7;0;Create;True;0;0;False;0;False;0.004360993,0.3530807,0.9245283,0;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.OutlineNode;8;-264.9986,430.6792;Inherit;False;0;True;Transparent;0;0;Front;3;0;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode;14;-314.7592,-106.4899;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;15;-298.7184,196.5044;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;0,0;Float;False;True;-1;2;ASEMaterialInspector;0;0;Unlit;hand_bluefilloutline;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;True;0;Custom;0.5;True;False;0;True;Opaque;;Transparent;All;14;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;False;2;5;False;-1;10;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;0;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;True;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;5;3;1;0
WireConnection;7;0;2;0
WireConnection;9;0;6;0
WireConnection;9;1;5;0
WireConnection;8;0;7;0
WireConnection;8;2;4;0
WireConnection;8;1;3;0
WireConnection;14;0;10;0
WireConnection;14;1;9;0
WireConnection;15;0;9;0
WireConnection;15;1;11;0
WireConnection;0;2;14;0
WireConnection;0;9;15;0
WireConnection;0;11;8;0
ASEEND*/
//CHKSM=B4ACDAC3EEDDCBE31914B5AD192B7952E69BE1C7