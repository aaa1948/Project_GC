using System.Collections;
using UnityEngine;
namespace Vampire
{
    public sealed partial class FoodBossRuntime
    {
        IEnumerator Recovery()
        {
            if(PhaseTwo)yield break;Recovering=true;Visual.Recovering=true;recoveryHealed=0;Reversals=0;recoveryDirection=Aim;nextReverse=0;weakpoints.Clear();
            if(Kind==FoodBossKind.Maribi||Kind==FoodBossKind.Tangdengi)hitbox.enabled=false;
            int count=Kind==FoodBossKind.Rolpangi?4:Kind==FoodBossKind.Hurumi?3:Kind==FoodBossKind.Tangdengi?3:Kind==FoodBossKind.Maribi?1:0;
            for(int i=0;i<count;i++)
            {
                Vector2 offset=Kind==FoodBossKind.Maribi?new Vector2(-.2f,.35f):Dir(i*360f/count+30)*(Kind==FoodBossKind.Rolpangi?7:Kind==FoodBossKind.Tangdengi?3.8f:1.8f);
                Vector2 point=FiniteStomachMap.SafeSpawn((Vector2)transform.position+offset,.5f);
                var node=FoodBossWeakpoint.Create(this,point,i);weakpoints.Add(node);
            }
            float pulse=0,rotation=0;
            while(!PhaseTwo&&!Dead)
            {
                if(Kind==FoodBossKind.Denikani){if(Reversals>=5)break;MoveToward((Vector2)transform.position+recoveryDirection*.38f*Time.deltaTime);rotation+=Time.deltaTime*45*(Reversals%2==0?1:-1);Visual.Roll=rotation;}
                else {weakpoints.RemoveAll(w=>w==null||w.Health<=0);if(weakpoints.Count==0)break;}
                if(Kind==FoodBossKind.Tangdengi)Visual.Lift=.45f+Mathf.Sin(Time.time*2)*.08f;
                pulse+=Time.deltaTime;if(pulse>=1){pulse=0;if(Kind!=FoodBossKind.Rolpangi)Heal(.004f*(Kind==FoodBossKind.Hurumi?weakpoints.Count:1));}
                yield return null;
            }
            Recovering=false;hitbox.enabled=true;Visual.ResetPose();Audio.Play(FoodBossSound.Break);FoodBossEffects.Burst(this,transform.position,2);yield return Wait(.8f);
        }
        public void ConsumeMini(FoodBossWeakpoint mini){if(!Recovering||mini==null)return;Heal(.02f);mini.Consume();}
    }
    public sealed class FoodBossWeakpoint:IDamageable
    {
        public float Health {get;private set;}public float MaxHealth {get;private set;}public FoodBossRuntime Owner {get;private set;}
        int index;float age;Vector2 anchor;SpriteRenderer art;LineRenderer thread;float width;
        public static FoodBossWeakpoint Create(FoodBossRuntime boss,Vector2 point,int index)
        {
            var go=new GameObject(boss.Kind+" recovery target "+index);go.transform.SetParent(boss.Effects,true);go.transform.position=point;go.layer=boss.gameObject.layer;
            var w=go.AddComponent<FoodBossWeakpoint>();w.Owner=boss;w.index=index;w.anchor=point;w.MaxHealth=w.Health=boss.Kind==FoodBossKind.Maribi?120:boss.Kind==FoodBossKind.Tangdengi?150:80;
            var rb=go.AddComponent<Rigidbody2D>();rb.bodyType=RigidbodyType2D.Kinematic;
            var col=go.AddComponent<CircleCollider2D>();col.isTrigger=true;col.radius=boss.Kind==FoodBossKind.Tangdengi?.55f:.4f;
            w.art=new GameObject("Recovery object art").AddComponent<SpriteRenderer>();w.art.transform.SetParent(go.transform,false);w.art.sortingOrder=516;
            switch(boss.Kind)
            {
                case FoodBossKind.Rolpangi:w.art.sprite=FoodBossArt.Walk(FoodBossKind.Rolpangi,false,0);w.width=1.05f;break;
                // The closed-shell sprite already contains the tongue; align the collider to it without drawing a second lemon.
                case FoodBossKind.Maribi:w.art.enabled=false;col.radius=.52f;break;
                case FoodBossKind.Tangdengi:w.art.sprite=FoodBossArt.Get("SugarMound");w.width=1.2f;w.thread=FoodBossEffects.Line(boss.Effects,new[]{point,(Vector2)boss.transform.position+Vector2.up*1.6f},.08f,new Color(1,.93f,.72f),false,506);break;
                default:w.art.sprite=FoodBossArt.Get("SushiFish");w.width=1.0f;break;
            }
            w.Scale();return w;
        }
        void Scale(){if(art.sprite!=null)art.transform.localScale=Vector3.one*(width/art.sprite.bounds.size.x);}
        void Update()
        {
            if(Owner==null||Owner.Dead||!Owner.Recovering||Owner.PhaseTwo){Destroy(gameObject);return;}if(FoodBossRuntime.Paused)return;age+=Time.deltaTime;
            if(Owner.Kind==FoodBossKind.Rolpangi)
            {
                art.sprite=FoodBossArt.Walk(FoodBossKind.Rolpangi,false,Mathf.FloorToInt(age*8)%8);Scale();Vector2 delta=Owner.Mouth-(Vector2)transform.position;art.flipX=delta.x>0;
                transform.position=Vector2.MoveTowards(transform.position,Owner.Mouth,1.15f*Time.deltaTime);if(delta.magnitude<.45f)Owner.ConsumeMini(this);
            }
            else if(Owner.Kind==FoodBossKind.Hurumi)
            {var offset=FoodBossRuntime.Dir(index*120+age*55);transform.position=Owner.transform.position+(Vector3)(new Vector2(offset.x*1.7f,offset.y*.65f+.8f));art.flipX=offset.y<0;}
            else if(Owner.Kind==FoodBossKind.Maribi)transform.position=Owner.transform.position+new Vector3(Owner.Aim.x>=0?.6f:-.6f,.5f,0);
            if(thread!=null){thread.SetPosition(0,transform.position);thread.SetPosition(1,Owner.transform.position+Vector3.up*1.6f);}
        }
        public override void TakeDamage(float damage,Vector2 knockback=default,bool isCritical=false)
        {if(FoodBossRuntime.Paused||Owner==null||!Owner.Recovering||Health<=0||damage<=0||float.IsNaN(damage)||float.IsInfinity(damage))return;Health=Mathf.Max(0,Health-damage);if(Health<=0){GetComponent<FoodBossCombatStatus>()?.Defeated();Owner.Audio.Play(FoodBossSound.Break,.7f);FoodBossEffects.Burst(Owner,transform.position,.65f);Destroy(gameObject);}else if(Owner.Kind==FoodBossKind.Tangdengi&&Health<MaxHealth*.5f){art.sprite=FoodBossArt.Get("SugarMoundCracked");Scale();}}
        public void Consume(){Health=0;Destroy(gameObject);}
        public override void Knockback(Vector2 knockback){}
        void OnDestroy(){if(thread!=null)Destroy(thread.gameObject);}
    }
}
