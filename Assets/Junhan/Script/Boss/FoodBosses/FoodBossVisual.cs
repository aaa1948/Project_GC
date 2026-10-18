using UnityEngine;
namespace Vampire
{
    public sealed class FoodBossVisual:MonoBehaviour
    {
        public SpriteRenderer Body {get;private set;}
        public float Lift,Roll,Recoil;public bool Airborne,Charge,Recovering,ClawsActive;
        public Vector2 LeftClaw,RightClaw;public float ClawAngleLeft,ClawAngleRight;
        FoodBossRuntime boss;SpriteRenderer[] claws,arms;SpriteRenderer shadow;Transform rig;float clock;int facing=-1;
        public void Initialize(FoodBossRuntime owner)
        {
            boss=owner;rig=new GameObject("Fixed-size art rig").transform;rig.SetParent(transform,false);
            Body=new GameObject("Body").AddComponent<SpriteRenderer>();Body.transform.SetParent(rig,false);Body.sortingOrder=510;
            shadow=new GameObject("Ground shadow").AddComponent<SpriteRenderer>();shadow.transform.SetParent(transform,false);shadow.sprite=BombCircleArt.Sprite;shadow.color=new Color(0,0,0,.22f);shadow.sortingOrder=479;
            claws=new[]{FoodBossArt.Make(transform,"Claw1",1.35f,512),FoodBossArt.Make(transform,"Claw1",1.1f,509)};
            foreach(var c in claws)c.enabled=false;
            arms=new SpriteRenderer[4];for(int i=0;i<4;i++)arms[i]=FoodBossArt.Make(transform,"CrabArm",.8f,510);
            ResetPose();Sample();
        }
        public void Face(Vector2 dir){if(Mathf.Abs(dir.x)>.05f)facing=dir.x<0?-1:1;}
        public void ResetPose(){Lift=Roll=Recoil=0;Airborne=Charge=Recovering=ClawsActive=false;LeftClaw=RightClaw=Vector2.zero;if(Body!=null){Body.color=Color.white;Body.enabled=true;}}
        void LateUpdate(){if(boss==null||FoodBossRuntime.Paused)return;clock+=Time.deltaTime;Sample();}
        void Sample()
        {
            if(boss==null)return;bool p=boss.PhaseTwo;float width=FoodBossArt.Widths[(int)boss.Kind];
            int frame=boss.Moving||Charge?Mathf.FloorToInt(clock*(Charge?16:8))%8:0;
            shadow.enabled=!Airborne;shadow.transform.localScale=new Vector3(width*.72f, .28f,1)/Mathf.Max(.01f,shadow.sprite.bounds.size.x);
            Body.sprite=FoodBossArt.Walk(boss.Kind,p,frame);Body.transform.localScale=Vector3.one*FoodBossArt.WalkScale(boss.Kind,p);
            Body.transform.localPosition=Vector3.zero;Body.transform.localRotation=Quaternion.identity;
            string pose=null;float poseWidth=width;
            if(boss.Kind==FoodBossKind.Denikani&&(ClawsActive||Recovering)) {pose=Recovering?"CrabRecovery":"CrabBody"+(p?2:1);poseWidth=width*.81f;}
            if(boss.Kind==FoodBossKind.Maribi&&Recovering){pose="ScallopClosed";}
            if(boss.Kind==FoodBossKind.Tangdengi){if(Recovering){pose="Pupa";poseWidth=width*1.15f;}else if(Charge){pose="BeetleFlight"+(p?2:1);poseWidth=width*(p?1.27f:1.12f);}}
            if(boss.Kind==FoodBossKind.Hurumi&&Airborne){pose="UdonAir"+(p?2:1);poseWidth=width*(p?1.07f:1);}
            if(pose!=null){Body.sprite=FoodBossArt.Get(pose);if(Body.sprite!=null){Body.transform.localScale=Vector3.one*(poseWidth/Body.sprite.bounds.size.x);Body.transform.localPosition=Vector3.up*(Body.sprite.bounds.size.y*Body.transform.localScale.x*.5f);}}
            float hop=boss.Kind==FoodBossKind.Maribi&&boss.Moving&&!Airborne?Mathf.Abs(Mathf.Sin(clock*5))*.22f:0;
            rig.localPosition=Vector3.up*(Lift+hop)+Vector3.right*(Recoil*facing);
            rig.localScale=new Vector3(-facing,1,1);
            if(Recovering&&boss.Kind==FoodBossKind.Denikani)Body.transform.localRotation=Quaternion.Euler(0,0,Roll);
            for(int i=0;i<2;i++)
            {
                var c=claws[i];c.enabled=ClawsActive&&boss.Kind==FoodBossKind.Denikani;
                c.sprite=FoodBossArt.Get((Charge?"Claw":"ClawOpen")+(p&&i==0?2:1));
                if(c.sprite!=null)c.transform.localScale=Vector3.one*((i==0&&p?1.95f:1.35f)/c.sprite.bounds.size.x);
                c.transform.localPosition=(Vector3)(i==0?LeftClaw:RightClaw);c.transform.localRotation=Quaternion.Euler(0,0,i==0?ClawAngleLeft:ClawAngleRight);
                Vector2 start=(Vector2)transform.position+new Vector2(i==0?-.55f:.55f,.9f),end=c.transform.position;
                Vector2 elbow=Vector2.Lerp(start,end,.55f)+Vector2.down*.24f;
                for(int j=0;j<2;j++){var arm=arms[i*2+j];arm.enabled=c.enabled;if(arm.sprite==null)continue;Vector2 a=j==0?start:elbow,b=j==0?elbow:end,delta=b-a;arm.transform.position=(a+b)*.5f;arm.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);arm.transform.localScale=new Vector3((delta.magnitude+.18f)/arm.sprite.bounds.size.x,.38f/arm.sprite.bounds.size.y,1);}
            }
        }
    }
}
