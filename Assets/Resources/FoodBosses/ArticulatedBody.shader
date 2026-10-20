Shader "FoodBosses/ArticulatedBody"
{
 Properties { [PerRendererData] _MainTex("Sprite",2D)="white"{} _Cell("Sprite cell",Vector)=(0,0,296,282) _Phase("Phase",Float)=0 }
 SubShader { Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" } Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _MainTex;float4 _MainTex_TexelSize;float4 _Cell;float _Phase;
 struct app {float4 vertex:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
 struct vary {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
 vary vert(app v){vary o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
 fixed4 frag(vary i):SV_Target {
  float2 p=i.uv*_MainTex_TexelSize.zw-_Cell.xy;p.y=_Cell.w-p.y;
  // Only the raised center noodle is replaced by the articulated limb. Keep bowl, feet and other noodles.
  float left=lerp(p.y<65?110:137,p.y<64?129:147,_Phase);
  float right=lerp(p.y<65?195:188,p.y<64?205:199,_Phase);
  float bottom=lerp(94,102,_Phase);
  if(p.x>left&&p.x<right&&p.y<bottom)discard;
  return tex2D(_MainTex,i.uv)*i.color;
 }
 ENDCG }
 }
}
