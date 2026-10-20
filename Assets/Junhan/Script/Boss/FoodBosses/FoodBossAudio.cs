using UnityEngine;
namespace Vampire
{
    public enum FoodBossSound { Shot,Radial,Dash,Land,Skill1,Skill2,Recover,Break,Phase,Death,Skid,Beam,Consume }
    public sealed class FoodBossAudio:MonoBehaviour
    {
        AudioSource[] voices;AudioClip[] clips;float[] gains,fadeLeft,fadeLength,started;
        FoodBossSound[] playing;FoodBossRuntime boss;float lastShot=-10,lastBreak=-10,lastConsume=-10;bool paused;
        public int PlayedCount {get;private set;}
        public void Initialize(FoodBossRuntime owner)
        {
            boss=owner;voices=new AudioSource[6];gains=new float[6];fadeLeft=new float[6];fadeLength=new float[6];started=new float[6];playing=new FoodBossSound[6];
            for(int i=0;i<voices.Length;i++){var s=gameObject.AddComponent<AudioSource>();s.playOnAwake=false;s.spatialBlend=0;s.priority=80;voices[i]=s;}
            var names=System.Enum.GetNames(typeof(FoodBossSound));clips=new AudioClip[names.Length];for(int i=0;i<clips.Length;i++)clips[i]=Resources.Load<AudioClip>($"FoodBosses/Audio/{(int)boss.Kind}-{names[i]}");
        }
        public float Duration(FoodBossSound sound)=>clips!=null&&(int)sound>=0&&(int)sound<clips.Length&&clips[(int)sound]!=null?clips[(int)sound].length:0;
        public bool HasAllClips
        {
            get
            {
                if(clips==null)return false;
                for(int i=0;i<clips.Length;i++)
                    if(Required((FoodBossSound)i)&&clips[i]==null)return false;
                return true;
            }
        }
        bool Required(FoodBossSound sound)=>sound!=FoodBossSound.Beam&&sound!=FoodBossSound.Consume
            ||sound==FoodBossSound.Beam&&boss.Kind==FoodBossKind.Tangdengi
            ||sound==FoodBossSound.Consume&&boss.Kind==FoodBossKind.Rolpangi;
        static int Priority(FoodBossSound sound)=>sound==FoodBossSound.Dash||sound==FoodBossSound.Beam?3:
            sound==FoodBossSound.Shot||sound==FoodBossSound.Break||sound==FoodBossSound.Consume?1:2;
        public void Play(FoodBossSound sound,float gain=1)
        {
            if(voices==null||FoodBossRuntime.Paused)return;int id=(int)sound;if(id<0||id>=clips.Length||clips[id]==null)return;
            if(sound==FoodBossSound.Shot&&Time.time-lastShot<.075f)return;if(sound==FoodBossSound.Shot)lastShot=Time.time;
            if(sound==FoodBossSound.Break&&Time.time-lastBreak<.09f)return;if(sound==FoodBossSound.Break)lastBreak=Time.time;
            if(sound==FoodBossSound.Consume&&Time.time-lastConsume<.12f)return;if(sound==FoodBossSound.Consume)lastConsume=Time.time;
            int slot=System.Array.FindIndex(voices,s=>!s.isPlaying);
            if(slot<0)
            {
                for(int i=0;i<voices.Length;i++)
                    if(Priority(playing[i])<=Priority(sound)&&(slot<0||Priority(playing[i])<Priority(playing[slot])
                        ||Priority(playing[i])==Priority(playing[slot])&&started[i]<started[slot]))slot=i;
                if(slot<0)return;
            }
            var v=voices[slot];v.Stop();v.clip=clips[id];
            // Keep approved material and flight pitch intact. Variation is in the assets.
            v.pitch=1;playing[slot]=sound;started[slot]=Time.time;fadeLength[slot]=fadeLeft[slot]=0;
            gains[slot]=Mathf.Max(0,gain)*(sound==FoodBossSound.Shot?.35f:sound==FoodBossSound.Land?.75f:.6f);Apply(slot);v.Play();PlayedCount++;
        }
        void Apply(int i)
        {
            var cam=Camera.main;float attenuation=1,pan=0;if(cam!=null){var viewport=cam.WorldToViewportPoint(transform.position);pan=Mathf.Clamp((viewport.x-.5f)*.6f,-.4f,.4f);float d=Vector2.Distance(cam.transform.position,transform.position);attenuation=Mathf.Clamp01(1-d/24f);}
            float fade=fadeLength[i]>0?Mathf.Clamp01(fadeLeft[i]/fadeLength[i]):1;
            voices[i].volume=gains[i]*GamePreferences.Current.effects*attenuation*fade;voices[i].panStereo=pan;
        }
        void Update()
        {
            if(voices==null)return;bool stop=FoodBossRuntime.Paused;if(stop!=paused){foreach(var v in voices)if(stop)v.Pause();else v.UnPause();paused=stop;}
            for(int i=0;i<voices.Length;i++)
            {
                if(!stop&&fadeLength[i]>0)
                {
                    fadeLeft[i]=Mathf.Max(0,fadeLeft[i]-Time.deltaTime);
                    if(fadeLeft[i]<=0){voices[i].Stop();fadeLength[i]=0;gains[i]=0;}
                }
                Apply(i);
            }
        }
        public void Stop(FoodBossSound sound,float fadeSeconds=.08f)
        {
            if(voices==null)return;
            for(int i=0;i<voices.Length;i++)if(playing[i]==sound)
            {
                if(fadeSeconds<=0){voices[i].Stop();fadeLength[i]=fadeLeft[i]=0;gains[i]=0;}
                else {fadeLength[i]=fadeLeft[i]=fadeSeconds;}
            }
        }
        public void Stop(){if(voices!=null)for(int i=0;i<voices.Length;i++){voices[i].Stop();fadeLength[i]=fadeLeft[i]=0;gains[i]=0;}paused=false;}
        void OnDisable()=>Stop();
    }
}
