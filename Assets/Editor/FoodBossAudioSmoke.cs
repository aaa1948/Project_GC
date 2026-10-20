using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Vampire;

// Isolated playback check: does not open, save or modify gameplay scenes/preferences.
[InitializeOnLoad]
public static class FoodBossAudioSmoke
{
    const string Key="FoodBossAudioSmoke";
    static double deadline,next;
    static int bossIndex,eventIndex,checkedCount,stage;
    static FoodBossRuntime boss;
    static AudioSource voice;
    static GamePreferenceValues original;
    static FoodBossSound[] events;
    static FoodBossAudioSmoke(){if(SessionState.GetBool(Key,false))Attach();}
    public static void Run()
    {
        SessionState.SetBool(Key,true);SessionState.SetBool(Key+"Done",false);SessionState.SetBool(Key+"Failed",false);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        Attach();EditorApplication.EnterPlaymode();
    }
    static void Attach(){deadline=EditorApplication.timeSinceStartup+240;EditorApplication.update-=Tick;EditorApplication.update+=Tick;}
    static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Tick()
    {
        if(!SessionState.GetBool(Key,false))return;
        if(SessionState.GetBool(Key+"Done",false))
        {
            if(EditorApplication.isPlaying)EditorApplication.ExitPlaymode();
            else if(!EditorApplication.isPlayingOrWillChangePlaymode){SessionState.SetBool(Key,false);EditorApplication.Exit(SessionState.GetBool(Key+"Failed",false)?1:0);}
            return;
        }
        try
        {
            Require(EditorApplication.timeSinceStartup<deadline,"Playback check timed out");
            if(!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
            if(stage==0)
            {
                original=GamePreferences.Current.Copy();var settings=original.Copy();settings.effects=1;settings.master=1;settings.muted=false;settings.pauseOnFocusLoss=false;settings.muteOnFocusLoss=false;
                GamePreferences.Apply(settings,false,false);Time.timeScale=1;AudioListener.pause=false;
                var camera=new GameObject("Playback test camera").AddComponent<Camera>();camera.tag="MainCamera";camera.transform.position=new Vector3(0,0,-10);camera.gameObject.AddComponent<AudioListener>();
                stage=1;next=EditorApplication.timeSinceStartup+2;return;
            }
            if(EditorApplication.timeSinceStartup<next)return;
            if(stage==1)
            {
                if(boss==null)
                {
                    boss=FoodBossRuntime.Spawn((FoodBossKind)bossIndex,null,Vector2.zero);boss.AutoPatterns=false;boss.transform.position=Vector3.zero;
                    Require(boss.Audio.HasAllClips,"Missing resource for boss "+bossIndex);
                    events=Enum.GetValues(typeof(FoodBossSound)).Cast<FoodBossSound>().Where(s=>s!=FoodBossSound.Beam&&s!=FoodBossSound.Consume||s==FoodBossSound.Beam&&bossIndex==3||s==FoodBossSound.Consume&&bossIndex==0).ToArray();eventIndex=0;
                }
                var sound=events[eventIndex];boss.Audio.Stop();boss.Audio.Play(sound);
                voice=boss.GetComponents<AudioSource>().FirstOrDefault(v=>v.isPlaying);
                Require(voice!=null,"Not playing "+bossIndex+"-"+sound);
                // Loop only inside this test so a slow editor frame cannot outlast a short one-shot.
                voice.loop=true;
                Require(voice.clip.loadState==AudioDataLoadState.Loaded&&voice.volume>0,"Unloaded/muted clip");
                var pcm=new float[voice.clip.samples*voice.clip.channels];Require(voice.clip.GetData(pcm,0)&&pcm.Any(v=>Mathf.Abs(v)>.001f),"Silent decoded clip");
                stage=2;next=EditorApplication.timeSinceStartup+.06;
            }
            else if(stage==2)
            {
                Require(voice.isPlaying&&voice.timeSamples>0,"Audio clock did not advance: "+bossIndex+"-"+events[eventIndex]+" playing="+voice.isPlaying+" samples="+voice.timeSamples);
                Debug.Log("FOOD_AUDIO_PASS "+bossIndex+"-"+events[eventIndex]+" samples="+voice.timeSamples);
                checkedCount++;voice.loop=false;boss.Audio.Stop();eventIndex++;stage=1;
                if(eventIndex>=events.Length){UnityEngine.Object.DestroyImmediate(boss.gameObject);boss=null;bossIndex++;}
                if(bossIndex==5){Require(checkedCount==57,"Wrong clip count");GamePreferences.Apply(original,false,false);Debug.Log("FOOD_AUDIO_SMOKE_PASS clips=57");SessionState.SetBool(Key+"Done",true);}
            }
        }
        catch(Exception ex)
        {
            Debug.LogError("FOOD_AUDIO_SMOKE_FAIL "+ex);if(original!=null)GamePreferences.Apply(original,false,false);
            SessionState.SetBool(Key+"Failed",true);SessionState.SetBool(Key+"Done",true);
        }
    }
}
