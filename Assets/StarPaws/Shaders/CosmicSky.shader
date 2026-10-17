Shader "StarPaws/CosmicSky" {
 Properties { _Tint ("Tint", Color) = (0.02,0.03,0.1,1) }
 SubShader { Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" } Cull Off ZWrite Off
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct v2f { float4 pos:SV_POSITION; float3 dir:TEXCOORD0; };
 v2f vert(float4 vertex:POSITION) { v2f o; o.pos=UnityObjectToClipPos(vertex); o.dir=vertex.xyz; return o; }
 float hash(float3 p) { p=frac(p*0.3183099+0.1); p*=17; return frac(p.x*p.y*p.z*(p.x+p.y+p.z)); }
 float noise(float3 p) { float3 i=floor(p), f=frac(p); f=f*f*(3-2*f); return lerp(lerp(lerp(hash(i),hash(i+float3(1,0,0)),f.x),lerp(hash(i+float3(0,1,0)),hash(i+float3(1,1,0)),f.x),f.y),lerp(lerp(hash(i+float3(0,0,1)),hash(i+float3(1,0,1)),f.x),lerp(hash(i+float3(0,1,1)),hash(i+1),f.x),f.y),f.z); }
 fixed4 frag(v2f i):SV_Target {
 float3 d=normalize(i.dir); float n=noise(d*3.8)*0.6+noise(d*8)*0.28+noise(d*18)*0.12;
 float band=pow(saturate(1-abs(d.y-d.x*0.36-0.12)*1.85),3);
 float3 col=lerp(float3(0.018,0.028,0.075),float3(0.07,0.035,0.16),saturate(d.y+0.45));
 col+=band*pow(n,2)*float3(0.16,0.065,0.24);
 col+=pow(saturate(1-length(d.xy-float2(-0.4,0.1))*1.4),4)*float3(0.015,0.06,0.08);
 return float4(col*0.55,1);
 }
 ENDCG } }
}
