Shader "FlatKit/GradientSkybox" {
	Properties {
		_ColorTop ("Top Color", Vector) = (0.97,0.67,0.51,0)
		_ColorMid ("Mid Color", Vector) = (0,0.7,0.74,0)
		_ColorBot ("Bottom Color", Vector) = (0,0.7,0.74,0)
		[Space] _Intensity ("Intensity", Range(0, 2)) = 1
		_Exponent ("整体的混合参数", Range(0, 3)) = 1
		_ColorMix ("三种颜色的混合参数", Range(1, 3)) = 2
		[Space] _DirectionYaw ("Direction X angle", Range(0, 180)) = 0
		_DirectionPitch ("Direction Y angle", Range(0, 180)) = 0
		[HideInInspector] _Direction ("Direction", Vector) = (0,1,0,0)
	}
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType" = "Opaque" }
		LOD 200
		CGPROGRAM
#pragma surface surf Standard
#pragma target 3.0

		struct Input
		{
			float2 uv_MainTex;
		};

		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			o.Albedo = 1;
		}
		ENDCG
	}
	//CustomEditor "GradientSkyboxEditor"
}