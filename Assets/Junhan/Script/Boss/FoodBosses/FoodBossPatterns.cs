using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Vampire
{
    public sealed partial class FoodBossRuntime
    {
        IEnumerator Dash()
        {
            if(Kind==FoodBossKind.Maribi||Kind==FoodBossKind.Hurumi){yield return Jump();yield break;}
            if(Kind==FoodBossKind.Rolpangi){yield return RollDash();yield break;}
            int count=Kind==FoodBossKind.Tangdengi&&PhaseTwo?2:1;
            for(int n=0;n<count;n++)
            {
                Vector2 from=transform.position,dir=Aim,to=FiniteStomachMap.ClampDash(from,from+dir*8,1.3f);
                var warning=FoodBossEffects.Line(Effects,new[]{from,to},1.25f,new Color(Warning.r,Warning.g,Warning.b,.38f));
                Visual.Face(dir);yield return Wait(n==0?1.1f:.8f);Destroy(warning.gameObject);Audio.Play(FoodBossSound.Dash);Visual.Charge=true;Visual.ClawsActive=Kind==FoodBossKind.Denikani;
                for(float t=0;t<.85f;t+=Time.deltaTime){MoveToward(Vector2.Lerp(from,to,t/.85f));Visual.LeftClaw=dir*1.6f+Rotate(dir,90)*Mathf.Sin(t*30)*.6f;Visual.RightClaw=dir*1.5f-Rotate(dir,90)*Mathf.Sin(t*30)*.6f;yield return null;}
                Visual.Charge=false;
                if(Kind==FoodBossKind.Tangdengi&&PhaseTwo&&n==0)
                {Audio.Play(FoodBossSound.Skid);var start=(Vector2)transform.position;var end=FiniteStomachMap.ClampDash(start,start+dir*1.4f,1.3f);FoodBossEffects.Path(this,new[]{start,end},.7f,1.5f);for(float t=0;t<.4f;t+=Time.deltaTime){MoveToward(Vector2.Lerp(start,end,Mathf.Sin(t/.4f*Mathf.PI*.5f)));yield return null;}}
            }
            if(Kind==FoodBossKind.Denikani&&PhaseTwo)
            {Vector2 dir=Aim;var points=FoodBossEffects.FanOutline(transform.position,dir,3.6f,150);var w=FoodBossEffects.Line(Effects,points,.12f,Warning);yield return Wait(.65f);Destroy(w.gameObject);yield return ClawSweep(dir,150,.45f);}
        }
        IEnumerator RollDash()
        {
            Vector2 start=transform.position,dir=Aim;var points=new List<Vector2>{start};
            if(PhaseTwo){for(int i=1;i<=32;i++){float t=i/32f;points.Add(start+Rotate(dir,t*270)*(t*3.2f));}Vector2 exit=(points[points.Count-1]-points[points.Count-2]).normalized;points.Add(points[points.Count-1]+exit*4.5f);}
            else points.Add(start+dir*8);
            for(int i=0;i<points.Count;i++)points[i]=FiniteStomachMap.ClampDash(start,points[i],1.3f);
            var w=FoodBossEffects.Line(Effects,points.ToArray(),1.3f,new Color(Warning.r,Warning.g,Warning.b,.38f));yield return Wait(1.15f);Destroy(w.gameObject);
            // Roll only the cake shell; toppings float separately, then settle back.
            Audio.Play(FoodBossSound.Dash);Visual.Body.enabled=false;var shell=new GameObject("Rolling cake").AddComponent<SpriteRenderer>();shell.transform.SetParent(Effects,false);shell.sprite=FoodBossArt.Get("RollShell"+(PhaseTwo?2:1));shell.sortingOrder=511;
            var toppings=new GameObject("Floating toppings").AddComponent<SpriteRenderer>();toppings.transform.SetParent(Effects,false);toppings.sprite=FoodBossArt.Get("RollToppings"+(PhaseTwo?2:1));toppings.sortingOrder=512;
            if(shell.sprite!=null)shell.transform.localScale=Vector3.one*(3.4f/shell.sprite.bounds.size.x);if(toppings.sprite!=null)toppings.transform.localScale=Vector3.one*(2.3f/toppings.sprite.bounds.size.x);
            float distance=0;for(int i=1;i<points.Count;i++)distance+=Vector2.Distance(points[i-1],points[i]);float progress=0;int segment=1;float segmentProgress=0;
            while(progress<distance&&segment<points.Count)
            {
                float step=8*Time.deltaTime;progress+=step;segmentProgress+=step;float len=Vector2.Distance(points[segment-1],points[segment]);
                while(segmentProgress>len&&segment<points.Count-1){segmentProgress-=len;segment++;len=Vector2.Distance(points[segment-1],points[segment]);}
                MoveToward(Vector2.Lerp(points[segment-1],points[segment],len<.001f?1:segmentProgress/len));shell.transform.position=transform.position+Vector3.up*1.5f;shell.transform.Rotate(0,0,-Time.deltaTime*800);toppings.transform.position=transform.position+Vector3.up*(3.2f+Mathf.Sin(progress*3)*.12f);yield return null;
            }
            for(float t=0;t<.35f;t+=Time.deltaTime){toppings.transform.position=transform.position+Vector3.up*Mathf.Lerp(3.2f,2.5f,t/.35f);yield return null;}
            Destroy(shell.gameObject);Destroy(toppings.gameObject);Visual.Body.enabled=true;
        }
        IEnumerator Jump()
        {
            int count=Kind==FoodBossKind.Hurumi&&PhaseTwo?2:1;
            for(int n=0;n<count;n++)
            {
                Vector2 target=FiniteStomachMap.SafeSpawn(Player.transform.position,2.2f);float radius=n==0&&count==2?1.7f:2.2f;
                var w=FoodBossEffects.Ring(this,target,radius);var shadow=FoodBossEffects.Line(Effects,FoodBossEffects.Arc(target,.7f),.7f,new Color(0,0,0,.24f),true,478);
                yield return Wait(.4f);Audio.Play(FoodBossSound.Dash);Visual.Airborne=true;hitbox.enabled=false;Vector2 start=transform.position;
                float height=Camera.main!=null?Camera.main.orthographicSize*2+8:20;
                for(float t=0;t<.4f;t+=Time.deltaTime){Visual.Lift=height*Mathf.Sin(t/.4f*Mathf.PI*.5f);yield return null;}
                Visual.Body.enabled=false;body.position=target;transform.position=target;
                yield return Wait(.55f);
                Visual.Body.enabled=true;
                for(float t=0;t<.28f;t+=Time.deltaTime){Visual.Lift=height*(1-t/.28f)*(1-t/.28f);yield return null;}
                Visual.Lift=0;Visual.Airborne=false;hitbox.enabled=true;Destroy(w.gameObject);Destroy(shadow.gameObject);
                Audio.Play(FoodBossSound.Land,n==0&&count==2?.6f:1);HitCircle(target,radius,n==0&&count==2?10:18);FoodBossEffects.Burst(this,target,radius);ToadCameraShake.Play(.18f,.10f);
                if(Kind==FoodBossKind.Maribi&&PhaseTwo){for(int i=0;i<4;i++){var direction=Dir(45+i*90);FoodBossEffects.Fan(this,target,direction,3.8f,45,1.1f,10);}yield return Wait(1.1f);}
                yield return Wait(n+1<count?.35f:.5f);
            }
        }
        void HitCircle(Vector2 at,float radius,float damage){if(Player!=null&&Vector2.Distance(Player.transform.position,at)<=radius+.18f)Player.TakeDamage(damage);}
        IEnumerator UniqueOne()
        {
            Audio.Play(FoodBossSound.Skill1);
            switch(Kind){case FoodBossKind.Rolpangi:yield return CreamSpiral();break;case FoodBossKind.Denikani:yield return Grab();break;case FoodBossKind.Maribi:yield return Seeds();break;case FoodBossKind.Tangdengi:yield return GrapeBeam();break;case FoodBossKind.Hurumi:yield return Curtain();break;}
        }
        IEnumerator UniqueTwo()
        {
            Audio.Play(FoodBossSound.Skill2);
            switch(Kind){case FoodBossKind.Rolpangi:yield return Toppings();break;case FoodBossKind.Denikani:yield return ClawWaves();break;case FoodBossKind.Maribi:yield return Juice();break;case FoodBossKind.Tangdengi:yield return StrawberrySeeds();break;case FoodBossKind.Hurumi:yield return Whips();break;}
        }
        IEnumerator CreamSpiral()
        {
            Vector2 origin=transform.position;int arms=PhaseTwo?2:1;
            for(int arm=0;arm<arms;arm++)
            {
                var paths=new List<Vector2[]>();var current=new List<Vector2>();
                for(int i=0;i<=150;i++){float t=i/150f;bool gap=PhaseTwo&&i%38>=28;if(gap){if(current.Count>1)paths.Add(current.ToArray());current.Clear();continue;}current.Add(origin+Dir(t*600+arm*180)*(1.6f+t*5));}
                if(current.Count>1)paths.Add(current.ToArray());var warnings=new List<LineRenderer>();foreach(var p in paths)warnings.Add(FoodBossEffects.Line(Effects,p,.65f,new Color(Warning.r,Warning.g,Warning.b,.35f)));
                yield return Wait(.9f);foreach(var w in warnings)Destroy(w.gameObject);
                foreach(var p in paths)FoodBossEffects.Path(this,p,.55f,6,0,true);yield return Wait(.45f);
            }
        }
        IEnumerator Toppings()
        {
            var target=(Vector2)Player.transform.position;bool strawberry=PhaseTwo||Random.value<.5f;bool berries=PhaseTwo||!strawberry;
            if(strawberry)StartCoroutine(Bomb(target,1.4f,"Strawberry",18));
            if(berries)for(int i=0;i<5;i++)StartCoroutine(Bomb(FiniteStomachMap.SafeSpawn(target+Dir(i*72)*2.8f,.5f),.65f,"Blueberry",9));
            yield return Wait(1.65f);
        }
        IEnumerator Bomb(Vector2 target,float radius,string art,float damage)
        {
            var warning=FoodBossEffects.Ring(this,target,radius);var r=new GameObject("Arcing "+art).AddComponent<SpriteRenderer>();r.transform.SetParent(Effects,false);r.sprite=FoodBossArt.Get(art);r.sortingOrder=530;if(r.sprite!=null)r.transform.localScale=Vector3.one*((radius>.8f?.8f:.38f)/r.sprite.bounds.size.x);
            Vector2 from=Mouth;for(float t=0;t<1.35f;t+=Time.deltaTime){if(Dead||Transitioning||r==null)yield break;float u=t/1.35f;r.transform.position=Vector2.Lerp(from,target,u)+Vector2.up*(Mathf.Sin(u*Mathf.PI)*3.5f);yield return null;}
            if(r==null||warning==null)yield break;Audio.Play(FoodBossSound.Land,.35f);Destroy(r.gameObject);Destroy(warning.gameObject);FoodBossEffects.Burst(this,target,radius);HitCircle(target,radius,damage);
        }
        IEnumerator Grab()
        {
            Vector2 origin=transform.position,dir=Aim,side=Rotate(dir,90);Visual.ClawsActive=true;
            Visual.Face(dir);
            Vector2[] left={origin+side*.7f,origin+dir*4+side*(PhaseTwo?2:.4f)},right={origin-side*.7f,origin+dir*4-side*(PhaseTwo?2:.4f)};
            var a=FoodBossEffects.Line(Effects,left,.7f,new Color(Warning.r,Warning.g,Warning.b,.35f));var b=FoodBossEffects.Line(Effects,right,.7f,new Color(Warning.r,Warning.g,Warning.b,.35f));
            Visual.LeftClaw=dir*1.2f+side*.6f;Visual.RightClaw=dir*1.2f-side*.6f;yield return Wait(2);Destroy(a.gameObject);Destroy(b.gameObject);
            bool caught=false;
            for(float t=0;t<.7f;t+=Time.deltaTime)
            {
                float u=Mathf.Clamp01(t/.25f),spread=PhaseTwo?Mathf.Lerp(2,0,Mathf.Clamp01((t-.25f)/.45f)):.4f;
                Visual.LeftClaw=dir*(4*u)+side*spread;Visual.RightClaw=dir*(4*u)-side*spread;
                if(Player!=null&&!Player.IsDashing&&!caught){Vector2 at=Player.transform.position;if(Vector2.Distance(at,origin+Visual.LeftClaw)<.6f||Vector2.Distance(at,origin+Visual.RightClaw)<.6f)caught=true;}
                yield return null;
            }
            if(caught&&Player!=null&&Player.IsAlive)
            {
                var slow=Player.GetComponent<SnailSlowStatus>()??Player.gameObject.AddComponent<SnailSlowStatus>();slow.Enter(GetInstanceID(),.95f);
                var rb=Player.GetComponent<Rigidbody2D>();Vector2 held=Player.transform.position,hold=origin+dir*2.2f;float nextTick=0;
                for(float t=0;t<1.05f;t+=Time.deltaTime)
                {
                    if(!Player.IsAlive||Player.IsDashing)break;
                    Vector2 at=Vector2.Lerp(held,hold,Mathf.Clamp01(t/.25f));Visual.LeftClaw=at-origin;Visual.RightClaw=at-origin+side*.35f;
                    if(rb!=null){rb.position=at;rb.velocity=Vector2.zero;}Player.transform.position=at;
                    if(t>=nextTick){Player.TakeDamage(4);nextTick+=.35f;}yield return null;
                }
                slow.Exit(GetInstanceID(),0);
                if(Player.IsAlive&&!Player.IsDashing){Vector2 from=Player.transform.position,landing=FiniteStomachMap.ClampDash(from,from+dir*1.8f,.25f);
                    for(float t=0;t<.25f;t+=Time.deltaTime){Vector2 at=Vector2.Lerp(from,landing,t/.25f)+Vector2.up*Mathf.Sin(t/.25f*Mathf.PI)*.4f;if(rb!=null)rb.position=at;Player.transform.position=at;yield return null;}
                    if(rb!=null)rb.position=landing;Player.transform.position=landing;Audio.Play(FoodBossSound.Land,.45f);FoodBossEffects.Burst(this,landing,.6f);}
            }
            yield return Wait(.4f);
        }
        IEnumerator ClawSweep(Vector2 dir,float sweep,float duration)
        {
            bool hit=false;float a=Mathf.Atan2(dir.y,dir.x)*Mathf.Rad2Deg;for(float t=0;t<duration;t+=Time.deltaTime){var p=Dir(a-sweep*.5f+sweep*t/duration)*3.2f;Visual.LeftClaw=p;if(!hit&&Vector2.Distance(Player.transform.position,(Vector2)transform.position+p)<.8f){Player.TakeDamage(14);hit=true;}yield return null;}
        }
        IEnumerator ClawWaves()
        {
            Visual.ClawsActive=true;Visual.LeftClaw=new Vector2(-1,2);Visual.RightClaw=new Vector2(1,2);
            int count=PhaseTwo?2:1;for(int n=0;n<count;n++)
            {
                Vector2 origin=transform.position;float start=PhaseTwo?(n==0?90:-90):0,sweep=PhaseTwo?180:360;
                var w=FoodBossEffects.Ring(this,origin,4.2f,start,sweep);yield return Wait(n==0?2:.9f);Destroy(w.gameObject);
                var wave=FoodBossEffects.Line(Effects,FoodBossEffects.Arc(origin,4.2f,start,sweep),.4f,Warning);wave.gameObject.AddComponent<FoodBossFade>();
                Vector2 delta=(Vector2)Player.transform.position-origin;if(delta.magnitude<=4.2f&&(sweep==360||n==0&&delta.x<=0||n==1&&delta.x>=0))Player.TakeDamage(14);
                Audio.Play(FoodBossSound.Land,.7f);ToadCameraShake.Play(.12f,.06f);yield return Wait(.4f);
            }
        }
        IEnumerator Seeds()
        {
            Vector2 origin=Mouth,dir=FireAim;float angle=PhaseTwo?85:0;
            var w=FoodBossEffects.Line(Effects,FoodBossEffects.FanOutline(origin,dir,8,PhaseTwo?100:22),.12f,Warning);yield return Wait(1);Destroy(w.gameObject);
            int count=PhaseTwo?14:10;for(int i=0;i<count;i++){Vector2 d=Rotate(dir,PhaseTwo?-angle*.5f+angle*i/(count-1):0);Vector2 from=origin+Rotate(d,90)*Random.Range(-.7f,.7f);Audio.Play(FoodBossSound.Shot,.6f);FoodBossBullet.Fire(this,from,d,2.5f,6,false,FoodBossArt.Get("LemonSeed"));Visual.Recoil=-.08f;yield return Wait(.15f);Visual.Recoil=0;}
        }
        IEnumerator Juice()
        {
            int count=PhaseTwo?2:1;for(int i=0;i<count;i++){Vector2 origin=Mouth,dir=FireAim;float reach=PhaseTwo?(i==0?7:3.7f):4.7f,angle=PhaseTwo?(i==0?28:120):70;
                var w=FoodBossEffects.Line(Effects,FoodBossEffects.FanOutline(origin,dir,reach,angle),.12f,Warning);yield return Wait(.9f);Destroy(w.gameObject);FoodBossEffects.Fan(this,origin,dir,reach,angle,1.15f,12);yield return Wait(1.35f);}
        }
        IEnumerator GrapeBeam()
        {
            Vector2 origin=Mouth,dir=FireAim;int count=PhaseTwo?3:1;var warnings=new List<LineRenderer>();
            for(int i=0;i<count;i++){Vector2 d=Rotate(dir,(i-(count-1)*.5f)*32);warnings.Add(FoodBossEffects.Line(Effects,new[]{origin,origin+d*9},.7f,new Color(Warning.r,Warning.g,Warning.b,.4f)));}
            var orb=FoodBossArt.Make(Effects,"Grape",.65f,522);orb.transform.position=origin;yield return Wait(1.3f);Destroy(orb.gameObject);foreach(var w in warnings)Destroy(w.gameObject);
            for(int i=0;i<count;i++)FoodBossEffects.Fan(this,origin,Rotate(dir,(i-(count-1)*.5f)*32),9,7,1.1f,14);yield return Wait(1.25f);
        }
        IEnumerator StrawberrySeeds()
        {
            Visual.Charge=true;var w=FoodBossEffects.Ring(this,transform.position,2);yield return Wait(1.15f);Destroy(w.gameObject);Visual.Charge=false;
            Audio.Play(FoodBossSound.Radial);for(int i=0;i<14;i++){var d=Dir(i*360f/14);FoodBossBullet.Fire(this,(Vector2)transform.position+d*1.3f,d,1.65f,7,PhaseTwo,PhaseTwo?null:FoodBossArt.Get("StrawberrySeed"));}yield return Wait(.8f);
        }
        IEnumerator Curtain()
        {
            Vector2 center=Player.transform.position,side=Rotate(Aim,90);var direction=Aim;int gap=3;
            var w=FoodBossEffects.Line(Effects,new[]{center-side*5,center+side*5},.7f,new Color(Warning.r,Warning.g,Warning.b,.35f));yield return Wait(1);Destroy(w.gameObject);
            var noodles=new LineRenderer[7];
            for(int i=0;i<7;i++){var at=center+side*((i-3)*1.45f);noodles[i]=FoodBossEffects.Line(Effects,new[]{at,at},.6f,new Color(1,.92f,.72f),false,511,true);}
            float hitAt=0;
            for(float t=0;t<4.8f;t+=Time.deltaTime)
            {
                // Gap shifts only after the incoming safe lane is already fully open.
                int nextGap=PhaseTwo?(t<1.7f?3:t<3.2f?4:2):3;float transition=PhaseTwo?(t<1.7f?0:t<3.2f?t-1.7f:t-3.2f):1;
                for(int i=0;i<7;i++)
                {
                    bool safe=i==nextGap||PhaseTwo&&transition<.6f&&i==gap;var at=center+side*((i-3)*1.45f);
                    float height=safe?0:Mathf.Min(Mathf.Clamp01(t/.35f),Mathf.Clamp01((4.8f-t)/.4f))*2.4f;
                    noodles[i].positionCount=20;for(int j=0;j<20;j++){float u=j/19f;noodles[i].SetPosition(j,at+Vector2.up*(height*u)+side*(Mathf.Sin(u*4+t*4)*.18f*u));}
                    noodles[i].enabled=height>.03f;
                    if(!safe&&height>.5f&&Time.time>=hitAt&&Vector2.Distance(Player.transform.position,at)<.65f){Player.TakeDamage(12);hitAt=Time.time+.7f;}
                }
                if(transition>=.6f)gap=nextGap;yield return null;
            }
            foreach(var r in noodles)Destroy(r.gameObject);
        }
        IEnumerator Whips()
        {
            int count=PhaseTwo?2:1;for(int n=0;n<count;n++)
            {
                Vector2 origin=(Vector2)transform.position+Vector2.up*.7f,dir=Aim;float reach=n==0?4.2f:5.8f,sweep=n==0?90:150;
                var w=FoodBossEffects.Line(Effects,FoodBossEffects.FanOutline(origin,dir,reach,sweep),.12f,Warning);yield return Wait(n==0?.85f:1.2f);Destroy(w.gameObject);
                var r=FoodBossEffects.Line(Effects,new[]{origin,origin},.48f,new Color(1,.92f,.72f),false,514,true);r.positionCount=24;bool hit=false;float baseAngle=Mathf.Atan2(dir.y,dir.x)*Mathf.Rad2Deg;
                for(float t=0;t<.55f;t+=Time.deltaTime)
                {
                    float angle=baseAngle+(n==0?-1:1)*(sweep*.5f-sweep*t/.55f);Vector2 end=origin+Dir(angle)*reach;Vector2 prev=origin;
                    for(int j=0;j<24;j++){float u=j/23f;Vector2 p=Vector2.Lerp(origin,end,u)+Rotate(dir,90)*Mathf.Sin(u*Mathf.PI)*.45f;r.SetPosition(j,p);if(!hit&&j>0&&SnailBossProjectile.SegmentDistance(Player.transform.position,prev,p)<.45f){Player.TakeDamage(14);hit=true;}prev=p;}yield return null;
                }
                Destroy(r.gameObject);yield return Wait(.3f);
            }
        }
    }
}
