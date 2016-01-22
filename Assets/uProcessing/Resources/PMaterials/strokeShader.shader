Shader "Custom/uProcessing/strokeShader" {
    Properties {
        _Color ("Color", Color) = (1,1,1,1)
    }

    SubShader {
		Tags {"Queue"="Transparent" "RenderType"="Transparent" }
		BindChannels { Bind "Color", color }
		//BindChannels { Bind "vertex", vertex Bind "color", color }
        Pass {
	        Blend SrcAlpha OneMinusSrcAlpha
	        Color[_Color] Lighting Off ZWrite On ZTest Less Cull Off Fog { Mode Off }
        }
    }
    
	Fallback "Transparent/VertexLit"
} 