// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "hand_blueGrafillOLAlphaB"
{
	Properties
	{
		[HDR]_Emission("Emission", Color) = (1.077491,1.077491,1.077491,0)
		[HDR]_line_emission("line_emission", Color) = (1.059274,1.059274,1.059274,0)
		_Opacity("Opacity", Range( 0 , 1)) = 0.5
		_line_opacity("line_opacity", Range( 0 , 1)) = 0.5
		_OutlineThickness("OutlineThickness", Range( 0 , 0.01)) = 0.001
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
		uniform float4 _line_emission;
		uniform float _line_opacity;
		uniform float _AlphaLevel;
		uniform float _AlphaBlur;
		
		struct Gradient
		{
			int type;
			int colorsLength;
			int alphasLength;
			float4 colors[8];
			float2 alphas[8];
		};


		Gradient NewGradient(int type, int colorsLength, int alphasLength, 
		float4 colors0, float4 colors1, float4 colors2, float4 colors3, float4 colors4, float4 colors5, float4 colors6, float4 colors7,
		float2 alphas0, float2 alphas1, float2 alphas2, float2 alphas3, float2 alphas4, float2 alphas5, float2 alphas6, float2 alphas7)
		{
			Gradient g;
			g.type = type;
			g.colorsLength = colorsLength;
			g.alphasLength = alphasLength;
			g.colors[ 0 ] = colors0;
			g.colors[ 1 ] = colors1;
			g.colors[ 2 ] = colors2;
			g.colors[ 3 ] = colors3;
			g.colors[ 4 ] = colors4;
			g.colors[ 5 ] = colors5;
			g.colors[ 6 ] = colors6;
			g.colors[ 7 ] = colors7;
			g.alphas[ 0 ] = alphas0;
			g.alphas[ 1 ] = alphas1;
			g.alphas[ 2 ] = alphas2;
			g.alphas[ 3 ] = alphas3;
			g.alphas[ 4 ] = alphas4;
			g.alphas[ 5 ] = alphas5;
			g.alphas[ 6 ] = alphas6;
			g.alphas[ 7 ] = alphas7;
			return g;
		}


		float4 SampleGradient( Gradient gradient, float time )
		{
			float3 color = gradient.colors[0].rgb;
			UNITY_UNROLL
			for (int c = 1; c < 8; c++)
			{
			float colorPos = saturate((time - gradient.colors[c-1].w) / (gradient.colors[c].w - gradient.colors[c-1].w)) * step(c, (float)gradient.colorsLength-1);
			color = lerp(color, gradient.colors[c].rgb, lerp(colorPos, step(0.01, colorPos), gradient.type));
			}
			#ifndef UNITY_COLORSPACE_GAMMA
			color = half3(GammaToLinearSpaceExact(color.r), GammaToLinearSpaceExact(color.g), GammaToLinearSpaceExact(color.b));
			#endif
			float alpha = gradient.alphas[0].x;
			UNITY_UNROLL
			for (int a = 1; a < 8; a++)
			{
			float alphaPos = saturate((time - gradient.alphas[a-1].y) / (gradient.alphas[a].y - gradient.alphas[a-1].y)) * step(a, (float)gradient.alphasLength-1);
			alpha = lerp(alpha, gradient.alphas[a].x, lerp(alphaPos, step(0.01, alphaPos), gradient.type));
			}
			return float4(color, alpha);
		}


		void outlineVertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			float outlineVar = _OutlineThickness;
			v.vertex.xyz += ( v.normal * outlineVar );
		}
		inline half4 LightingOutline( SurfaceOutput s, half3 lightDir, half atten ) { return half4 ( 0,0,0, s.Alpha); }
		void outlineSurf( Input i, inout SurfaceOutput o )
		{
			Gradient gradient55 = NewGradient( 0, 3, 2, float4( 1, 1, 1, 0.2117647 ), float4( 1, 1, 1, 0.3852903 ), float4( 0.1043798, 0.73307, 0.9016573, 0.5735256 ), 0, 0, 0, 0, 0, float2( 1, 0 ), float2( 1, 1 ), 0, 0, 0, 0, 0, 0 );
			float2 uv3_TexCoord83 = i.uv3_texcoord3 + float2( 0,-0.08 );
			float cos85 = cos( (float)radians( 0 ) );
			float sin85 = sin( (float)radians( 0 ) );
			float2 rotator85 = mul( uv3_TexCoord83 - float2( 0.5,0.5 ) , float2x2( cos85 , -sin85 , sin85 , cos85 )) + float2( 0.5,0.5 );
			float2 CenteredUV15_g1 = ( rotator85 - float2( 0.5,0 ) );
			float2 break17_g1 = CenteredUV15_g1;
			float2 appendResult23_g1 = (float2(( length( CenteredUV15_g1 ) * 0.5 * 2.0 ) , ( atan2( break17_g1.x , break17_g1.y ) * ( 1.0 / 6.28318548202515 ) * 1.0 )));
			float smoothstepResult79 = smoothstep( _AlphaLevel , _AlphaBlur , appendResult23_g1.x);
			o.Emission = ( _line_emission * SampleGradient( gradient55, i.uv3_texcoord3.y ) ).rgb;
			o.Alpha = ( _line_opacity * smoothstepResult79 );
		}
		ENDCG
		

		Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
		Cull Back
		Blend SrcAlpha OneMinusSrcAlpha
		
		CGPROGRAM
		#pragma target 3.0
		#pragma surface surf Unlit keepalpha noshadow vertex:vertexDataFunc 
		struct Input
		{
			float2 uv3_texcoord3;
		};

		uniform float4 _Emission;
		uniform float _Opacity;
		uniform float _AlphaLevel;
		uniform float _AlphaBlur;


		struct Gradient
		{
			int type;
			int colorsLength;
			int alphasLength;
			float4 colors[8];
			float2 alphas[8];
		};


		Gradient NewGradient(int type, int colorsLength, int alphasLength, 
		float4 colors0, float4 colors1, float4 colors2, float4 colors3, float4 colors4, float4 colors5, float4 colors6, float4 colors7,
		float2 alphas0, float2 alphas1, float2 alphas2, float2 alphas3, float2 alphas4, float2 alphas5, float2 alphas6, float2 alphas7)
		{
			Gradient g;
			g.type = type;
			g.colorsLength = colorsLength;
			g.alphasLength = alphasLength;
			g.colors[ 0 ] = colors0;
			g.colors[ 1 ] = colors1;
			g.colors[ 2 ] = colors2;
			g.colors[ 3 ] = colors3;
			g.colors[ 4 ] = colors4;
			g.colors[ 5 ] = colors5;
			g.colors[ 6 ] = colors6;
			g.colors[ 7 ] = colors7;
			g.alphas[ 0 ] = alphas0;
			g.alphas[ 1 ] = alphas1;
			g.alphas[ 2 ] = alphas2;
			g.alphas[ 3 ] = alphas3;
			g.alphas[ 4 ] = alphas4;
			g.alphas[ 5 ] = alphas5;
			g.alphas[ 6 ] = alphas6;
			g.alphas[ 7 ] = alphas7;
			return g;
		}


		float4 SampleGradient( Gradient gradient, float time )
		{
			float3 color = gradient.colors[0].rgb;
			UNITY_UNROLL
			for (int c = 1; c < 8; c++)
			{
			float colorPos = saturate((time - gradient.colors[c-1].w) / (gradient.colors[c].w - gradient.colors[c-1].w)) * step(c, (float)gradient.colorsLength-1);
			color = lerp(color, gradient.colors[c].rgb, lerp(colorPos, step(0.01, colorPos), gradient.type));
			}
			#ifndef UNITY_COLORSPACE_GAMMA
			color = half3(GammaToLinearSpaceExact(color.r), GammaToLinearSpaceExact(color.g), GammaToLinearSpaceExact(color.b));
			#endif
			float alpha = gradient.alphas[0].x;
			UNITY_UNROLL
			for (int a = 1; a < 8; a++)
			{
			float alphaPos = saturate((time - gradient.alphas[a-1].y) / (gradient.alphas[a].y - gradient.alphas[a-1].y)) * step(a, (float)gradient.alphasLength-1);
			alpha = lerp(alpha, gradient.alphas[a].x, lerp(alphaPos, step(0.01, alphaPos), gradient.type));
			}
			return float4(color, alpha);
		}


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
			Gradient gradient58 = NewGradient( 0, 3, 2, float4( 1, 1, 1, 0.2117647 ), float4( 1, 1, 1, 0.3852903 ), float4( 0.1058823, 0.6903975, 0.9019608, 0.5735256 ), 0, 0, 0, 0, 0, float2( 1, 0 ), float2( 1, 1 ), 0, 0, 0, 0, 0, 0 );
			o.Emission = ( _Emission * SampleGradient( gradient58, i.uv3_texcoord3.y ) ).rgb;
			Gradient gradient60 = NewGradient( 0, 2, 2, float4( 0, 0, 0, 0 ), float4( 1, 1, 1, 1 ), 0, 0, 0, 0, 0, 0, float2( 0, 0 ), float2( 1, 1 ), 0, 0, 0, 0, 0, 0 );
			float2 uv3_TexCoord83 = i.uv3_texcoord3 + float2( 0,-0.08 );
			float cos85 = cos( (float)radians( 0 ) );
			float sin85 = sin( (float)radians( 0 ) );
			float2 rotator85 = mul( uv3_TexCoord83 - float2( 0.5,0.5 ) , float2x2( cos85 , -sin85 , sin85 , cos85 )) + float2( 0.5,0.5 );
			float2 CenteredUV15_g1 = ( rotator85 - float2( 0.5,0 ) );
			float2 break17_g1 = CenteredUV15_g1;
			float2 appendResult23_g1 = (float2(( length( CenteredUV15_g1 ) * 0.5 * 2.0 ) , ( atan2( break17_g1.x , break17_g1.y ) * ( 1.0 / 6.28318548202515 ) * 1.0 )));
			float smoothstepResult79 = smoothstep( _AlphaLevel , _AlphaBlur , appendResult23_g1.x);
			o.Alpha = ( SampleGradient( gradient60, i.uv3_texcoord3.y ) * _Opacity * smoothstepResult79 ).r;
		}

		ENDCG
	}
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18400
342;379;1261;640;1712.93;-887.5146;1.328516;True;False
Node;AmplifyShaderEditor.IntNode;91;-1706.307,681.7248;Inherit;False;Constant;_Angle;Angle;6;0;Create;True;0;0;False;0;False;0;0;0;1;INT;0
Node;AmplifyShaderEditor.RadiansOpNode;87;-1555.781,602.1027;Inherit;False;1;0;INT;0;False;1;INT;0
Node;AmplifyShaderEditor.Vector2Node;90;-1731.154,519.8516;Inherit;False;Constant;_Vector1;Vector 1;8;0;Create;True;0;0;False;0;False;0.5,0.5;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.TextureCoordinatesNode;83;-1801.803,390.1316;Inherit;False;2;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,-0.08;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RotatorNode;85;-1480.633,431.9954;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;82;-1572.435,777.4719;Inherit;False;Constant;_Vector0;Vector 0;7;0;Create;True;0;0;False;0;False;0.5,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.FunctionNode;80;-1242.218,483.2999;Inherit;True;Polar Coordinates;-1;;1;7dab8e02884cf104ebefaa2e788e4162;0;4;1;FLOAT2;0,0;False;2;FLOAT2;0.5,0.5;False;3;FLOAT;0.5;False;4;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;78;-1156.697,798.0149;Inherit;False;Property;_AlphaLevel;AlphaLevel;6;0;Create;True;0;0;False;0;False;0.3;0.3;0;0.5;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;76;-1095.31,914.9216;Inherit;False;Property;_AlphaBlur;AlphaBlur;7;0;Create;True;0;0;False;0;False;0.44;0.44;0.44;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;81;-943.0475,512.2486;Inherit;False;FLOAT2;1;0;FLOAT2;0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.TextureCoordinatesNode;56;-1671.374,1377.394;Inherit;False;2;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GradientNode;55;-1631.188,1246.841;Inherit;False;0;3;2;1,1,1,0.2117647;1,1,1,0.3852903;0.1043798,0.73307,0.9016573,0.5735256;1,0;1,1;0;1;OBJECT;0
Node;AmplifyShaderEditor.ColorNode;59;-1293.536,1146.677;Inherit;False;Property;_line_emission;line_emission;2;1;[HDR];Create;True;0;0;False;0;False;1.059274,1.059274,1.059274,0;2,2,2,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GradientNode;60;-1475.69,-20.53053;Inherit;False;0;2;2;0,0,0,0;1,1,1,1;0,0;1,1;0;1;OBJECT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;79;-632.1965,456.2326;Inherit;True;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;62;-1138.051,-258.9387;Inherit;False;2;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GradientSampleNode;57;-1352.047,1355.975;Inherit;True;2;0;OBJECT;;False;1;FLOAT;0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;65;-1306.458,1574.246;Inherit;False;Property;_line_opacity;line_opacity;4;0;Create;True;0;0;False;0;False;0.5;0.5;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.GradientNode;58;-1097.866,-389.4914;Inherit;False;0;3;2;1,1,1,0.2117647;1,1,1,0.3852903;0.1058823,0.6903975,0.9019608,0.5735256;1,0;1,1;0;1;OBJECT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;61;-1516.875,110.0218;Inherit;False;2;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;63;-868.9616,250.0318;Inherit;False;Property;_Opacity;Opacity;3;0;Create;True;0;0;False;0;False;0.5;0.5;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;68;-967.8851,1306.605;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;84;-327.2535,1001.345;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GradientSampleNode;69;-1221.191,57.66915;Inherit;True;2;0;OBJECT;;False;1;FLOAT;0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;67;-669.6264,-537.4017;Inherit;False;Property;_Emission;Emission;1;1;[HDR];Create;True;0;0;False;0;False;1.077491,1.077491,1.077491,0;2,2,2,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GradientSampleNode;66;-843.3655,-311.2914;Inherit;True;2;0;OBJECT;;False;1;FLOAT;0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;64;-460.8334,1245.242;Inherit;False;Property;_OutlineThickness;OutlineThickness;5;0;Create;True;0;0;False;0;False;0.001;0.001;0;0.01;0;1;FLOAT;0
Node;AmplifyShaderEditor.OutlineNode;70;-72.50205,780.1182;Inherit;False;0;True;Transparent;0;0;Front;3;0;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;72;-415.3831,-234.004;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;71;-370.3713,44.75506;Inherit;True;3;3;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;0,0;Float;False;True;-1;2;ASEMaterialInspector;0;0;Unlit;hand_blueGrafillOLAlphaB;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;True;0;Custom;0.5;True;False;0;True;Transparent;;Transparent;All;14;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;False;2;5;False;-1;10;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;0;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;True;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;87;0;91;0
WireConnection;85;0;83;0
WireConnection;85;1;90;0
WireConnection;85;2;87;0
WireConnection;80;1;85;0
WireConnection;80;2;82;0
WireConnection;81;0;80;0
WireConnection;79;0;81;0
WireConnection;79;1;78;0
WireConnection;79;2;76;0
WireConnection;57;0;55;0
WireConnection;57;1;56;2
WireConnection;68;0;59;0
WireConnection;68;1;57;0
WireConnection;84;0;65;0
WireConnection;84;1;79;0
WireConnection;69;0;60;0
WireConnection;69;1;61;2
WireConnection;66;0;58;0
WireConnection;66;1;62;2
WireConnection;70;0;68;0
WireConnection;70;2;84;0
WireConnection;70;1;64;0
WireConnection;72;0;67;0
WireConnection;72;1;66;0
WireConnection;71;0;69;0
WireConnection;71;1;63;0
WireConnection;71;2;79;0
WireConnection;0;2;72;0
WireConnection;0;9;71;0
WireConnection;0;11;70;0
ASEEND*/
//CHKSM=D58C979ABD5406E71E7F772ED1CFFAB06E8408B3