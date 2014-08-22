Shader "Custom/uProcessing/strokeShader1" {
    Properties {
        _Color ("Color", Color) = (1,1,1,1)
    }

    SubShader {
		Tags { "RenderType"="Transparent" }
        Pass {
	        Blend SrcAlpha OneMinusSrcAlpha
	        Color[_Color]
            Lighting Off
            ZWrite On
            ZTest LEqual 
            Cull Off
            Fog { Mode Off }
        }
    }
	FallBack "Diffuse"
} 