Shader "Custom/uProcessing/fillShader" {
	Properties {
        _Color ("Main Color", Color) = (1,1,1,1)
        _SpecColor ("Spec Color", Color) = (0,0,0,0)
        _Emission ("Emmisive Color", Color) = (0,0,0,0)
        _Shininess ("Shininess", Range (0.01, 1)) = 0.0
        _MainTex ("Base (RGB)", 2D) = "white" {}
	}
	
    SubShader {
		Tags {"Queue"="Transparent" "RenderType"="Transparent" }
        Pass {
	        Blend SrcAlpha OneMinusSrcAlpha
	        //Color[_Color]
            //Lighting Off
            Material {
                Diffuse [_Color]
                Ambient [_Color]
                //Shininess [_Shininess]
                //Specular [_SpecColor]
                Emission [_Emission]
            } 
            Lighting On
            //SeparateSpecular On
            ZWrite On
            ZTest LEqual
            Cull Off
            Fog { Mode Off }
            SetTexture [_MainTex] {
 				Combine texture * primary DOUBLE, texture * primary
			}
        }
    }
    
	Fallback "Transparent/VertexLit"
}