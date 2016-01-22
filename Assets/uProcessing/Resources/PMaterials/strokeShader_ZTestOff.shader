Shader "Custom/uProcessing/strokeShader_ZTestOff" {
    Properties {
        _Color ("Color", Color) = (1,1,1,1)
    }

    SubShader {
		Tags {"Queue"="Transparent" "RenderType"="Transparent" }
		BindChannels { Bind "Color", color }
        Pass {
	        Blend SrcAlpha OneMinusSrcAlpha
	        Color[_Color] Lighting Off ZWrite Off ZTest Always Cull Off Fog { Mode Off }
        }
    }
    
	Fallback "Transparent/VertexLit"
} 