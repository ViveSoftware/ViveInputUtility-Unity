// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "hand_bluefilloutline_uv1"
{
	Properties
	{
		_hand_color("hand_color", Color) = (0.2078431,0.4980392,0.8980392,0)
		[HDR]_handglow_color("handglow_color", Color) = (0.1137255,0.2588235,0.7490196,0)
		[HDR]_linecolor("linecolor", Color) = (0.02731139,0.4233265,1.304119,0)
		_OutlineThickness("OutlineThickness", Range( 0 , 0.01)) = 0.001
		_line_opacity("line_opacity", Range( 0 , 1)) = 0.5
		_power("power", Range( 0 , 1)) = 0.77
		_opacity("opacity", Range( 0 , 1)) = 0.6
		_AlphaText("AlphaText", 2D) = "white" {}
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
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
		
		
		
		#if defined(SHADER_API_D3D11) || defined(SHADER_API_XBOXONE) || defined(UNITY_COMPILER_HLSLCC) || defined(SHADER_API_PSSL) || (defined(SHADER_TARGET_SURFACE_ANALYSIS) && !defined(SHADER_TARGET_SURFACE_ANALYSIS_MOJOSHADER))//ASE Sampler Macros
		#define SAMPLE_TEXTURE2D(tex,samplerTex,coord) tex.Sample(samplerTex,coord)
		#else//ASE Sampling Macros
		#define SAMPLE_TEXTURE2D(tex,samplerTex,coord) tex2D(tex,coord)
		#endif//ASE Sampling Macros
		
		
		struct Input
		{
			float2 uv_texcoord;
		};
		uniform float _OutlineThickness;
		uniform float4 _linecolor;
		uniform float _line_opacity;
		UNITY_DECLARE_TEX2D_NOSAMPLER(_AlphaText);
		SamplerState sampler_AlphaText;
		
		void outlineVertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			float outlineVar = _OutlineThickness;
			v.vertex.xyz += ( v.normal * outlineVar );
		}
		inline half4 LightingOutline( SurfaceOutput s, half3 lightDir, half atten ) { return half4 ( 0,0,0, s.Alpha); }
		void outlineSurf( Input i, inout SurfaceOutput o )
		{
			float4 tex2DNode41 = SAMPLE_TEXTURE2D( _AlphaText, sampler_AlphaText, i.uv_texcoord );
			o.Emission = (_linecolor).rgba.rgb;
			o.Alpha = ( _line_opacity * tex2DNode41 ).r;
		}
		ENDCG
		

		Tags{ "RenderType" = "Opaque"  "Queue" = "Transparent+0" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
		Cull Back
		Blend SrcAlpha OneMinusSrcAlpha
		
		CGPROGRAM
		#pragma target 3.0
		#if defined(SHADER_API_D3D11) || defined(SHADER_API_XBOXONE) || defined(UNITY_COMPILER_HLSLCC) || defined(SHADER_API_PSSL) || (defined(SHADER_TARGET_SURFACE_ANALYSIS) && !defined(SHADER_TARGET_SURFACE_ANALYSIS_MOJOSHADER))//ASE Sampler Macros
		#define SAMPLE_TEXTURE2D(tex,samplerTex,coord) tex.Sample(samplerTex,coord)
		#else//ASE Sampling Macros
		#define SAMPLE_TEXTURE2D(tex,samplerTex,coord) tex2D(tex,coord)
		#endif//ASE Sampling Macros

		#pragma surface surf Unlit keepalpha noshadow vertex:vertexDataFunc 
		struct Input
		{
			float3 worldPos;
			float3 worldNormal;
			float2 uv_texcoord;
		};

		uniform float4 _hand_color;
		uniform float4 _handglow_color;
		uniform float _power;
		uniform float _opacity;
		UNITY_DECLARE_TEX2D_NOSAMPLER(_AlphaText);
		SamplerState sampler_AlphaText;

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
			float4 tex2DNode41 = SAMPLE_TEXTURE2D( _AlphaText, sampler_AlphaText, i.uv_texcoord );
			o.Alpha = ( ( temp_output_9_0 + _opacity ) * tex2DNode41 ).r;
		}

		ENDCG
	}
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18400
320;246;1261;651;2231.18;-140.714;1.72053;True;False
Node;AmplifyShaderEditor.RangedFloatNode;1;-1274.458,39.50004;Inherit;False;Property;_power;power;6;0;Create;True;0;0;False;0;False;0.77;0.77;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.FresnelNode;5;-981.9887,63.71713;Inherit;False;Standard;WorldNormal;ViewDir;False;False;5;0;FLOAT3;0,0,1;False;4;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;5;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;42;-1585.983,432.344;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;6;-1036.399,-182.4641;Inherit;False;Property;_handglow_color;handglow_color;2;1;[HDR];Create;True;0;0;False;0;False;0.1137255,0.2588235,0.7490196,0;0.1137255,0.2588235,0.7490196,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;9;-651.3372,31.55554;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;41;-1305.536,397.9333;Inherit;True;Property;_AlphaText;AlphaText;8;0;Create;True;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;11;-881.2112,295.0575;Inherit;False;Property;_opacity;opacity;7;0;Create;True;0;0;False;0;False;0.6;0.6;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;4;-1059.368,801.1543;Inherit;False;Property;_line_opacity;line_opacity;5;0;Create;True;0;0;False;0;False;0.5;0.5;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;2;-948.9506,583.9514;Inherit;False;Property;_linecolor;linecolor;3;1;[HDR];Create;True;0;0;False;0;False;0.02731139,0.4233265,1.304119,0;0.02731139,0.4233265,1.304119,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ComponentMaskNode;7;-648.8818,626.1487;Inherit;False;True;True;True;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;3;-552.1449,1023.464;Inherit;False;Property;_OutlineThickness;OutlineThickness;4;0;Create;True;0;0;False;0;False;0.001;0.001;0;0.01;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;10;-637.3572,-311.4575;Inherit;False;Property;_hand_color;hand_color;1;0;Create;True;0;0;False;0;False;0.2078431,0.4980392,0.8980392,0;0.2078431,0.4980392,0.8980392,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;15;-504.4844,162.7557;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;40;-643.791,784.3417;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.OutlineNode;8;-393.6221,678.4123;Inherit;False;0;True;Transparent;0;0;Front;3;0;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;21;-257.7149,287.2043;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;14;-314.7592,-106.4899;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;0,0;Float;False;True;-1;2;ASEMaterialInspector;0;0;Unlit;hand_bluefilloutline_uv1;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;True;0;Custom;0.5;True;False;0;True;Opaque;;Transparent;All;14;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;False;2;5;False;-1;10;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;0;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;True;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;5;3;1;0
WireConnection;9;0;6;0
WireConnection;9;1;5;0
WireConnection;41;1;42;0
WireConnection;7;0;2;0
WireConnection;15;0;9;0
WireConnection;15;1;11;0
WireConnection;40;0;4;0
WireConnection;40;1;41;0
WireConnection;8;0;7;0
WireConnection;8;2;40;0
WireConnection;8;1;3;0
WireConnection;21;0;15;0
WireConnection;21;1;41;0
WireConnection;14;0;10;0
WireConnection;14;1;9;0
WireConnection;0;2;14;0
WireConnection;0;9;21;0
WireConnection;0;11;8;0
ASEEND*/
//CHKSM=21ADAA91B80863D90D2080C75F8FFDE3179E21A8