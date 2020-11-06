// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "hand_bluefilloutlineB"
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
		_AlphaLevel("AlphaLevel", Range( 0 , 0.5)) = 0.3
		_AlphaBlur("AlphaBlur", Range( 0.44 , 1)) = 0.44
		[HideInInspector] _texcoord3( "", 2D ) = "white" {}
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
			float2 uv3_texcoord3;
		};
		uniform float _OutlineThickness;
		uniform float4 _linecolor;
		uniform float _line_opacity;
		uniform float _AlphaLevel;
		uniform float _AlphaBlur;
		
		void outlineVertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			float outlineVar = _OutlineThickness;
			v.vertex.xyz += ( v.normal * outlineVar );
		}
		inline half4 LightingOutline( SurfaceOutput s, half3 lightDir, half atten ) { return half4 ( 0,0,0, s.Alpha); }
		void outlineSurf( Input i, inout SurfaceOutput o )
		{
			float2 uv3_TexCoord32 = i.uv3_texcoord3 + float2( 0,-0.08 );
			float cos34 = cos( (float)radians( 0 ) );
			float sin34 = sin( (float)radians( 0 ) );
			float2 rotator34 = mul( uv3_TexCoord32 - float2( 0.5,0.5 ) , float2x2( cos34 , -sin34 , sin34 , cos34 )) + float2( 0.5,0.5 );
			float2 CenteredUV15_g1 = ( rotator34 - float2( 0.5,0 ) );
			float2 break17_g1 = CenteredUV15_g1;
			float2 appendResult23_g1 = (float2(( length( CenteredUV15_g1 ) * 0.5 * 2.0 ) , ( atan2( break17_g1.x , break17_g1.y ) * ( 1.0 / 6.28318548202515 ) * 1.0 )));
			float smoothstepResult39 = smoothstep( _AlphaLevel , _AlphaBlur , appendResult23_g1.x);
			o.Emission = (_linecolor).rgba.rgb;
			o.Alpha = ( _line_opacity * smoothstepResult39 );
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
			float2 uv3_texcoord3;
		};

		uniform float4 _hand_color;
		uniform float4 _handglow_color;
		uniform float _power;
		uniform float _opacity;
		uniform float _AlphaLevel;
		uniform float _AlphaBlur;

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
			float2 uv3_TexCoord32 = i.uv3_texcoord3 + float2( 0,-0.08 );
			float cos34 = cos( (float)radians( 0 ) );
			float sin34 = sin( (float)radians( 0 ) );
			float2 rotator34 = mul( uv3_TexCoord32 - float2( 0.5,0.5 ) , float2x2( cos34 , -sin34 , sin34 , cos34 )) + float2( 0.5,0.5 );
			float2 CenteredUV15_g1 = ( rotator34 - float2( 0.5,0 ) );
			float2 break17_g1 = CenteredUV15_g1;
			float2 appendResult23_g1 = (float2(( length( CenteredUV15_g1 ) * 0.5 * 2.0 ) , ( atan2( break17_g1.x , break17_g1.y ) * ( 1.0 / 6.28318548202515 ) * 1.0 )));
			float smoothstepResult39 = smoothstep( _AlphaLevel , _AlphaBlur , appendResult23_g1.x);
			o.Alpha = ( ( temp_output_9_0 + _opacity ) * smoothstepResult39 ).r;
		}

		ENDCG
	}
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18400
342;379;1261;640;1251.955;-755.7175;1;True;False
Node;AmplifyShaderEditor.IntNode;29;-2430.415,856.8522;Inherit;False;Constant;_Angle;Angle;7;0;Create;True;0;0;False;0;False;0;0;0;1;INT;0
Node;AmplifyShaderEditor.RadiansOpNode;30;-2279.889,777.2302;Inherit;False;1;0;INT;0;False;1;INT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;32;-2525.911,565.259;Inherit;False;2;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,-0.08;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector2Node;31;-2455.262,694.9791;Inherit;False;Constant;_Vector0;Vector 0;8;0;Create;True;0;0;False;0;False;0.5,0.5;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.Vector2Node;33;-2296.543,952.5995;Inherit;False;Constant;_Vector1;Vector 1;7;0;Create;True;0;0;False;0;False;0.5,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RotatorNode;34;-2204.742,607.1228;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;1;-1274.458,39.50004;Inherit;False;Property;_power;power;6;0;Create;True;0;0;False;0;False;0.77;0.31;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;35;-1961.327,655.4274;Inherit;True;Polar Coordinates;-1;;1;7dab8e02884cf104ebefaa2e788e4162;0;4;1;FLOAT2;0,0;False;2;FLOAT2;0.5,0.5;False;3;FLOAT;0.5;False;4;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FresnelNode;5;-981.9887,63.71713;Inherit;False;Standard;WorldNormal;ViewDir;False;False;5;0;FLOAT3;0,0,1;False;4;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;5;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;37;-1822.051,1090.049;Inherit;False;Property;_AlphaBlur;AlphaBlur;9;0;Create;True;0;0;False;0;False;0.44;0.44;0.44;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;38;-1927.65,940.8784;Inherit;False;Property;_AlphaLevel;AlphaLevel;8;0;Create;True;0;0;False;0;False;0.3;0.3;0;0.5;0;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;36;-1667.156,687.376;Inherit;False;FLOAT2;1;0;FLOAT2;0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.ColorNode;6;-1036.399,-182.4641;Inherit;False;Property;_handglow_color;handglow_color;2;1;[HDR];Create;True;0;0;False;0;False;0.1137255,0.2588235,0.7490196,0;0.360376,1.50457,2.297397,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;11;-881.2112,295.0575;Inherit;False;Property;_opacity;opacity;7;0;Create;True;0;0;False;0;False;0.6;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;2;-948.9506,583.9514;Inherit;False;Property;_linecolor;linecolor;3;1;[HDR];Create;True;0;0;False;0;False;0.02731139,0.4233265,1.304119,0;0.01378214,0.3410338,0.9739395,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;9;-651.3372,31.55554;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SmoothstepOpNode;39;-1349.154,613.4831;Inherit;True;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;4;-1059.368,801.1543;Inherit;False;Property;_line_opacity;line_opacity;5;0;Create;True;0;0;False;0;False;0.5;0.5;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;40;-643.791,784.3417;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;15;-504.4844,162.7557;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;10;-637.3572,-311.4575;Inherit;False;Property;_hand_color;hand_color;1;0;Create;True;0;0;False;0;False;0.2078431,0.4980392,0.8980392,0;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ComponentMaskNode;7;-648.8818,626.1487;Inherit;False;True;True;True;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;3;-552.1449,1023.464;Inherit;False;Property;_OutlineThickness;OutlineThickness;4;0;Create;True;0;0;False;0;False;0.001;0.001;0;0.01;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;21;-257.7149,287.2043;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;14;-314.7592,-106.4899;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.OutlineNode;8;-393.6221,678.4123;Inherit;False;0;True;Transparent;0;0;Front;3;0;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;0,0;Float;False;True;-1;2;ASEMaterialInspector;0;0;Unlit;hand_bluefilloutlineB;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;True;0;Custom;0.5;True;False;0;True;Opaque;;Transparent;All;14;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;False;2;5;False;-1;10;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;0;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;True;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;30;0;29;0
WireConnection;34;0;32;0
WireConnection;34;1;31;0
WireConnection;34;2;30;0
WireConnection;35;1;34;0
WireConnection;35;2;33;0
WireConnection;5;3;1;0
WireConnection;36;0;35;0
WireConnection;9;0;6;0
WireConnection;9;1;5;0
WireConnection;39;0;36;0
WireConnection;39;1;38;0
WireConnection;39;2;37;0
WireConnection;40;0;4;0
WireConnection;40;1;39;0
WireConnection;15;0;9;0
WireConnection;15;1;11;0
WireConnection;7;0;2;0
WireConnection;21;0;15;0
WireConnection;21;1;39;0
WireConnection;14;0;10;0
WireConnection;14;1;9;0
WireConnection;8;0;7;0
WireConnection;8;2;40;0
WireConnection;8;1;3;0
WireConnection;0;2;14;0
WireConnection;0;9;21;0
WireConnection;0;11;8;0
ASEEND*/
//CHKSM=C7ABBC6714ECCA5C971CD63BD63920B9FEDB3798