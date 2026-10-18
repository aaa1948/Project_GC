using UnityEngine;
namespace Vampire
{
    public enum FoodBossSound { Shot,Radial,Dash,Land,Skill1,Skill2,Recover,Break,Phase,Death,Skid }
    public sealed class FoodBossAudio:MonoBehaviour
    {
        AudioSource[] voices;AudioClip[] clips;float[] gains;FoodBossRuntime boss;float lastShot=-10,lastBreak=-10;bool paused;
        public int PlayedCount {get;private set;}
        public void Initialize(FoodBossRuntime owner)
        {
            boss=owner;voices=new AudioSource[6];gains=new float[6];
            for(int i=0;i<voices.Length;i++){var s=gameObject.AddComponent<AudioSource>();s.playOnAwake=false;s.spatialBlend=0;s.priority=80;voices[i]=s;}
            var names=System.Enum.GetNames(typeof(FoodBossSound));clips=new AudioClip[names.Length];for(int i=0;i<clips.Length;i++)clips[i]=Resources.Load<AudioClip>($"FoodBosses/Audio/{(int)boss.Kind}-{names[i]}");
        }
        public bool HasAllClips=>clips!=null&&System.Array.TrueForAll(clips,c=>c!=null);
        public void Play(FoodBossSound sound,float gain=1)
        {
            if(voices==null||FoodBossRuntime.Paused)return;int id=(int)sound;if(clips[id]==null)return;
            if(sound==FoodBossSound.Shot&&Time.time-lastShot<.075f)return;if(sound==FoodBossSound.Shot)lastShot=Time.time;
            if(sound==FoodBossSound.Break&&Time.time-lastBreak<.09f)return;if(sound==FoodBossSound.Break)lastBreak=Time.time;
            int slot=System.Array.FindIndex(voices,s=>!s.isPlaying);if(slot<0){if(sound==FoodBossSound.Shot)return;slot=0;}
            var v=voices[slot];v.Stop();v.clip=clips[id];v.pitch=Random.Range(.97f,1.03f);gains[slot]=gain*(sound==FoodBossSound.Shot?.35f:sound==FoodBossSound.Land?.85f:.6f);Apply(slot);v.Play();PlayedCount++;
        }
        void Apply(int i)
        {
            var cam=Camera.main;float attenuation=1,pan=0;if(cam!=null){var viewport=cam.WorldToViewportPoint(transform.position);pan=Mathf.Clamp((viewport.x-.5f)*.6f,-.4f,.4f);float d=Vector2.Distance(cam.transform.position,transform.position);attenuation=Mathf.Clamp01(1-d/24f);}
            voices[i].volume=gains[i]*GamePreferences.Current.effects*attenuation;voices[i].panStereo=pan;
        }
        void Update()
        {
            if(voices==null)return;bool stop=FoodBossRuntime.Paused;if(stop!=paused){foreach(var v in voices)if(stop)v.Pause();else v.UnPause();paused=stop;}
            for(int i=0;i<voices.Length;i++)Apply(i);
        }
        public void Stop(){if(voices!=null)foreach(var v in voices)v.Stop();}
        void OnDisable()=>Stop();
    }
}
