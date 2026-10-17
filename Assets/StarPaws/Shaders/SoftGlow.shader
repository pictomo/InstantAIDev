Shader "StarPaws/SoftGlow" {
 Properties { _Color ("Color", Color) = (1,1,1,1) }
 SubShader { Tags { "Queue"="Transparent" "RenderType"="Transparent" } Blend SrcAlpha One Cull Off ZWrite Off
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 float4 _Color;
 struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; fixed4 col:COLOR; };
 v2f vert(appdata_full v) { v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.texcoord.xy; o.col=v.color; return o; }
 fixed4 frag(v2f i):SV_Target { float r=length(i.uv*2-1); return float4(_Color.rgb*i.col.rgb, pow(saturate(1-r),3)*_Color.a*i.col.a); }
 ENDCG } }
}
