using UnityEngine;
using UnityEngine.UI;

namespace Vampire
{
    // Resolution-independent applique edges. Decoration never participates in UI hit testing.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class StitchPatchGraphic : MaskableGraphic
    {
        public Color accent = new Color(.91f,.48f,.43f);
        public Color paper = new Color(1,.96f,.86f);
        public bool fill, corners, stars, twinkle, button, starOnly, titleGap, hideBottom, bannerOnly;
        public float inset=7, radius=12;
        float nextFrame;
        protected override void Awake(){base.Awake();raycastTarget=false;}
        void Update()
        {
            if(twinkle && !GamePreferences.Current.reducedMotion && Time.unscaledTime>=nextFrame)
            {nextFrame=Time.unscaledTime+1f/15;SetVerticesDirty();}
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=GetPixelAdjustedRect();if(r.width<4||r.height<4)return;
            if(bannerOnly){Banner(vh,r);return;}
            if(starOnly){Star(vh,r.center,Mathf.Min(r.width,r.height)*.43f,accent);return;}
            var edge=Shrink(r,2);float round=Mathf.Min(radius,edge.height*.48f);
            if(fill)
            {
                Solid(vh,edge,round,new Color(.28f,.15f,.16f));
                Solid(vh,Shrink(edge,2),Mathf.Max(1,round-2),Color.Lerp(accent,Color.white,.55f));
                Solid(vh,Shrink(edge,4),Mathf.Max(1,round-4),button?accent:paper,true);
            }
            Ring(vh,Shrink(edge,1),round,1.25f,Color.Lerp(accent,Color.white,.38f));
            var seam=Shrink(r,Mathf.Min(inset,Mathf.Min(r.width,r.height)*.2f));
            float seamRadius=Mathf.Min(Mathf.Max(2,round-inset+2),seam.height*.45f);
            Ring(vh,Shrink(seam,-1.8f),seamRadius+1.8f,.8f,new Color(accent.r,accent.g,accent.b,.22f));
            float perimeter=2*(seam.width+seam.height-4*seamRadius)+2*Mathf.PI*seamRadius;
            int count=Mathf.Max(8,Mathf.RoundToInt(perimeter/9));float step=perimeter/count;
            Color thread=button?Color.Lerp(accent,Color.white,.85f):Color.Lerp(accent,Color.white,.12f);
            for(int i=0;i<count;i++)
            {
                Vector2 a=Along(seam,seamRadius,(i*step+.7f)/perimeter),b=Along(seam,seamRadius,(i*step+step*.62f)/perimeter);
                if(hideBottom&&a.y<r.yMin+inset*2)continue;
                if(titleGap&&a.y>r.yMax-inset*2&&a.x>Mathf.Lerp(r.x,r.xMax,.29f)&&a.x<Mathf.Lerp(r.x,r.xMax,.69f))continue;
                Line(vh,a+Vector2.down*1.2f,b+Vector2.down*1.2f,2,new Color(.37f,.16f,.13f,.18f));
                Line(vh,a,b,1.4f,thread);Line(vh,a+Vector2.up*.7f,b+Vector2.up*.7f,.45f,new Color(1,1,.93f,.8f));
            }
            if(corners)
            {
                float size=Mathf.Min(23,Mathf.Min(r.width,r.height)*.10f);
                for(int c=0;c<4;c++)
                {
                    Vector2 point=new Vector2(c%2==0?r.x+size*.68f:r.xMax-size*.68f,c<2?r.y+size*.68f:r.yMax-size*.68f);
                    var patch=new Rect(point.x-size*.66f,point.y-size*.66f,size*1.32f,size*1.32f);
                    Solid(vh,patch,5,Color.Lerp(accent,new Color(.28f,.12f,.14f),.3f));
                    Solid(vh,Shrink(patch,1.8f),4,Color.Lerp(accent,Color.white,.2f),true);
                    Ring(vh,Shrink(patch,3.5f),3,.8f,new Color(1,.95f,.80f,.85f));
                    Star(vh,point,size*.31f,Color.Lerp(accent,Color.white,.65f));
                }
            }
            if(button&&r.width>110&&r.height>34)
            {Star(vh,new Vector2(r.x+17,r.center.y),4,new Color(1,.88f,.55f));Star(vh,new Vector2(r.xMax-17,r.center.y),4,new Color(1,.88f,.55f));}
            if(stars)
            {
                for(int i=0;i<6;i++)
                {
                    float x=i%2==0?r.x+18:r.xMax-18;
                    float y=Mathf.Lerp(r.y+30,r.yMax-30,new[]{.12f,.32f,.76f}[i/2]);
                    Star(vh,new Vector2(x,y),4.5f,accent);
                    float pulse=GamePreferences.Current.reducedMotion?.25f:Mathf.Pow(Mathf.Max(0,Mathf.Sin(Time.unscaledTime*1.5f+i*1.7f)),8);
                    if(twinkle&&pulse>.05f)
                    {
                        var white=new Color(1,1,.93f,pulse*.7f);
                        Line(vh,new Vector2(x-3,y+3),new Vector2(x+3,y+3),.9f,white);
                        Line(vh,new Vector2(x,y),new Vector2(x,y+6),.9f,white);
                    }
                }
            }
        }
        static void Banner(VertexHelper vh,Rect r)
        {
            // Two arched seams follow the existing ribbon silhouette without repainting its art.
            for(int row=0;row<2;row++)for(int i=0;i<36;i++)
            {
                Vector2[] ends=new Vector2[2];
                for(int j=0;j<2;j++)
                {
                    float u=(i+(j==0?.12f:.70f))/36;
                    float y=(row==0?.86f:.42f)-(row==0?.14f:.12f)*Mathf.Pow(u*2-1,2);
                    ends[j]=new Vector2(r.x+r.width*Mathf.Lerp(.17f,.83f,u),r.y+r.height*y);
                }
                Line(vh,ends[0]+Vector2.down,ends[1]+Vector2.down,2.3f,new Color(.56f,.16f,.20f,.6f));
                Line(vh,ends[0],ends[1],1.5f,new Color(1,.96f,.76f));
            }
            Star(vh,new Vector2(r.x+r.width*.185f,r.y+r.height*.57f),6,new Color(1,.80f,.36f));
            Star(vh,new Vector2(r.x+r.width*.815f,r.y+r.height*.57f),6,new Color(1,.80f,.36f));
        }
        static Rect Shrink(Rect r,float n)=>new Rect(r.x+n,r.y+n,Mathf.Max(1,r.width-2*n),Mathf.Max(1,r.height-2*n));
        static Vector2 Along(Rect r,float rad,float t)
        {
            float w=r.width-2*rad,h=r.height-2*rad,arc=rad*Mathf.PI*.5f;
            float d=Mathf.Repeat(t,1)*(2*w+2*h+4*arc);
            if(d<w)return new Vector2(r.x+rad+d,r.y);d-=w;
            if(d<arc)return Arc(new Vector2(r.xMax-rad,r.y+rad),rad,-90+d/arc*90);d-=arc;
            if(d<h)return new Vector2(r.xMax,r.y+rad+d);d-=h;
            if(d<arc)return Arc(new Vector2(r.xMax-rad,r.yMax-rad),rad,d/arc*90);d-=arc;
            if(d<w)return new Vector2(r.xMax-rad-d,r.yMax);d-=w;
            if(d<arc)return Arc(new Vector2(r.x+rad,r.yMax-rad),rad,90+d/arc*90);d-=arc;
            if(d<h)return new Vector2(r.x,r.yMax-rad-d);d-=h;
            return Arc(new Vector2(r.x+rad,r.y+rad),rad,180+d/arc*90);
        }
        static Vector2 Arc(Vector2 center,float r,float a)=>center+new Vector2(Mathf.Cos(a*Mathf.Deg2Rad),Mathf.Sin(a*Mathf.Deg2Rad))*r;
        static void Solid(VertexHelper vh,Rect r,float radius,Color c,bool gloss=false)
        {
            int first=vh.currentVertCount;vh.AddVert(r.center,gloss?Color.Lerp(c,Color.white,.12f):c,Vector2.zero);
            const int n=64;for(int i=0;i<n;i++){var p=Along(r,radius,i/(float)n);var shade=gloss?Color.Lerp(Color.Lerp(c,Color.black,.06f),Color.Lerp(c,Color.white,.42f),Mathf.InverseLerp(r.y,r.yMax,p.y)):c;vh.AddVert(p,shade,Vector2.zero);}
            for(int i=0;i<n;i++)vh.AddTriangle(first,first+1+i,first+1+(i+1)%n);
        }
        void Ring(VertexHelper vh,Rect r,float radius,float thick,Color c)
        {const int n=120;for(int i=0;i<n;i++){var a=Along(r,radius,i/(float)n);if(hideBottom&&a.y<r.yMin+3)continue;if(titleGap&&a.y>r.yMax-3&&a.x>Mathf.Lerp(r.x,r.xMax,.29f)&&a.x<Mathf.Lerp(r.x,r.xMax,.69f))continue;Line(vh,a,Along(r,radius,(i+1)/(float)n),thick,c);}}
        static void Line(VertexHelper vh,Vector2 a,Vector2 b,float width,Color c)
        {
            var v=(b-a).normalized;var normal=new Vector2(-v.y,v.x)*width*.5f;int n=vh.currentVertCount;
            vh.AddVert(a-normal,c,Vector2.zero);vh.AddVert(a+normal,c,Vector2.zero);vh.AddVert(b+normal,c,Vector2.zero);vh.AddVert(b-normal,c,Vector2.zero);
            vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
        }
        static void Star(VertexHelper vh,Vector2 p,float radius,Color c)
        {
            StarFace(vh,p+Vector2.down*1.1f,radius+1,Color.Lerp(c,new Color(.35f,.20f,.17f),.28f));
            StarFace(vh,p,radius,Color.Lerp(c,Color.white,.38f));
            StarFace(vh,p+Vector2.down*.4f,radius*.68f,c);
        }
        static void StarFace(VertexHelper vh,Vector2 p,float radius,Color c)
        {
            int first=vh.currentVertCount;vh.AddVert(p,c,Vector2.zero);
            for(int i=0;i<10;i++)vh.AddVert(Arc(p,radius*(i%2==0?1:.52f),90+i*36),c,Vector2.zero);
            for(int i=0;i<10;i++)vh.AddTriangle(first,first+1+i,first+1+(i+1)%10);
        }
    }
}
