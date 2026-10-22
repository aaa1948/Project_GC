using System.Collections.Generic;
using UnityEngine;
namespace Vampire
{
    // Telegraph geometry and the damage geometry share the same sampled world points.
    public static class FoodBossEffects
    {
        static Material lineMaterial;
        public static LineRenderer Line(Transform root,Vector2[] points,float width,Color color,bool loop=false,int order=480,bool liquid=false)
        {
            var r=new GameObject(liquid?"Continuous liquid ribbon":"Boss attack warning").AddComponent<LineRenderer>();r.transform.SetParent(root,true);
            if(lineMaterial==null)lineMaterial=new Material(Shader.Find("Sprites/Default"));
            r.sharedMaterial=lineMaterial;r.useWorldSpace=true;r.positionCount=points.Length;r.loop=loop;r.startWidth=r.endWidth=width;r.numCornerVertices=5;r.numCapVertices=5;r.sortingOrder=order;r.startColor=r.endColor=color;
            for(int i=0;i<points.Length;i++)r.SetPosition(i,points[i]);
            if(order==480&&!liquid)
            {
                var shader=Resources.Load<Shader>("FoodBosses/Hazard");
                if(shader!=null){var material=new Material(shader);r.sharedMaterial=material;r.gameObject.AddComponent<FoodBossMaterialLifetime>().Owned=material;}
                if(points.Length>3&&Vector2.Distance(points[0],points[points.Length-1])<.01f)FoodBossHazardFill.Add(r,points,color);
            }
            if(liquid){var shader=Resources.Load<Shader>("FoodBosses/LiquidFlow");if(shader!=null){r.material=new Material(shader);r.gameObject.AddComponent<FoodBossMaterialLifetime>().Owned=r.material;}}
            return r;
        }
        public static Vector2[] Arc(Vector2 origin,float radius,float start=0,float sweep=360,int count=64)
        {var p=new Vector2[count+1];for(int i=0;i<=count;i++)p[i]=origin+FoodBossRuntime.Dir(start+sweep*i/count)*radius;return p;}
        public static LineRenderer Ring(FoodBossRuntime b,Vector2 at,float radius,float start=0,float sweep=360)
        {
            Vector2[] p;
            if(sweep>=359)p=Arc(at,radius,start,sweep);
            else {var arc=Arc(at,radius,start,sweep);p=new Vector2[arc.Length+2];p[0]=at;System.Array.Copy(arc,0,p,1,arc.Length);p[p.Length-1]=at;}
            return Line(b.Effects,p,.10f,b.Warning);
        }
        public static void Burst(FoodBossRuntime b,Vector2 point,float radius)
        {var r=Line(b.Effects,Arc(point,radius),.11f,b.Warning,false,488);var f=r.gameObject.AddComponent<FoodBossFade>();f.Lifetime=.32f;}
        public static FoodBossZone Path(FoodBossRuntime b,Vector2[] points,float width,float seconds,float damage=0,bool cream=false)
        {
            Color c=cream?new Color(1,.9f,.7f,.95f):b.Kind==FoodBossKind.Maribi?new Color(1,.86f,.12f,.85f):b.Kind==FoodBossKind.Tangdengi?new Color(.63f,.95f,.28f,.9f):new Color(1,.9f,.7f);
            var r=Line(b.Effects,points,width,c,false,485,true);var z=r.gameObject.AddComponent<FoodBossZone>();z.Init(b,points,width*.5f,seconds,damage,cream);return z;
        }
        public static Vector2[] FanOutline(Vector2 origin,Vector2 direction,float length,float angle)
        {
            float a=Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg;var arc=Arc(origin,length,a-angle*.5f,angle,30);
            var p=new Vector2[arc.Length+2];p[0]=origin;System.Array.Copy(arc,0,p,1,arc.Length);p[p.Length-1]=origin;return p;
        }
        public static FoodBossFan Fan(FoodBossRuntime b,Vector2 origin,Vector2 direction,float length,float angle,float duration,float damage)
        {var go=new GameObject("Continuous flowing fan");go.transform.SetParent(b.Effects,false);var f=go.AddComponent<FoodBossFan>();f.Init(b,origin,direction,length,angle,duration,damage);return f;}
    }
    public sealed class FoodBossFade:MonoBehaviour
    {
        public float Lifetime=.3f;float age,alpha;void Start(){var r=GetComponent<LineRenderer>();alpha=r!=null?r.startColor.a:1;}void Update(){if(FoodBossRuntime.Paused)return;age+=Time.deltaTime;if(age>=Lifetime){Destroy(gameObject);return;}var r=GetComponent<LineRenderer>();if(r!=null){var c=r.startColor;c.a=alpha*(1-age/Lifetime);r.startColor=r.endColor=c;}foreach(var f in GetComponentsInChildren<FoodBossHazardFill>())f.Fade(1-age/Lifetime);}
    }
    public sealed class FoodBossMaterialLifetime:MonoBehaviour{public Material Owned;void OnDestroy(){if(Owned!=null)Destroy(Owned);}}
    public sealed class FoodBossZone:MonoBehaviour
    {
        FoodBossRuntime boss;Vector2[] points;float radius,left,damage,tick;bool cream;SnailSlowStatus slow;
        public void Init(FoodBossRuntime b,Vector2[] p,float r,float duration,float d,bool c){boss=b;points=p;radius=r;left=duration;damage=d;cream=c;}
        void Update()
        {
            if(boss==null||boss.Dead){Destroy(gameObject);return;}if(FoodBossRuntime.Paused)return;left-=Time.deltaTime;if(left<=0){Destroy(gameObject);return;}
            var player=boss.Player;if(player==null||!player.IsAlive)return;Vector2 at=player.transform.position;bool inside=false;
            for(int i=1;i<points.Length;i++)if(SnailBossProjectile.SegmentDistance(at,points[i-1],points[i])<radius+.18f){inside=true;break;}
            if(cream)
            {
                if(inside){slow=player.GetComponent<SnailSlowStatus>()??player.gameObject.AddComponent<SnailSlowStatus>();slow.Enter(GetInstanceID(),.3f);
                    var rb=player.GetComponent<Rigidbody2D>();if(rb!=null)rb.position=FiniteStomachMap.ClampDash(rb.position,rb.position+((Vector2)boss.transform.position-at).normalized*.38f*Time.deltaTime,.2f);}
                else if(slow!=null)slow.Exit(GetInstanceID(),0);
            }
            if(inside&&damage>0&&Time.time>=tick){player.TakeDamage(damage);tick=Time.time+.65f;}
        }
        void OnDestroy(){if(slow!=null)slow.Exit(GetInstanceID(),0);}
    }
    public sealed class FoodBossFan:MonoBehaviour
    {
        FoodBossRuntime boss;Vector2 origin,direction;float length,angle,duration,damage,age,next;Mesh mesh;Material material;
        public void Init(FoodBossRuntime b,Vector2 o,Vector2 d,float l,float a,float seconds,float hit)
        {
            boss=b;origin=o;direction=d;length=l;angle=a;duration=seconds;damage=hit;
            var filter=gameObject.AddComponent<MeshFilter>();mesh=new Mesh();filter.sharedMesh=mesh;
            var r=gameObject.AddComponent<MeshRenderer>();material=new Material(Resources.Load<Shader>("FoodBosses/LiquidFlow"));r.sharedMaterial=material;r.sortingOrder=490;
        }
        void Update()
        {
            if(boss==null||boss.Dead){Destroy(gameObject);return;}if(FoodBossRuntime.Paused)return;age+=Time.deltaTime;if(age>duration){Destroy(gameObject);return;}
            float reach=length*Mathf.Clamp01(age/.25f);const int n=48;var v=new Vector3[n+2];var uv=new Vector2[n+2];var c=new Color[n+2];var triangles=new int[n*3];
            v[0]=origin;Color tint=boss.Kind==FoodBossKind.Tangdengi?new Color(.55f,1,.25f,.95f):new Color(1,.86f,.15f,.9f);c[0]=tint;
            float start=Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg-angle*.5f;
            for(int i=0;i<=n;i++){v[i+1]=origin+FoodBossRuntime.Dir(start+angle*i/n)*reach;uv[i+1]=v[i+1];c[i+1]=tint;if(i<n){triangles[i*3]=0;triangles[i*3+1]=i+1;triangles[i*3+2]=i+2;}}
            mesh.Clear();mesh.vertices=v;mesh.uv=uv;mesh.colors=c;mesh.triangles=triangles;mesh.RecalculateBounds();
            var p=boss.Player;if(p!=null&&p.IsAlive&&Time.time>=next){Vector2 delta=(Vector2)p.transform.position-origin;if(delta.magnitude<=reach+.15f&&Vector2.Angle(direction,delta)<=angle*.5f){p.TakeDamage(damage);next=Time.time+.7f;}}
        }
        void OnDestroy(){if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material);}
    }
    public sealed class FoodBossBullet:MonoBehaviour
    {
        public Vector2 Velocity {get;private set;}public FoodBossRuntime Owner {get;private set;}public bool Sugar;
        float age,damage,travel;SpriteRenderer art;
        public static FoodBossBullet Fire(FoodBossRuntime b,Vector2 start,Vector2 direction,float speed=1.8f,float hit=8,bool sugar=false,Sprite sprite=null)
        {
            var go=new GameObject("Food boss bullet");go.transform.SetParent(b.Effects,true);go.transform.position=start;
            var p=go.AddComponent<FoodBossBullet>();p.Owner=b;p.Velocity=direction.normalized*speed;p.damage=hit;p.Sugar=sugar;
            p.art=go.AddComponent<SpriteRenderer>();p.art.sprite=sprite??(sugar?FoodBossArt.Get("CandySeed"):FoodBossArt.Projectile(b.Kind));p.art.sortingOrder=525;
            if(p.art.sprite!=null)go.transform.localScale=Vector3.one*((sugar?.5f:.28f)/p.art.sprite.bounds.size.x);
            OctoberEnemyProjectile.Register(go,true);return p;
        }
        void Update()
        {
            if(Owner==null||Owner.Dead){Destroy(gameObject);return;}if(FoodBossRuntime.Paused)return;
            age+=Time.deltaTime;if(age>10){Destroy(gameObject);return;}Vector2 before=transform.position;transform.position+=(Vector3)Velocity*Time.deltaTime;travel+=Velocity.magnitude*Time.deltaTime;
            if(Sugar&&travel>=3.3f){Owner.Audio.Play(FoodBossSound.Break,.25f);for(int i=-1;i<=1;i++)Fire(Owner,transform.position,FoodBossRuntime.Rotate(Velocity,i*25),1.9f,6,false,FoodBossArt.Get("StrawberrySeed"));Destroy(gameObject);return;}
            var p=Owner.Player;if(p!=null&&p.IsAlive&&SnailBossProjectile.SegmentDistance(p.transform.position,before,transform.position)<(Sugar?.34f:.24f)){p.TakeDamage(damage);Destroy(gameObject);}
        }
    }
}
