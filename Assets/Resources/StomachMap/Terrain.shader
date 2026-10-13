Shader "Vampire/Finite Stomach Terrain"
{
    Properties
    {
        _MainTex ("Stomach layout", 2D) = "white" {}
        _DetailTex ("Fine pixel material", 2D) = "gray" {}
        _PixelSize ("World pixel size", Float) = .050844
        _DetailStrength ("Detail contrast", Range(0,1)) = 1
        _LayoutLod ("Macro smoothing", Range(0,3)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Opaque" }
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _MainTex, _DetailTex, _DistanceTex;
            float _WallWidth;
            float _PixelSize, _DetailStrength, _LayoutLod;
            float4 _MapRect;
            struct appdata { float4 vertex:POSITION; };
            struct v2f { float4 vertex:SV_POSITION; float2 world:TEXCOORD0; };
            v2f vert(appdata v)
            {
                v2f o; o.vertex=UnityObjectToClipPos(v.vertex);
                o.world=mul(unity_ObjectToWorld,v.vertex).xy; return o;
            }
            float2 mirrorUV(float2 p) { return 1-abs(frac(p*.5)*2-1); }
            fixed4 frag(v2f i):SV_Target
            {
                float2 cells=i.world/_PixelSize;
                float2 grid=floor(cells)+.5;
                float2 world=grid*_PixelSize;
                float2 uv=(world-_MapRect.xy)/_MapRect.zw;
                // Large-scale shape/color is filtered; it is NOT enlarged nearest-neighbor art.
                float3 baseColor=tex2Dlod(_MainTex,float4(uv,0,_LayoutLod)).rgb;
                // Both layers use absolute world coordinates so neighboring mesh chunks join exactly.
                float2 tile=grid/104.5;
                float3 a=tex2Dlod(_DetailTex,float4(mirrorUV(tile),0,0)).rgb;
                // Detail fades before it becomes sub-pixel, keeping the overview free of shimmer.
                float footprint=max(length(ddx(cells)),length(ddy(cells)));
                float visible=1-smoothstep(.7,2.0,footprint);
                float terrain=smoothstep(.20,.42,dot(baseColor,float3(.299,.587,.114)));
                // Preserve the approved peach/rose motif colors and density, without a second
                // rotated texture washing out or doubling the markings. Macro shading still
                // supplies the bowl/rim colors. At spawn the reference tile colors are unchanged.
                float3 referenceFloor=float3(251,151,148)/255.0;
                #ifndef UNITY_COLORSPACE_GAMMA
                    referenceFloor=GammaToLinearSpace(referenceFloor);
                #endif
                float3 detailed=a*baseColor/referenceFloor;
                float3 floorColor=saturate(lerp(baseColor,detailed,_DetailStrength*visible*terrain));
                float2 dataUV=uv*(511.0/512.0)+.5/512.0;
                float distance=tex2D(_DistanceTex,dataUV).r;
                float slope=1-smoothstep(0,_WallWidth,distance);
                // Quiet, clean rounded surface. No stretched floor motifs or fleshy mottling.
                float3 wallLow=float3(.94,.43,.51),wallHigh=float3(1,.64,.65);
                float3 outside=float3(.27,.055,.095),rimLight=float3(1,.79,.69);
                #ifndef UNITY_COLORSPACE_GAMMA
                    wallLow=GammaToLinearSpace(wallLow);wallHigh=GammaToLinearSpace(wallHigh);
                    outside=GammaToLinearSpace(outside);rimLight=GammaToLinearSpace(rimLight);
                #endif
                float3 wall=lerp(wallLow,wallHigh,smoothstep(.35,1,slope));
                float grain=frac(sin(dot(grid,float2(12.9898,78.233)))*43758.5453)-.5;
                wall+=grain*.008*visible;
                float blend=1-smoothstep(_WallWidth*.58,_WallWidth,distance);
                float3 color=lerp(floorColor,wall,blend);
                // One rounded lip, not concentric ridges. Both art and collision share the signed distance.
                float lip=exp(-pow((distance-.18)/.44,2));
                color=lerp(color,rimLight,lip*.85);
                color=lerp(outside,color,smoothstep(-.7,-.08,distance));
                // Retain the approved esophagus/outlet landmarks and tube silhouette.
                float inlet=length((uv-float2(.844,.885))/float2(.060,.105));
                float outlet=length((uv-float2(.195,.300))/float2(.067,.095));
                float tube=(1-smoothstep(.043,.058,abs(uv.x-.858)))*smoothstep(.86,.94,uv.y);
                float landmark=max(1-smoothstep(.70,1,inlet),1-smoothstep(.70,1,outlet));
                color=lerp(color,baseColor,max(landmark,tube));
                return fixed4(saturate(color),1);
            }
            ENDCG
        }
    }
}
