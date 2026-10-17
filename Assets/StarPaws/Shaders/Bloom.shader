Shader "Hidden/StarPaws/Bloom" {
 Properties { _MainTex ("Texture", 2D) = "white" {} }
 CGINCLUDE
 #include "UnityCG.cginc"
 sampler2D _MainTex, _GlowTex; float4 _MainTex_TexelSize; float2 _Direction;
 half4 bright(v2f_img i):SV_Target { half3 c=tex2D(_MainTex,i.uv).rgb; return half4(c*saturate((max(c.r,max(c.g,c.b))-0.8)/max(max(c.r,max(c.g,c.b)),0.001)),1); }
 half4 blur(v2f_img i):SV_Target { float2 d=_MainTex_TexelSize.xy*_Direction; return tex2D(_MainTex,i.uv)*0.227027+(tex2D(_MainTex,i.uv+d*1.384615)+tex2D(_MainTex,i.uv-d*1.384615))*0.316216+(tex2D(_MainTex,i.uv+d*3.230769)+tex2D(_MainTex,i.uv-d*3.230769))*0.070270; }
 half4 composite(v2f_img i):SV_Target { half3 c=tex2D(_MainTex,i.uv).rgb+tex2D(_GlowTex,i.uv).rgb*0.6; float2 p=i.uv*2-1; c*=1-dot(p,p)*0.075; return half4(c,1); }
 ENDCG
 SubShader { Cull Off ZWrite Off ZTest Always
 Pass { CGPROGRAM
 #pragma vertex vert_img
 #pragma fragment bright
 ENDCG }
 Pass { CGPROGRAM
 #pragma vertex vert_img
 #pragma fragment blur
 ENDCG }
 Pass { CGPROGRAM
 #pragma vertex vert_img
 #pragma fragment composite
 ENDCG }
 }
}
