Shader "Toony Colors Pro 2/Legacy/Mobile_VF_OutLine" {
	Properties {
		[Describe(13)] _JustTip ("开启云点后需要C#设置
        (1)pointMaterial.SetBuffer(_Positions, positionBuffer);
            pointMaterial.SetMatrix(_LocalToWorld, Trans.localToWorldMatrix);
       
        (2)Graphics.DrawMeshInstancedIndirect(
            pointMesh,
            0,//Mesh文件中的第几个网格
            pointMaterial,
            new Bounds(Trans.position, Vector3.one * 1f),//这个Bounds内可见
            argsBuffer,//
            camera: Camera.main
        );
        
        (3) argsBuffer参数args: [indexCountPerInstance, instanceCount, startIndex, baseVertex, startInstance]
        uint[] args = new uint[5] {
            pointMesh.GetIndexCount(0),//搞不懂
            (uint)instanceCount,//所有点的数目
            pointMesh.GetIndexStart(0),//搞不懂
            pointMesh.GetBaseVertex(0),//搞不懂
            0//搞不懂
        };
        argsBuffer = new ComputeBuffer(1, sizeof(uint) * args.Length, ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(args);//每次修改args后都要执行SetData
        ", Range(1, 1)) = 1
		_Color ("Color", Vector) = (1,1,1,1)
		_HColor ("Highlight Color", Vector) = (0.785,0.785,0.785,1)
		_SColor ("Shadow Color", Vector) = (0.195,0.195,0.195,1)
		_MainTex ("Main Texture", 2D) = "white" {}
		_RampThreshold ("Ramp Threshold", Range(0, 1)) = 0.5
		_RampSmooth ("Ramp Smoothing", Range(0, 1)) = 0.01
		_SpecularColor ("Specular Color", Vector) = (1,1,1,1)
		_SpecIntensity ("Specular Intensity", Range(0, 2)) = 1
		_SpecSmooth ("Specular Smooth", Range(0, 1)) = 0
		_SpecThreshold ("Specular Threshold", Range(0, 1)) = 0.95
		_Shininess ("Shininess", Range(0.01, 1)) = 0
		[Toggle(_UseNormalMap)] _UseNormalMap ("Use Normal Map", Float) = 0
		_BumpMap ("Normal Map", 2D) = "bump" {}
		_BumpScale ("Normal Intensity", Range(0, 10)) = 1
		[Toggle(_UseFresnel)] _UseFresnel ("Use Fresnel", Float) = 0
		_F0 ("Fresnel F0 (RGB)", Vector) = (0.04,0.04,0.04,1)
		_FresnelStrength ("Fresnel Strength", Range(0, 5)) = 3
		[TipMes(2)] _FresnelBias ("Fresnel Bias (Add)[小加法偏置（适合美术调风格]", Range(0, 1)) = 0
		_FresnelPower ("Fresnel Power (override, 5=Schlick)", Range(1, 8)) = 5
		[Toggle(_UseTransparent)] _UseTransparent ("UseTransparent", Float) = 0
		_Transparent ("Transparent", Range(0, 1)) = 0.5
		_CutOff ("Cut Off", Range(0, 1)) = 0.5
		[Toggle(_UseSpecular)] _UseSpecular ("Use Specular", Float) = 0
		[Toggle(_UseBestRamp)] _UseBestRamp ("Use BestRamp", Float) = 0
		[Toggle(_UseBurn)] _UseBurn ("Use Burn", Float) = 0
		_BurnAmount ("当前消融进度", Range(-0.1, 1)) = 0.2
		_LineWidth ("消融颜色的宽度", Range(0, 0.2)) = 0.1
		_BurnFirstColor ("第一消融颜色", Vector) = (1,0,0,1)
		_BurnSecondColor ("第二消融颜色", Vector) = (1,0,0,1)
		_BurnMap ("消融纹理", 2D) = "white" {}
		[Toggle(_USE_OUTLINE)] _USE_OUTLINE ("Use OutLine", Float) = 0
		[TipMes(1)] _Outline_Scale ("基于法线的描边[外描边需要你选择最适合的实现方案]", Float) = 0.03
		[TipMes(1)] _Z_Offset ("视野方向偏移[W分量控制Scale,XYZ控制位置]", Vector) = (0,0,0,0)
		_OutlineColor ("Outline Color", Vector) = (0,0,0,1)
		[Toggle(_USEEMISSION)] _USEEMISSION ("Use Emission", Float) = 0
		[HDR] _EmissionColor ("Emission Color", Vector) = (0,0,0,1)
		_EmissionMap ("Emission Map", 2D) = "white" {}
		_EmissionIntensity ("Emission Intensity", Range(0, 1)) = 1
		[Toggle(_EMISSIONBREATHE)] _EMISSIONBREATHE ("Emission Breathe Effect", Float) = 0
		_BreatheSpeed ("Breathe Speed", Range(0.1, 10)) = 5
		_BreatheIntensity ("Breathe Intensity", Range(0, 1)) = 0.8
		[Toggle(_FLASH)] _FLASH ("Use Flash", Float) = 0
		_FlashColor ("闪烁颜色", Vector) = (1,1,1,1)
		[IntSlider] _FlashFrequency ("闪烁频率", Range(1, 10)) = 3
		_FlashProcess ("闪烁过程值", Range(0, 1)) = 0
		[Toggle(_FLASH_Type2)] _FLASH_Type2 ("外边框闪烁", Float) = 1
		[Toggle(_FlashReversal)] _FlashReversal ("闪烁反转", Float) = 0
		_FlashOffset ("闪烁偏移值", Range(0.5, 10)) = 1
		[Toggle(_TransparentWrightDepth)] _TransparentWrightDepth ("透明物体要不要写入深度", Float) = 1
		[Toggle(_Clip_Lenght)] _Clip_Lenght ("长度剔除", Float) = 0
		_CenterLenght ("显示中心点百分比", Range(0, 1)) = 1
		_CenterShowLenght ("显示的长度百分比", Range(0, 1)) = 1
		[Toggle(_Use_UVEdgeColor)] _Use_UVEdgeColor ("基于uv坐标的边缘色", Float) = 0
		_UV_EdgeColor ("UV边缘光", Vector) = (1,1,1,1)
		_UV_EdgeRange ("uv边缘光显示的范围", Range(0, 0.5)) = 0.1
		[Toggle(_QuadCalNormal)] _QuadCalNormal ("面片上模型法线（特供版）", Float) = 0
		[Toggle(_FLASH_AlphaOverrider)] _FLASH_AlphaOverrider ("闪烁自己控制透明度", Float) = 0
		[Toggle(_USEEMISSION_AlphaOverrider)] _USEEMISSION_AlphaOverrider ("自发光自己控制透明度", Float) = 0
		[Toggle(_FLASH_Auto)] _FLASH_Auto ("闪烁_自动化", Float) = 0
		[Toggle(_Outline_UseClipOffset)] _Outline_UseClipOffset ("裁切空间Z进行偏移", Float) = 0
		_Outline_ClipOffset ("裁切空间Z进行偏移", Range(-0.1, 0)) = -0.01
		[Toggle(_Specular_Reversal)] _Specular_Reversal ("反转高光", Float) = 0
		_ReversalSpecularColor ("Reversal SpecularColor", Vector) = (1,1,1,1)
		_ReversalSpecIntensity ("Reversal SpecIntensity", Range(0, 0.5)) = 0.02
		_ReversalSpecSmooth ("Reversal SpecSmooth", Range(0, 1)) = 0
		_ReversalSpecFade ("Reversal SpecFade", Float) = 1
		[Toggle(_UV_EdgeColor_ByX)] _UV_EdgeColor_ByX ("uv边缘光基于X?", Float) = 0
		_QuadCalNormalYScale ("面片上模型法线YScale", Float) = 1
		[Toggle(_Use_CartoonSpec)] _Use_CartoonSpec ("使用卡通高光", Float) = 1
		[Toggle(_Use_SimpleSpec)] _Use_SimpleSpec ("最省性能的高光", Float) = 0
		[Toggle(_Use_SimpleStandardSpec)] _Use_SimpleStandardSpec ("使用简易Unity标准高光", Float) = 0
		[Toggle(_Use_UnityStandardSpec)] _Use_UnityStandardSpec ("使用Unity标准高光", Float) = 0
		[Toggle(_Use_PosComputerBuffer)] _Use_PosComputerBuffer ("开启支持ComputerBuffer", Float) = 0
		[Toggle(_Use_LocalPosComputerBuffer)] _Use_LocalPosComputerBuffer ("本地坐标坐标ComputerBuffer", Float) = 0
		[Toggle(_Use_WorldPosComputerBuffer)] _Use_WorldPosComputerBuffer ("世界坐标ComputerBuffer", Float) = 0
		[Toggle(_Use_ReceiveShadow)] _Use_ReceiveShadow ("接收阴影", Float) = 0
		[Toggle(_Use_TexClip)] _Use_TexClip ("纹理控制剔除", Float) = 0
		[Toggle(_Use_TexClip_InMesh)] _Use_TexClip_InMesh ("在Mesh上运行", Float) = 1
		[Toggle(_Use_TexClip_InUV)] _Use_TexClip_InUV ("在UV上运行", Float) = 0
		[Toggle(_Use_FourDirectionLine)] _Use_FourDirectionLine ("四个方向的线", Float) = 0
		_TopLine ("_ForLineTex", 2D) = "white" {}
		[Toggle(_Use_TexClip_ColorOverride)] _Use_TexClip_ColorOverride ("纹理控制剔除_颜色覆盖使用", Float) = 0
		[Toggle(_Use_SpecularHighLight)] _Use_SpecularHighLight ("高光纹理", Float) = 0
		_SpecularHighLight ("_SpecularHighLight", 2D) = "white" {}
		[Toggle(_Use_FixedValueShadow)] _Use_FixedValueShadow ("硬性阴影值", Float) = 1
		[Toggle(_Use_PureColor)] _Use_PureColor ("纯色", Float) = 0
		[Toggle(_UseBurn_ByUV)] _UseBurn_ByUV ("UV贴图采样消融", Float) = 1
		[Toggle(_UseBurn_ByUV2_GridLayout)] _UseBurn_ByUV_y ("uv2网格化消融", Float) = 0
		[Toggle(_UseBurn_ByUV2_y)] _UseBurn_ByUV2_y ("uv2_Y进行消融", Float) = 0
		[MustInt] _GridLayoutSize ("网格化边数", Float) = 10
		_YBurnWidth ("Y平面消融宽度", Range(0, 1)) = 0.15
		_YBurnFixValue ("过度预期的消融进度", Range(0, 1)) = 0.5
		[Enum(UnityEngine.Rendering.CullMode)] _CullMode ("Cull", Float) = 2
		[Toggle(_Use_BlinnPhong)] _Use_BlinnPhong ("使用BlinnPhong高光", Float) = 0
		[Toggle(_Use_ViewSpec)] _Use_ViewSpec ("使用视角高光", Float) = 0
		[Toggle(_Use_AlphaIsSpec)] _Use_AlphaIsSpec ("用高光值来作为透明度", Float) = 0
	}
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType"="Opaque" }
		LOD 200
		CGPROGRAM
#pragma surface surf Standard
#pragma target 3.0

		sampler2D _MainTex;
		fixed4 _Color;
		struct Input
		{
			float2 uv_MainTex;
		};
		
		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = c.rgb;
			o.Alpha = c.a;
		}
		ENDCG
	}
	Fallback "Diffuse"
	//CustomEditor "TCP2_Mobile_VF_Inspector"
}