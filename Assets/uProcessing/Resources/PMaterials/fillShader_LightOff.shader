Shader "Custom/uProcessing/fillShader_LightOff" {
	Properties {
        _Color ("Main Color", Color) = (1,1,1,1)
        _MainTex ("Base (RGB)", 2D) = "white" {}
	}
	
    SubShader {
		Tags {"Queue"="Transparent" "RenderType"="Transparent" }
        Pass {
	        Blend SrcAlpha OneMinusSrcAlpha
	        Color[_Color]
            Lighting Off
            ZWrite On
            ZTest LEqual
            Cull Off
            Fog { Mode Off }
            SetTexture [_MainTex] {
 				Combine texture * primary, texture * primary
			}
        }
    }
    
	Fallback "Transparent/VertexLit"
}