Shader "Custom/uProcessing/strokeShader_ZWriteOff" {
    Properties {
        _Color ("Color", Color) = (1,1,1,1)
    }

    SubShader {
		Tags {"Queue"="Transparent" "RenderType"="Transparent" }
		BindChannels { Bind "Color", color }
        Pass {
	        Blend SrcAlpha OneMinusSrcAlpha
	        Color[_Color] Lighting Off ZWrite Off ZTest Less Cull Off Fog { Mode Off }
        }
    }
    
	Fallback "Transparent/VertexLit"
} 